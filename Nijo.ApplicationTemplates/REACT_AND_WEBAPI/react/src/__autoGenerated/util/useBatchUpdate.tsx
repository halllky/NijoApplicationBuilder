import React, { useCallback, useContext, useEffect, useMemo, useReducer, useState } from 'react'
import { UUID } from 'uuidjs'
import * as ReactUtil from './ReactUtil'
import * as Validation from './Validation'
import * as Notification from './Notification'
import { useFieldArray } from 'react-hook-form'


// 一覧/特定集約 共用

export type LocalRepositoryState
  = '' // No Change
  | '+' // Add
  | '*' // Modify
  | '-' // Delete

type LocalRepositoryStoredItem = {
  dataTypeKey: string
  itemKey: string
  itemName: string
  serializedItem: string
  state: LocalRepositoryState
}

const useIndexedDbLocalRepositoryTable = () => {
  return useIndexedDbTable<LocalRepositoryStoredItem>({
    dbName: '::nijo::',
    dbVersion: 1,
    tableName: 'LocalRepository',
    keyPath: ['dataTypeKey', 'itemKey'],
  })
}

// -------------------------------------------------
// ローカルリポジトリ変更一覧

export type LocalRepositoryItemListItem = {
  dataTypeKey: string
  itemKey: string
  itemName: string
  state: LocalRepositoryState
}

type LocalRepositoryContextValue = {
  changes: LocalRepositoryItemListItem[]
  setToLocalRepository: (value: LocalRepositoryStoredItem) => Promise<void>
  deleteFromLocalRepository: (key: IDBValidKey) => Promise<void>
  ready: boolean
}
const LocalRepositoryContext = React.createContext<LocalRepositoryContextValue>({
  changes: [],
  setToLocalRepository: () => Promise.resolve(),
  deleteFromLocalRepository: () => Promise.resolve(),
  ready: false,
})

export const LocalRepositoryContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const { ready, loadRecords, setRecord, deleteRecord } = useIndexedDbLocalRepositoryTable()
  const [changes, setChanges] = useState<LocalRepositoryItemListItem[]>([])

  const reload = useCallback(async () => {
    const records = await loadRecords()
    const changes = records.map(r => ({
      dataTypeKey: r.dataTypeKey,
      state: r.state,
      itemKey: r.itemKey,
      itemName: r.itemName,
    }))
    setChanges(changes)
  }, [loadRecords, setChanges])

  const setToLocalRepository = useCallback(async (data: LocalRepositoryStoredItem) => {
    await setRecord(data)
    await reload()
  }, [setRecord, reload])

  const deleteFromLocalRepository = useCallback(async (key: IDBValidKey) => {
    await deleteRecord(key)
    await reload()
  }, [deleteRecord, reload])

  const contextValue: LocalRepositoryContextValue = useMemo(() => ({
    changes,
    setToLocalRepository,
    deleteFromLocalRepository,
    ready,
  }), [changes, setToLocalRepository, deleteFromLocalRepository, ready])

  useEffect(() => {
    if (ready) reload()
  }, [reload, ready])

  return (
    <LocalRepositoryContext.Provider value={contextValue}>
      {children}
    </LocalRepositoryContext.Provider>
  )
}

export const useLocalRepositoryChangeList = () => {
  const { ready, changes } = useContext(LocalRepositoryContext)
  return { ready, changes }
}

// -------------------------------------------------
// 特定の集約の変更

export type LocalRepositoryArgs<T> = {
  dataTypeKey: string
  getItemKey: (t: T) => IDBValidKey
  getItemName?: (t: T) => string
  serialize: (t: T) => string
  deserialize: (str: string) => T
}
export type LocalRepositoryStateAndKeyAndItem<T> = {
  itemKey: string
  state: LocalRepositoryState
  item: T
}

export const useLocalRepository = <T,>({
  dataTypeKey,
  getItemKey,
  getItemName,
  serialize,
  deserialize,
}: LocalRepositoryArgs<T>) => {

  const {
    ready,
    setToLocalRepository: setToDb,
    deleteFromLocalRepository: delFromDb,
  } = useContext(LocalRepositoryContext)
  const {
    loadRecords,
  } = useIndexedDbLocalRepositoryTable()

  const loadAll = useCallback(async (): Promise<LocalRepositoryStateAndKeyAndItem<T>[]> => {
    const records = await loadRecords(x => x.dataTypeKey === dataTypeKey)
    return records.map(x => ({
      item: deserialize(x.serializedItem),
      itemKey: x.itemKey,
      state: x.state,
    }))
  }, [dataTypeKey, deserialize, loadRecords])

  const loadOne = useCallback(async (itemKey: string): Promise<LocalRepositoryStateAndKeyAndItem<T> | undefined> => {
    const records = await loadRecords(x =>
      x.dataTypeKey === dataTypeKey
      && x.itemKey === itemKey)
    if (records.length === 0) return undefined
    return {
      item: deserialize(records[0].serializedItem),
      itemKey: records[0].itemKey,
      state: records[0].state,
    }
  }, [dataTypeKey, deserialize, loadRecords])

  const getLocalRepositoryState = useCallback(async (itemKey: string): Promise<LocalRepositoryState> => {
    return (await loadOne(itemKey))?.state ?? ''
  }, [loadOne, dataTypeKey])

  const addToLocalRepository = useCallback(async (item: T): Promise<LocalRepositoryStateAndKeyAndItem<T>> => {
    const itemKey = UUID.generate()
    const itemName = getItemName?.(item) ?? ''
    const serializedItem = serialize(item)
    const state: LocalRepositoryState = '+'
    await setToDb({ state, dataTypeKey, itemKey, itemName, serializedItem })
    return { itemKey, state, item }
  }, [dataTypeKey, setToDb, getItemName, serialize])

  const updateLocalRepositoryItem = useCallback(async (itemKey: string, item: T): Promise<LocalRepositoryStateAndKeyAndItem<T>> => {
    const serializedItem = serialize(item)
    const itemName = getItemName?.(item) ?? ''
    const stateBeforeUpdate = await getLocalRepositoryState(itemKey)
    const state: LocalRepositoryState = stateBeforeUpdate === '+' || stateBeforeUpdate === '-'
      ? stateBeforeUpdate
      : '*'
    await setToDb({ dataTypeKey, itemKey, itemName, serializedItem, state })
    return { itemKey, state, item }
  }, [dataTypeKey, setToDb, serialize, getItemName, getLocalRepositoryState])

  const deleteLocalRepositoryItem = useCallback(async (itemKey: string, item: T): Promise<{ remains: boolean }> => {
    const stateBeforeUpdate = await getLocalRepositoryState(itemKey)
    if (stateBeforeUpdate === '+') {
      await delFromDb([dataTypeKey, itemKey])
      return { remains: false }
    } else {
      const serializedItem = serialize(item)
      const itemName = getItemName?.(item) ?? ''
      const state: LocalRepositoryState = '-'
      await setToDb({ dataTypeKey, itemKey, itemName, serializedItem, state })
      return { remains: true }
    }
  }, [dataTypeKey, delFromDb, setToDb, serialize, getItemName, getLocalRepositoryState])

  const commit = useCallback(async (itemKey: string): Promise<void> => {
    await delFromDb([dataTypeKey, itemKey])
  }, [delFromDb, dataTypeKey])

  const reset = useCallback(async (itemKey: string): Promise<void> => {
    await delFromDb([dataTypeKey, itemKey])
  }, [delFromDb, dataTypeKey])

  return {
    ready,
    loadAll,
    loadOne,
    getLocalRepositoryState,
    addToLocalRepository,
    updateLocalRepositoryItem,
    deleteLocalRepositoryItem,
    commit,
    reset,
  }
}

// -----------------------------------------------
// Paging

type PagingState = {
  itemCount: number | undefined
  pageSize: number
  currentPage: number
}
const pagingReducer = ReactUtil.defineReducer((state: PagingState) => ({
  prevPage: () => ({ ...state, currentPage: Math.max(0, state.currentPage - 1) }),
  nextPage: () => ({ ...state, currentPage: state.currentPage + 1 }),
}))

const usePaging = (pageSize: number = 20, itemCount?: number) => {
  const [{ currentPage }, dispatch] = useReducer(pagingReducer, undefined, () => ({
    itemCount,
    pageSize,
    currentPage: 0,
  }))

  const prevPage = useCallback(() => {
    dispatch(state => state.prevPage())
  }, [dispatch])

  const nextPage = useCallback(() => {
    dispatch(state => state.nextPage())
  }, [dispatch])

  return {
    currentPage,
    prevPage,
    nextPage,
  }
}

// ---------------------------------------
// IndexedDB
const useIndexedDbTable = <T,>({ dbName, dbVersion, tableName, keyPath }: {
  dbName: string,
  dbVersion: number,
  tableName: string,
  keyPath: (keyof T)[]
}) => {

  const [, dispatchMsg] = Notification.useMsgContext()
  const [db, setDb] = useState<IDBDatabase>()
  const [ready, setReady] = useState(false)

  // データベースを開く
  useEffect(() => {
    const request = indexedDB.open(dbName, dbVersion)
    request.onerror = ev => {
      dispatchMsg(msg => msg.error('データベースを開けませんでした。'))
    }
    request.onsuccess = ev => {
      setDb((ev.target as IDBOpenDBRequest).result)
      setReady(true)
    }
    request.onupgradeneeded = ev => {
      const db = (ev.target as IDBOpenDBRequest).result
      db.createObjectStore(tableName, { keyPath: keyPath as string[] })
    }

    return () => {
      db?.close()
    }
  }, [dbName, dbVersion, tableName, dispatchMsg, ...keyPath])

  /** データ追加更新 */
  const setRecord = useCallback((data: T): Promise<void> => {
    if (!db) throw Promise.reject('データベースが初期化されていません。')
    return new Promise<void>((resolve, reject) => {
      const transaction = db.transaction([tableName], 'readwrite')
      const objectStore = transaction.objectStore(tableName)
      const request = objectStore.put(data)
      request.onerror = ev => reject('データの更新に失敗しました。')
      request.onsuccess = ev => resolve()
    })
  }, [db, tableName, dispatchMsg])

  /** データ削除 */
  const deleteRecord = useCallback((key: IDBValidKey): Promise<void> => {
    if (!db) throw Promise.reject('データベースが初期化されていません。')
    return new Promise<void>((resolve, reject) => {
      const transaction = db.transaction([tableName], 'readwrite')
      const objectStore = transaction.objectStore(tableName)
      const request = objectStore.delete(key)
      request.onerror = ev => reject('データの削除に失敗しました。')
      request.onsuccess = ev => resolve()
    })
  }, [db, tableName, dispatchMsg])

  const loadRecords = useCallback((filter?: (data: T) => boolean): Promise<T[]> => {
    if (!db) throw Promise.reject('データベースが初期化されていません。')
    return new Promise((resolve, reject) => {
      const transaction = db.transaction([tableName], 'readonly')
      const objectStore = transaction.objectStore(tableName)
      const request = objectStore.openCursor()
      const queryResult: T[] = []
      request.onerror = ev => reject('データの検索に失敗しました。')
      request.onsuccess = ev => {
        const cursor = (ev.target as IDBRequest<IDBCursorWithValue>).result
        if (cursor) {
          if (filter === undefined || filter(cursor.value)) {
            queryResult.push(cursor.value)
          }
          cursor.continue()
        } else {
          resolve(queryResult)
        }
      }
    })
  }, [db, tableName, dispatchMsg])

  const count = useCallback(async (filter?: (data: T) => boolean): Promise<number> => {
    if (!db) throw Promise.reject('データベースが初期化されていません。')
    return new Promise((resolve, reject) => {
      const transaction = db.transaction([tableName], 'readonly')
      const objectStore = transaction.objectStore(tableName)
      const request = objectStore.openCursor()
      let cnt = 0
      request.onerror = ev => reject('データの検索に失敗しました。')
      request.onsuccess = ev => {
        const cursor = (ev.target as IDBRequest<IDBCursorWithValue>).result
        if (cursor) {
          if (filter === undefined || filter(cursor.value)) {
            cnt++
          }
          cnt++
          cursor.continue()
        } else {
          resolve(cnt)
        }
      }
    })
  }, [db, tableName, dispatchMsg])

  return {
    ready,
    setRecord,
    deleteRecord,
    loadRecords,
    count,
  }
}

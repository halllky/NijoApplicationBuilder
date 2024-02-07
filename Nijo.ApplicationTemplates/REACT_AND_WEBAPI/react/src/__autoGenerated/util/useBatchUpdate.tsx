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
  const { ready, reduce, request } = useIndexedDbLocalRepositoryTable()
  const [changes, setChanges] = useState<LocalRepositoryItemListItem[]>([])

  const reload = useCallback(async () => {
    const changes = await reduce<LocalRepositoryItemListItem[]>([], (arr, cursor) => [...arr, {
      dataTypeKey: cursor.value.dataTypeKey,
      state: cursor.value.state,
      itemKey: cursor.value.itemKey,
      itemName: cursor.value.itemName,
    }])
    setChanges(changes)
  }, [reduce, setChanges])

  const setToLocalRepository = useCallback(async (data: LocalRepositoryStoredItem) => {
    await request(table => table.put(data))
    await reload()
  }, [request, reload])

  const deleteFromLocalRepository = useCallback(async (key: IDBValidKey) => {
    await request(table => table.delete(key))
    await reload()
  }, [request, reload])

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
    reduce,
    request,
  } = useIndexedDbLocalRepositoryTable()

  const loadAll = useCallback(async (): Promise<LocalRepositoryStateAndKeyAndItem<T>[]> => {
    return await reduce<LocalRepositoryStateAndKeyAndItem<T>[]>([], (arr, cursor) => {
      return cursor.value.dataTypeKey === dataTypeKey
        ? [...arr, {
          item: deserialize(cursor.value.serializedItem),
          itemKey: cursor.value.itemKey,
          state: cursor.value.state,
        }]
        : arr
    })
  }, [dataTypeKey, deserialize, reduce])

  const loadOne = useCallback(async (itemKey: string): Promise<LocalRepositoryStateAndKeyAndItem<T> | undefined> => {
    const key: IDBValidKey = [dataTypeKey, itemKey]
    const found = await request(table => table.get(key) as IDBRequest<LocalRepositoryStoredItem>)
    return found === undefined ? undefined : {
      item: deserialize(found.serializedItem),
      itemKey: found.itemKey,
      state: found.state,
    }
  }, [dataTypeKey, deserialize, request])

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

  // 読み込み系処理全般
  const reduce = useCallback(<T,>(initialState: T, fn: ((beforeState: T, cursor: IDBCursorWithValue) => T)): Promise<T> => {
    if (!db) throw Promise.reject('データベースが初期化されていません。')
    return new Promise<T>((resolve, reject) => {
      const transaction = db.transaction([tableName], 'readonly')
      const objectStore = transaction.objectStore(tableName)
      const request = objectStore.openCursor()
      let currentState = initialState
      request.onerror = ev => reject(ev)
      request.onsuccess = ev => {
        const cursor = (ev.target as IDBRequest<IDBCursorWithValue>).result
        if (cursor) {
          currentState = fn(currentState, cursor)
          cursor.continue()
        } else {
          resolve(currentState)
        }
      }
    })
  }, [db, tableName, dispatchMsg])

  // put, deleteなどのIDBObjectStoreのAPIを直接使うもの全般
  const request = useCallback(<T,>(fn: ((store: IDBObjectStore) => IDBRequest<T>), mode: IDBTransactionMode = 'readwrite'): Promise<T> => {
    if (!db) throw Promise.reject('データベースが初期化されていません。')
    return new Promise<T>((resolve, reject) => {
      const transaction = db.transaction([tableName], mode)
      const objectStore = transaction.objectStore(tableName)
      const request = fn(objectStore)
      request.onerror = ev => reject(ev)
      request.onsuccess = ev => resolve((ev.target as IDBRequest<T>).result)
    })
  }, [db, tableName, dispatchMsg])

  return {
    ready,
    reduce,
    request,
  }
}

import React, { useCallback, useEffect, useMemo, useReducer, useState } from 'react'
import { UUID } from 'uuidjs'
import * as ReactUtil from './ReactUtil'
import * as Validation from './Validation'
import * as Notification from './Notification'
import { useFieldArray } from 'react-hook-form'


// 一覧/特定集約 共用

export type ChangeType
  = undefined // No Change
  | '+' // Add
  | '*' // Modify
  | '-' // Delete

export type LocalRepositoryArgs<T> = {
  dataTypeKey: string
  getItemKey: (t: T) => IDBValidKey
  getItemName?: (t: T) => string
  getNewItem: () => T
  serialize: (t: T) => string
  deserialize: (str: string) => T
}
export type ItemWithLocalRepositoryState<T> = {
  dataTypeKey: string
  itemKey: string
  itemName: string
  item: T
  changeType: ChangeType
}
export type LocalRepositoryItem = Omit<ItemWithLocalRepositoryState<unknown>, 'item'>

const useIndexedDbLocalRepositoryTable = <T,>() => {
  return useIndexedDbTable<ItemWithLocalRepositoryState<T>>({
    dbName: '::nijo::',
    dbVersion: 1,
    tableName: 'LocalRepository',
    keyPath: ['dataTypeKey', 'itemKey'],
  })
}

// -------------------------------------------------
// ローカルリポジトリ変更一覧

export const useLocalRepositoryList = () => {
  const { loadRecords, deleteRecord } = useIndexedDbLocalRepositoryTable()

  const loadAll = useCallback(async (): Promise<LocalRepositoryItem[]> => {
    return await loadRecords()
  }, [loadRecords])

  const commitChanges = useCallback(async (): Promise<void> => {
    throw new Error()
  }, [])

  const clearChanges = useCallback(async (): Promise<void> => {
    throw new Error()
  }, [])

  return {
    loadAll,
    commitChanges,
    clearChanges,
  }
}

const {
  reducer: localRepositoryReducer,
  ContextProvider: LocalRepositoryContextProviderInternal,
  useContext: useLocalRepositoryListContext,
} = ReactUtil.defineContext2(
  (): LocalRepositoryItem[] => [],
  state => ({
    addChangeList: (...items: LocalRepositoryItem[]) => [...state, ...items],
    clearChangeList: () => [],
  })
)

export const LocalRepositoryContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const { loadAll } = useLocalRepositoryList()
  const [state, dispatch] = useReducer(localRepositoryReducer, undefined, () => [])
  const memorized = useMemo(() => [state, dispatch] as const, [state, dispatch])

  useEffect(() => {
    loadAll().then(data => {
      dispatch(state => state.addChangeList(...data))
    })
  }, [loadAll])

  return (
    <LocalRepositoryContextProviderInternal value={memorized}>
      {children}
    </LocalRepositoryContextProviderInternal>
  )
}

// -------------------------------------------------
// 特定の集約の変更

export const useLocalRepository = <T,>({
  dataTypeKey,
  getItemKey,
  getItemName,
  getNewItem,
  serialize,
  deserialize,
}: LocalRepositoryArgs<T>) => {

  const { loadRecords, setRecord, deleteRecord } = useIndexedDbLocalRepositoryTable<T>()
  const rhf = Validation.useFormEx<{ items: ItemWithLocalRepositoryState<T>[] }>({})
  const { fields, insert, remove, update } = useFieldArray({ name: 'items', control: rhf.control })
  const [, dispatchInmemory] = useLocalRepositoryListContext()
  const paging = usePaging()

  const items: ItemWithLocalRepositoryState<T>[] = fields

  const createNewItem = useCallback(async (): Promise<string> => {
    const itemKey = UUID.generate()
    const itemName = '新しいデータ'
    const changeType: ChangeType = '+'
    const item = getNewItem()
    const newItem = { dataTypeKey, itemKey, itemName, changeType, item }

    await setRecord(newItem)
    dispatchInmemory(state => state.addChangeList(newItem))
    insert(0, newItem)

    return itemKey
  }, [dataTypeKey, getNewItem, setRecord])

  const modifyItem = useCallback(async (item: ItemWithLocalRepositoryState<T>): Promise<void> => {
    const updated: ItemWithLocalRepositoryState<T> = { ...item, changeType: '*' }
    await setRecord(updated)
    const index = fields.findIndex(x => x.itemKey === updated.itemKey)
    if (index !== -1) update(index, updated)
  }, [setRecord, fields, update])

  const markToDelete = useCallback(async (...items: ItemWithLocalRepositoryState<T>[]): Promise<void> => {
    for (const item of items) {
      const deleted: ItemWithLocalRepositoryState<T> = { ...item, changeType: '*' }
      await setRecord(deleted)
      const index = fields.findIndex(x => x.itemKey === deleted.itemKey)
      if (index !== -1) update(index, deleted)
    }
  }, [setRecord, fields, update])

  return {
    items,
    ...paging,
    createNewItem,
    modifyItem,
    markToDelete,
    rhf,
  }
}

// API検討途中
// const {
//   items,
//   currentPage, showPrevPage, showNextPage,
//   append, remove, udpate,
//   commitChanges, clearChanges,
// } = useLocalRepository({
//   dataTypeKey: '集約A',
//   getItemKey: obj => [obj.key1, obj.key2],
//   initializer: () => ({ name: 'デフォルト名' }),
//   serialize: obj => JSON.stringify(obj),
//   deserialize: str => JSON.parse(str),
// })


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

  // データベースを開く
  useEffect(() => {
    const request = indexedDB.open(dbName, dbVersion)
    request.onerror = ev => {
      dispatchMsg(msg => msg.error('データベースを開けませんでした。'))
    }
    request.onsuccess = ev => {
      setDb((ev.target as IDBOpenDBRequest).result)
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
    if (!db) { dispatchMsg(msg => msg.error('データベースが初期化されていません。')); return Promise.resolve() }
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
    if (!db) { dispatchMsg(msg => msg.error('データベースが初期化されていません。')); return Promise.resolve() }
    return new Promise<void>((resolve, reject) => {
      const transaction = db.transaction([tableName], 'readwrite')
      const objectStore = transaction.objectStore(tableName)
      const request = objectStore.delete(key)
      request.onerror = ev => reject('データの削除に失敗しました。')
      request.onsuccess = ev => resolve()
    })
  }, [db, tableName, dispatchMsg])

  const loadRecords = useCallback((filter?: (data: T) => boolean): Promise<T[]> => {
    if (!db) { dispatchMsg(msg => msg.error('データベースが初期化されていません。')); return Promise.resolve([]) }
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
    if (!db) { dispatchMsg(msg => msg.error('データベースが初期化されていません。')); return Promise.resolve(0) }
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
    setRecord,
    deleteRecord,
    loadRecords,
    count,
  }
}

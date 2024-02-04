import { useCallback, useEffect, useMemo, useReducer, useState } from 'react'
import { UUID } from 'uuidjs'
import * as ReactUtil from './ReactUtil'
import * as Notification from './Notification'

export type ChangeType
  = undefined // No Change
  | '+' // Add
  | '*' // Modify
  | '-' // Delete

export type IndexedDbItemType = {
  dataTypeKey: string
  itemKey: string
  itemName: string
  changeType: ChangeType
  contentsJson: string
}
export type ContextItem = Omit<IndexedDbItemType, 'contentsJson'>

const useLocalReposIndexedDb = () => {
  return useIndexedDbTable<IndexedDbItemType>(
    '::nijo::',
    1,
    '::nijo::local-repository',
    ['dataTypeKey', 'itemKey'])
}

const [BatchUpdateContextProvider, useBatchUpdateContext] = ReactUtil.defineContext(
  (): ContextItem[] => [],

  state => ({
    reloadLocalChanges: (newState: ContextItem[]) => newState,
    add: (...items: ContextItem[]) => [...state, ...items],
    clear: () => [],
  }),

  (Ctx, reducer) => ({ children }: {
    children?: React.ReactNode
  }) => {
    const [, dispatchMsg] = Notification.useMsgContext()
    const [state, dispatch] = useReducer(reducer, undefined, () => [])
    const memorized = useMemo(() => [state, dispatch] as const, [state, dispatch])
    const { loadFromTable } = useLocalReposIndexedDb()

    // 初回読み込み
    useEffect(() => {
      loadFromTable().then(d => {
        dispatch(state => state.reloadLocalChanges(d))
      }).catch(() => {
        dispatchMsg(msg => msg.warn('ローカルリポジトリからの初回読み込みに失敗しました。'))
      })
    }, [loadFromTable, dispatch])

    return (
      <Ctx.Provider value={memorized}>
        {children}
      </Ctx.Provider>
    )
  }
)

const useLocalRepository = () => {
  const [list, dispatch] = useBatchUpdateContext()
  const { loadFromTable, putToTable, deleleFromTable } = useLocalReposIndexedDb()

  // -------------------------------
  // マルチ

  const reloadList = useCallback(async (): Promise<void> => {
    // TODO: IndexedDBから非同期読み込み
    // TODO: dispatchする
  }, [])

  const resetChanges = useCallback(async (): Promise<void> => {
    //TODO: IndexedDBからの削除
    //TODO: dispatchする
  }, [])

  const commitChanges = useCallback(async (): Promise<void> => {
    // TODO: ローカルリポジトリから非同期読み込み
    // TODO: 保存(サーバーAPIをたたく)
    // TODO: dispatchする
  }, [])

  // -------------------------------
  // シングル

  const createNewItem = useCallback(async <T,>(dataTypeKey: string, item: T): Promise<string> => {
    const itemKey = UUID.generate()
    const itemName = '新しいデータ'
    const changeType = '+'
    const contentsJson = JSON.stringify(item)
    await putToTable({ dataTypeKey, itemKey, itemName, changeType, contentsJson })
    dispatch(state => state.add({ dataTypeKey, itemKey, itemName, changeType }))
    return itemKey
  }, [putToTable])

  const loadItem = useCallback(async (dataTypeKey: string, key: string): Promise<IndexedDbItemType | undefined> => {
    const items = await loadFromTable(x => x.dataTypeKey === dataTypeKey && x.itemKey === key)
    return items[0]
    // TODO: 見つからない場合sqliteから検索(サーバーAPIをたたく)
  }, [loadFromTable])

  const saveLocal = useCallback(async (item: IndexedDbItemType): Promise<void> => {
    // TOOD: IndexedDBに保存
    // TODO: dispatchする
  }, [])

  const markToDelete = useCallback(async <T,>(item: T): Promise<void> => {
    // TOOD: IndexedDBに保存
    // TODO: dispatchする
  }, [])

  return {
    list,
    reloadList,
    loadItem,
    createNewItem,
    saveLocal,
    markToDelete,
    resetChanges,
  }
}

export {
  BatchUpdateContextProvider,
  useLocalRepository,
}


// ---------------------------------------
// IndexedDB
const useIndexedDbTable = <T,>(
  dbName: string,
  dbVersion: number,
  tableName: string,
  keyPath: (keyof T)[]) => {

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
  const putToTable = useCallback((data: T): Promise<void> => {
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
  const deleleFromTable = useCallback((key: IDBValidKey): Promise<void> => {
    if (!db) { dispatchMsg(msg => msg.error('データベースが初期化されていません。')); return Promise.resolve() }
    return new Promise<void>((resolve, reject) => {
      const transaction = db.transaction([tableName], 'readwrite')
      const objectStore = transaction.objectStore(tableName)
      const request = objectStore.delete(key)
      request.onerror = ev => reject('データの削除に失敗しました。')
      request.onsuccess = ev => resolve()
    })
  }, [db, tableName, dispatchMsg])

  const loadFromTable = useCallback((filter?: (data: T) => boolean): Promise<T[]> => {
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

  return {
    putToTable,
    deleleFromTable,
    loadFromTable,
  }
}

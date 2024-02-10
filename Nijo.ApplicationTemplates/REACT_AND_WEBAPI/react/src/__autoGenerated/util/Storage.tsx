import { useCallback, useEffect, useReducer, useState } from "react"
import { defineContext } from "./ReactUtil"
import { useMsgContext } from "./Notification"

// ---------------------------------------
// LocalStorage
export type LocalStorageHandler<T> = {
  storageKey: string
  serialize: (obj: T) => string
  deserialize: (str: string) => DeserializeResult<T>
  defaultValue: () => T
  noMessageOnSave?: boolean
}
export type DeserializeResult<T>
  = { ok: true, obj: T }
  | { ok: false }

export const defineStorageContext = <T,>(handler: LocalStorageHandler<T>) => {
  const [CacheProvider, useCache] = defineContext(
    // 初回読み込み前の初期状態
    handler.defaultValue,

    // 操作
    () => ({
      setValue: (value: T) => value,
    }),

    // プロバイダ
    (Ctx, reducer) => ({ children }) => {
      const [, dispatchMsg] = useMsgContext()
      const reducerValue = useReducer(reducer, (() => {
        // 画面表示時読み込み
        try {
          const serialized = localStorage.getItem(handler.storageKey)
          if (serialized == null) {
            return handler.defaultValue()
          } else {
            const deserializeResult = handler.deserialize(serialized)
            if (deserializeResult?.ok !== true) throw new Error(`Failuer to parse local storage value as '${handler.storageKey}'.`)
            return deserializeResult.obj
          }
        } catch (error) {
          dispatchMsg(msg => msg.warn(error))
          return handler.defaultValue()
        }
      })())

      return (
        <Ctx.Provider value={reducerValue}>
          {children}
        </Ctx.Provider>
      )
    },
  )

  const useLocalStorage = () => {
    const [, dispatchMsg] = useMsgContext()
    const [cache, dispatchCache] = useCache()

    const save = useCallback((value: T) => {
      localStorage.setItem(
        handler.storageKey,
        handler.serialize(value))
      if (!handler.noMessageOnSave) {
        dispatchMsg(msg => msg.info('保存しました。'))
      }
      dispatchCache(state => state.setValue(value))
    }, [dispatchMsg, dispatchCache])

    return { data: cache, save }
  }

  return [CacheProvider, useLocalStorage] as const
}


// ---------------------------------------
// IndexedDB
export const useIndexedDbTable = <T,>({ dbName, dbVersion, tableName, keyPath }: {
  dbName: string,
  dbVersion: number,
  tableName: string,
  keyPath: (keyof T)[]
}) => {

  const [, dispatchMsg] = useMsgContext()
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
  type IDBCursorWithValueEx<T> = Omit<IDBCursorWithValue, 'value'> & { value: T }
  const openCursor = useCallback(async (mode: IDBTransactionMode, fn: ((cursor: IDBCursorWithValueEx<T>) => void)): Promise<void> => {
    if (!db) throw Promise.reject('データベースが初期化されていません。')
    await new Promise<void>((resolve, reject) => {
      const transaction = db.transaction([tableName], mode)
      const objectStore = transaction.objectStore(tableName)
      const request = objectStore.openCursor()
      request.onerror = ev => reject(ev)
      request.onsuccess = ev => {
        const cursor = (ev.target as IDBRequest<IDBCursorWithValueEx<T>>).result
        if (cursor) {
          fn(cursor)
          cursor.continue()
        } else {
          resolve()
        }
      }
    })
  }, [db, tableName])

  // put, deleteなどのIDBObjectStoreのAPIを直接使うもの全般
  type IDBObjectStoreEx<T> = Omit<IDBObjectStore, 'put' | 'get'> & {
    put: (value: T) => IDBRequest<IDBValidKey>
    get: (query: IDBValidKey | IDBKeyRange) => IDBRequest<T>
  }
  const openTable = useCallback(<TRequest,>(fn: ((store: IDBObjectStoreEx<T>) => IDBRequest<TRequest>), mode: IDBTransactionMode = 'readwrite'): Promise<TRequest> => {
    if (!db) throw Promise.reject('データベースが初期化されていません。')
    return new Promise<TRequest>((resolve, reject) => {
      const transaction = db.transaction([tableName], mode)
      const objectStore = transaction.objectStore(tableName)
      const request = fn(objectStore)
      request.onerror = ev => reject(ev)
      request.onsuccess = ev => resolve((ev.target as IDBRequest<TRequest>).result)
    })
  }, [db, tableName])

  // テスト用
  const dump = useCallback(async () => {
    const arr: T[] = []
    await openCursor('readonly', cursor => {
      arr.push(cursor.value)
    })
    return arr
  }, [openCursor])

  return {
    ready,
    openCursor,
    openTable,
    dump,
  }
}

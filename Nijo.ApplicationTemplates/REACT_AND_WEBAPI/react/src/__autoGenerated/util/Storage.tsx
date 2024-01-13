import { useCallback, useReducer } from "react"
import { defineContext } from "./ReactUtil"
import { useMsgContext } from "./Notification"

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
    }, [])

    return { data: cache, save }
  }

  return [CacheProvider, useLocalStorage] as const
}

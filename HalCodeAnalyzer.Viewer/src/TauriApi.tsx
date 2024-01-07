import { useCallback, useEffect, useReducer } from 'react'
import { invoke } from '@tauri-apps/api'
import { UnknownDataSource } from './Graph.DataSource'
import { Messaging, ReactHookUtil } from './util'

// 未使用 セキュリティの問題でReactからfsを使えない
const [CtxProvider, useTauriContext] = ReactHookUtil.defineContext(
  (): { targetFileName?: string } => ({}),
  state => ({
    changeTargetFile: (targetFileName: string) => {
      return { ...state, targetFileName }
    },
  }),

  (Ctx, reducer) => ({ children }) => {
    const [, dispatchMessage] = Messaging.useMsgContext()
    const contextValue = useReducer(reducer, {})
    useEffect(() => {
      invoke('get_openedfile_fullpath').then(result => {
        contextValue[1](state => state.changeTargetFile(result as string))
      }).catch((err: Error) => {
        dispatchMessage(msg => msg.push('error', err))
      })
    }, [])
    return <Ctx.Provider value={contextValue}>{children}</Ctx.Provider>
  },
)

export const TauriApiContextProvider = CtxProvider

export const useTauriApi = () => {
  const [, dispatchMessage] = Messaging.useMsgContext()

  const readTargetFile = useCallback(async (): Promise<UnknownDataSource | undefined> => {
    try {
      const json: string = await invoke('read_target_file_contents')
      const obj = JSON.parse(json)
      if (!json) throw new Error(`File is empty.`)
      if (typeof obj !== 'object') throw new Error(`File contents is not json object: ${json}`)
      return obj as UnknownDataSource
    } catch (error) {
      dispatchMessage(msg => msg.push('error', error))
      return undefined
    }
  }, [])

  const writeTargetFile = useCallback(async (obj: UnknownDataSource) => {
    try {
      const contents = JSON.stringify(obj, undefined, '  ')
      await invoke('write_target_file_contents', { contents })
    } catch (error) {
      dispatchMessage(msg => msg.push('error', error))
    }
  }, [])

  return {
    readTargetFile,
    writeTargetFile,
  }
}

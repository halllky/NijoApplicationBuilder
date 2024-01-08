import { useCallback } from 'react'
import { invoke, window as windowApi } from '@tauri-apps/api'
import { UnknownDataSource } from './Graph.DataSource'
import { Messaging } from './util'

export const useTauriApi = () => {
  const [, dispatchMessage] = Messaging.useMsgContext()

  const readTargetFile = useCallback(async (): Promise<UnknownDataSource | undefined> => {
    try {
      const obj: OpenedFile = await invoke('load_file', { suffix: SUFFIX.DATA_FILE })
      if (typeof obj !== 'object'
        || typeof obj.fullpath !== 'string'
        || typeof obj.contents !== 'string')
        throw new Error(`File open result is invalid object: ${JSON.stringify(obj)}`)

      windowApi.appWindow.setTitle(getBasename(obj.fullpath))

      const dataSource: UnknownDataSource = JSON.parse(obj.contents)
      return dataSource
    } catch (error) {
      dispatchMessage(msg => msg.push('error', error))
      return undefined
    }
  }, [])

  const writeTargetFile = useCallback(async (obj: UnknownDataSource) => {
    try {
      const contents = JSON.stringify(obj, undefined, '  ')
      await invoke('save_flie', { contents, suffix: SUFFIX.DATA_FILE })
    } catch (error) {
      dispatchMessage(msg => msg.push('error', error))
    }
  }, [])

  return {
    readTargetFile,
    writeTargetFile,
  }
}

const SUFFIX = {
  DATA_FILE: '',
  VIEWSTATE_FILE: '.viewState',
} as const

type OpenedFile = {
  fullpath: string
  contents: string
}
const getBasename = (fullpath: string) => {
  const splitted = fullpath
    .replaceAll('\\', '/')
    .split('/')
  return splitted[splitted.length - 1]
}

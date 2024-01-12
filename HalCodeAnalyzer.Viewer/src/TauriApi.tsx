import { useCallback } from 'react'
import { invoke, window as windowApi } from '@tauri-apps/api'
import { UnknownDataSource } from './DataSource'
import { ViewState, getEmptyViewState } from './Cy.SaveLoad'

export const useTauriApi = () => {

  // --------------- データファイル --------------------
  const loadTargetFile = useCallback(async (): Promise<UnknownDataSource> => {
    const dataFile: OpenedFile = await invoke('load_file', { suffix: SUFFIX.DATA_FILE })
    if (typeof dataFile !== 'object'
      || typeof dataFile.fullpath !== 'string'
      || typeof dataFile.contents !== 'string'
      || !dataFile.contents)
      throw new Error(`File open result is invalid object: ${JSON.stringify(dataFile)}`)

    windowApi.appWindow.setTitle(getBasename(dataFile.fullpath))
    return JSON.parse(dataFile.contents) as UnknownDataSource
  }, [])

  const saveTargetFile = useCallback(async (obj: UnknownDataSource) => {
    const contents = JSON.stringify(obj, undefined, '  ')
    await invoke('save_file', { contents, suffix: SUFFIX.DATA_FILE })
  }, [])

  // --------------- ビューステートファイル --------------------
  const loadViewStateFile = useCallback(async (): Promise<ViewState> => {
    const vsFile: OpenedFile = await invoke('load_file', { suffix: SUFFIX.VIEWSTATE_FILE })
    if (typeof vsFile !== 'object'
      || typeof vsFile.fullpath !== 'string'
      || typeof vsFile.contents !== 'string'
      || !vsFile.contents) {
      return getEmptyViewState()
    }

    return JSON.parse(vsFile.contents) as ViewState
  }, [])

  const saveViewStateFile = useCallback(async (obj: ViewState) => {
    const contents = JSON.stringify(obj, undefined, '  ')
    await invoke('save_file', { contents, suffix: SUFFIX.VIEWSTATE_FILE })
  }, [])


  return {
    loadTargetFile,
    saveTargetFile,
    loadViewStateFile,
    saveViewStateFile,
  }
}

const SUFFIX = {
  DATA_FILE: '',
  VIEWSTATE_FILE: '.viewstate',
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

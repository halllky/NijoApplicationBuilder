import * as React from "react"
import useEvent from "react-use-event-hook"
import { Perspective, TypedDocumentContextType } from "./types"
import { AppSettingsForDisplay, AppSettingsForSave } from "../types"
import { SERVER_DOMAIN } from "../../routes"
import { QueryEditor } from "../データプレビュー/types"
import { DATA_PREVIEW_LOCALSTORAGE_KEY, GET_DEFAULT_DATA } from "../データプレビュー"

/** 型つきドキュメントのコンテキスト。各画面から利用する関数群 */
export const TypedDocumentContext = React.createContext<TypedDocumentContextType>({
  isReady: false,
  loadAppSettings: () => { throw new Error("Not implemented") },
  saveAppSettings: () => { throw new Error("Not implemented") },
  createPerspective: () => { throw new Error("Not implemented") },
  loadPerspectivePageData: () => { throw new Error("Not implemented") },
  savePerspective: () => { throw new Error("Not implemented") },
} as TypedDocumentContextType)

/** C#側と合わせる必要あり */
export const SERVER_URL_SUBDIRECTORY = {
  LOAD_SETTINGS: `/typed-document/load-settings`,
  SAVE_SETTINGS: `/typed-document/save-settings`,
  LOAD_TYPED_DOCUMENT: `/typed-document/load`,
  SAVE_TYPED_DOCUMENT: `/typed-document/save`,
  LOAD_DATA_PREVIEW: `/data-preview/load`,
  SAVE_DATA_PREVIEW: `/data-preview/save`,
} as const

/** C#側と合わせる必要あり */
export type SERVER_API_TYPE_INFO = {
  [SERVER_URL_SUBDIRECTORY.LOAD_SETTINGS]: {
    body: undefined,
    query: undefined,
    response: AppSettingsForDisplay
  }
  [SERVER_URL_SUBDIRECTORY.SAVE_SETTINGS]: {
    body: AppSettingsForSave,
    query: undefined,
    response: void
  }
  [SERVER_URL_SUBDIRECTORY.LOAD_TYPED_DOCUMENT]: {
    body: undefined,
    query: { typeId: string },
    response: Perspective
  }
  [SERVER_URL_SUBDIRECTORY.SAVE_TYPED_DOCUMENT]: {
    body: Perspective,
    query: undefined,
    response: void
  }
  [SERVER_URL_SUBDIRECTORY.LOAD_DATA_PREVIEW]: {
    body: undefined,
    query: { dataPreviewId: string },
    response: QueryEditor
  }
  [SERVER_URL_SUBDIRECTORY.SAVE_DATA_PREVIEW]: {
    body: QueryEditor,
    query: undefined,
    response: void
  }
}

/**
 * 型つきドキュメントのコンテキストを提供する。
 * 永続化はlocalStorageに行う。
 */
export const useTypedDocumentContextProvider = (): TypedDocumentContextType => {

  const loadAppSettings: TypedDocumentContextType["loadAppSettings"] = React.useCallback(async () => {
    try {
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.LOAD_SETTINGS}`, {
        method: 'GET',
      })
      if (!response.ok) {
        alert(`一覧を取得できませんでした。(${response.status} ${response.statusText})`)
        return {
          applicationName: "",
          entityTypeList: [],
          dataPreviewList: [],
        }
      }
      const data: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.LOAD_SETTINGS]["response"] = await response.json()

      // 後方互換性（データプレビューをローカルストレージに保存していたときからの移行）
      if (data.dataPreviewList.length === 0) {
        const localStorageItem = localStorage.getItem(DATA_PREVIEW_LOCALSTORAGE_KEY)
        if (localStorageItem) {
          const dataPreivew: QueryEditor = JSON.parse(localStorageItem)
          data.dataPreviewList = [{
            id: dataPreivew.id,
            title: dataPreivew.title,
          }]
          await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.SAVE_DATA_PREVIEW}`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: localStorageItem,
          })
        } else {
          // データプレビューが1件も無い場合は新規作成
          data.dataPreviewList = [GET_DEFAULT_DATA()]
        }
      }

      return data
    } catch (error) {
      alert(`一覧を取得できませんでした。(${error})`)
      return {
        applicationName: "",
        entityTypeList: [],
        dataPreviewList: [],
      }
    }
  }, [])

  const saveAppSettings: TypedDocumentContextType["saveAppSettings"] = useEvent(async settings => {
    try {
      const body: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.SAVE_SETTINGS]["body"] = settings
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.SAVE_SETTINGS}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
      })
      if (!response.ok) {
        alert(`保存できませんでした。(${response.status} ${response.statusText})`)
        return false
      }
      await loadAppSettings()
      return true
    } catch (error) {
      alert(`保存できませんでした。(${error})`)
      return false
    }
  })

  const createPerspective: TypedDocumentContextType["createPerspective"] = useEvent(async newPerspective => {
    try {
      const body: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.SAVE_TYPED_DOCUMENT]["body"] = newPerspective
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.SAVE_TYPED_DOCUMENT}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
      })
      if (!response.ok) {
        alert(`保存できませんでした。(${response.status} ${response.statusText})`)
        return newPerspective
      }
      return newPerspective
    } catch (error) {
      alert(`保存できませんでした。(${error})`)
      return newPerspective
    }
  })

  const loadPerspectivePageData: TypedDocumentContextType["loadPerspectivePageData"] = useEvent(async perspectiveId => {
    try {
      const QUERY_KEY = "typeId" satisfies keyof SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.LOAD_TYPED_DOCUMENT]["query"]
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.LOAD_TYPED_DOCUMENT}?${QUERY_KEY}=${perspectiveId}`, {
        method: 'GET',
      })
      if (!response.ok) {
        alert(`読み込めませんでした。(${response.status} ${response.statusText})`)
        return undefined
      }
      const data: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.LOAD_TYPED_DOCUMENT]["response"] = await response.json()
      return {
        perspective: data,
      }
    } catch (error) {
      alert(`読み込めませんでした。(${error})`)
      return undefined
    }
  })

  const savePerspective: TypedDocumentContextType["savePerspective"] = useEvent(async data => {
    try {
      const body: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.SAVE_TYPED_DOCUMENT]["body"] = data.perspective
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.SAVE_TYPED_DOCUMENT}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
      })
      if (!response.ok) {
        alert(`保存できませんでした。(${response.status} ${response.statusText})`)
        return false
      }
      return true
    } catch (error) {
      alert(`保存できませんでした。(${error})`)
      return false
    }
  })

  return {
    isReady: true,
    loadAppSettings,
    saveAppSettings,
    createPerspective,
    loadPerspectivePageData,
    savePerspective,
  }
}

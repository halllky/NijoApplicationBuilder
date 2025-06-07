import * as React from "react"
import useEvent from "react-use-event-hook"
import { Entity, NavigationMenuItem, Perspective, TypedDocumentContextType, PerspectivePageData } from "./types"
import { SERVER_DOMAIN } from "../routes"

/** 型つきドキュメントのコンテキスト。各画面から利用する関数群 */
export const TypedDocumentContext = React.createContext<TypedDocumentContextType>({
  isReady: false,
  loadNavigationMenus: () => { throw new Error("Not implemented") },
  createPerspective: () => { throw new Error("Not implemented") },
  loadPerspectivePageData: () => { throw new Error("Not implemented") },
  savePerspective: () => { throw new Error("Not implemented") },
} as TypedDocumentContextType)

/** C#側と合わせる必要あり */
const SERVER_URL_SUBDIRECTORY = {
  LIST_MEMOS: `/typed-document/list`,
  LOAD_DATA: `/typed-document/load`,
  SAVE_DATA: `/typed-document/save`,
} as const

/** C#側と合わせる必要あり */
type SERVER_API_TYPE_INFO = {
  [SERVER_URL_SUBDIRECTORY.LIST_MEMOS]: {
    body: undefined,
    query: undefined,
    response: [entityId: string, entityName: string][]
  }
  [SERVER_URL_SUBDIRECTORY.LOAD_DATA]: {
    body: undefined,
    query: { typeId: string },
    response: Perspective
  }
  [SERVER_URL_SUBDIRECTORY.SAVE_DATA]: {
    body: Perspective,
    query: undefined,
    response: void
  }
}

/**
 * 型つきドキュメントのコンテキストを提供する。
 * 永続化はlocalStorageに行う。
 */
export const useTypedDocumentContextProvider = (): TypedDocumentContextType => {

  const loadNavigationMenus: TypedDocumentContextType["loadNavigationMenus"] = useEvent(async () => {
    try {
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.LIST_MEMOS}`, {
        method: 'GET',
      })
      if (!response.ok) {
        alert(`一覧を取得できませんでした。(${response.status} ${response.statusText})`)
        return []
      }
      const data: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.LIST_MEMOS]["response"] = await response.json()
      return data.map(item => ({
        id: item[0],
        label: item[1],
        type: "perspective",
      }))
    } catch (error) {
      alert(`一覧を取得できませんでした。(${error})`)
      return []
    }
  })

  const createPerspective: TypedDocumentContextType["createPerspective"] = useEvent(async newPerspective => {
    try {
      const body: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.SAVE_DATA]["body"] = newPerspective
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.SAVE_DATA}`, {
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
      const QUERY_KEY = "typeId" satisfies keyof SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.LOAD_DATA]["query"]
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.LOAD_DATA}?${QUERY_KEY}=${perspectiveId}`, {
        method: 'GET',
      })
      if (!response.ok) {
        alert(`読み込めませんでした。(${response.status} ${response.statusText})`)
        return undefined
      }
      const data: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.LOAD_DATA]["response"] = await response.json()
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
      const body: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.SAVE_DATA]["body"] = data.perspective
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.SAVE_DATA}`, {
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
    loadNavigationMenus,
    createPerspective,
    loadPerspectivePageData,
    savePerspective,
  }
}

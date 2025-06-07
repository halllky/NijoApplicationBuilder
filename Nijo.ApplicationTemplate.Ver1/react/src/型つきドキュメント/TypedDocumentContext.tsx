import * as React from "react"
import useEvent from "react-use-event-hook"
import { Entity, NavigationMenuItem, Perspective, TypedDocumentContextType, PerspectivePageData } from "./types"

/** 型つきドキュメントのコンテキスト。各画面から利用する関数群 */
export const TypedDocumentContext = React.createContext<TypedDocumentContextType>({
  isReady: false,
  loadNavigationMenus: () => { throw new Error("Not implemented") },
  createPerspective: () => { throw new Error("Not implemented") },
  loadPerspectivePageData: () => { throw new Error("Not implemented") },
  savePerspective: () => { throw new Error("Not implemented") },
} as TypedDocumentContextType)


/**
 * 型つきドキュメントのコンテキストを提供する。
 * 永続化はlocalStorageに行う。
 */
export const useTypedDocumentContextProvider = (): TypedDocumentContextType => {

  type LocalStorageDataInternal = {
    menuItems: NavigationMenuItem[]
    perspectives: Perspective[]
  }

  const LOCAL_STORAGE_KEY = "typedDocument"
  const [ready, setReady] = React.useState(false)
  const [localStorageData, setLocalStorageData] = React.useState<LocalStorageDataInternal>({
    menuItems: [],
    perspectives: [],
  })

  // 初回表示時は読み込み
  React.useEffect(() => {
    const storedData = localStorage.getItem(LOCAL_STORAGE_KEY)
    if (storedData) {
      setLocalStorageData(JSON.parse(storedData))
    }
    setReady(true)
  }, [])

  // ステートが更新されるたびにlocalStorageに反映
  React.useEffect(() => {
    if (!ready) return
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(localStorageData))
  }, [localStorageData, ready])

  const loadNavigationMenus: TypedDocumentContextType["loadNavigationMenus"] = useEvent(() => {
    return Promise.resolve(localStorageData.menuItems)
  })

  const createPerspective: TypedDocumentContextType["createPerspective"] = useEvent(newPerspective => {
    setLocalStorageData(prev => ({
      ...prev,
      perspectives: [...prev.perspectives, newPerspective],
      menuItems: [...prev.menuItems, {
        id: newPerspective.perspectiveId,
        label: newPerspective.name,
        type: "perspective",
      }],
    }))
    return Promise.resolve(newPerspective)
  })

  const loadPerspectivePageData: TypedDocumentContextType["loadPerspectivePageData"] = useEvent(perspectiveId => {
    const perspective = localStorageData.perspectives.find(p => p.perspectiveId === perspectiveId)
    if (perspective) {
      return Promise.resolve({ perspective } satisfies PerspectivePageData)
    } else {
      return Promise.resolve(undefined)
    }
  })

  const savePerspective: TypedDocumentContextType["savePerspective"] = useEvent(data => {
    setLocalStorageData(prev => {
      const index = prev.perspectives.findIndex(p => p.perspectiveId === data.perspective.perspectiveId)
      if (index === -1) {
        return {
          ...prev,
          perspectives: [...prev.perspectives, data.perspective],
        }
      } else {
        const newPerspectives = [...prev.perspectives]
        newPerspectives[index] = data.perspective
        return {
          ...prev,
          perspectives: newPerspectives,
        }
      }
    })
    return Promise.resolve()
  })

  return {
    isReady: ready,
    loadNavigationMenus,
    createPerspective,
    loadPerspectivePageData,
    savePerspective,
  }
}

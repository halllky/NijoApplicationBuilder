import * as React from "react"
import useEvent from "react-use-event-hook"
import { Entity, EntityType, NavigationMenuItem, Perspective, TypedDocumentContextType, EntityTypePageData, PerspectivePageData } from "./types"

/** 型つきドキュメントのコンテキスト。各画面から利用する関数群 */
export const TypedDocumentContext = React.createContext<TypedDocumentContextType>({
  loadNavigationMenus: () => { throw new Error("Not implemented") },
  createEntityType: () => { throw new Error("Not implemented") },
  createPerspective: () => { throw new Error("Not implemented") },
  loadEntityTypePageData: () => { throw new Error("Not implemented") },
  saveEntities: () => { throw new Error("Not implemented") },
  tryDeleteEntityType: () => { throw new Error("Not implemented") },
  loadPerspectivePageData: () => { throw new Error("Not implemented") },
  savePerspective: () => { throw new Error("Not implemented") },
} as TypedDocumentContextType)


/**
 * 型つきドキュメントのコンテキストを提供する。
 * 永続化はlocalStorageに行う。
 */
export const useTypedDocumentContextProvider = (): TypedDocumentContextType => {

  type LocalStorageDataInternal = {
    entities: Entity[]
    menuItems: NavigationMenuItem[]
    entityTypes: EntityType[]
    perspectives: Perspective[]
  }

  const LOCAL_STORAGE_KEY = "typedDocument"
  const [ready, setReady] = React.useState(false)
  const [localStorageData, setLocalStorageData] = React.useState<LocalStorageDataInternal>({
    entities: [],
    menuItems: [],
    entityTypes: [],
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

  const createEntityType: TypedDocumentContextType["createEntityType"] = useEvent(newEntityType => {
    setLocalStorageData(prev => ({
      ...prev,
      entityTypes: [...prev.entityTypes, newEntityType],
      menuItems: [...prev.menuItems, {
        id: newEntityType.typeId,
        label: newEntityType.typeName,
        type: "entityType",
      }],
    }))
    return Promise.resolve(newEntityType)
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

  const loadEntityTypePageData: TypedDocumentContextType["loadEntityTypePageData"] = useEvent(entityTypeId => {
    const entityType = localStorageData.entityTypes.find(et => et.typeId === entityTypeId)
    const entities = localStorageData.entities.filter(e => e.entityId === entityTypeId)

    if (entityType) {
      return Promise.resolve({ entityType, entities } satisfies EntityTypePageData)
    } else {
      return Promise.resolve(undefined)
    }
  })

  const saveEntities: TypedDocumentContextType["saveEntities"] = useEvent(async (data) => {
    setLocalStorageData(prev => {
      const beforeSort = [
        ...prev.entities.filter(e => e.entityId !== data.entityType.typeId),
        ...data.entities,
      ]

      // エンティティを型ごとにグルーピングする
      const entitiesByType = beforeSort.reduce((acc, e) => {
        if (!acc.has(e.entityId)) acc.set(e.entityId, [])
        acc.get(e.entityId)!.push(e)
        return acc
      }, new Map<string, Entity[]>())

      // 第1ソートキーは型IDの昇順、第2ソートキーは引数で渡ってきた順番で保存
      const entities = Array.from(entitiesByType.entries()).sort((a, b) => {
        const [typeIdA] = a
        const [typeIdB] = b
        return typeIdA.localeCompare(typeIdB)
      }).flatMap(([, entities]) => entities)

      return { ...prev, entities }
    })
    return Promise.resolve()
  })

  const tryDeleteEntityType: TypedDocumentContextType["tryDeleteEntityType"] = useEvent(entityTypeId => {
    const isInUse = localStorageData.entities.some(e => e.entityId === entityTypeId)
    if (isInUse) return Promise.resolve(false)

    setLocalStorageData(prev => ({
      ...prev,
      entityTypes: prev.entityTypes.filter(et => et.typeId !== entityTypeId),
      menuItems: prev.menuItems.filter(item => item.type !== "entityType" || item.id !== entityTypeId),
    }))
    return Promise.resolve(true)
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
    loadNavigationMenus,
    createEntityType,
    createPerspective,
    loadEntityTypePageData,
    saveEntities,
    tryDeleteEntityType,
    loadPerspectivePageData,
    savePerspective,
  }
}

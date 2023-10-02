import React, { createContext, useCallback, useContext, useEffect, useId, useMemo, useReducer, useRef } from "react"

type RegisteredItem = { tabId: string, controlId: string, ref: React.RefObject<HTMLElement> }
type State = {
  registered: RegisteredItem[]
  active?: RegisteredItem
  lastFocused: Map<string, string>
  getTabs: () => { tabId: string, items: RegisteredItem[] }[]
  getHtmlElements(): HTMLElement[]
  getLastFocusItem: (tabId: string) => RegisteredItem | undefined
}
type Action
  = { type: 'register', item: RegisteredItem }
  | { type: 'unregister', controlId: string }
  | { type: 'activate-first-item', tabId: string }
  | { type: 'move-to-next-tab' }
  | { type: 'move-to-previous-tab' }
  | { type: 'activate-by-id', controlId: string }
  | { type: 'activate-by-element', el: HTMLElement }

const reducer = (state: State, action: Action) => {
  const updated = { ...state }
  if (action.type === 'register') {
    updated.registered = [...updated.registered.filter(x => x.controlId !== action.item.controlId), action.item]

  } else if (action.type === 'unregister') {
    const index = updated.registered.findIndex(x => x.controlId === action.controlId)
    updated.registered = updated.registered.filter(x => x.controlId !== action.controlId)

    // フォーカス中の要素が削除された場合、近くの要素を選択する
    if (updated.active?.controlId === action.controlId) {
      const newItemIndex = Math.min(index, updated.registered.length - 1)
      updated.active = updated.registered[newItemIndex]
    }
  }
  // ---------------------------

  updated.getTabs = () => updated.registered.reduce((list, item) => {
    const index = list.findIndex(x => x.tabId === item.tabId)
    if (index === -1) list.push({ tabId: item.tabId, items: [item] })
    else list[index].items.push(item)
    return list
  }, [] as { tabId: string, items: RegisteredItem[] }[])

  updated.getHtmlElements = () => updated
    .registered
    .map(x => x.ref.current)
    .filter(current => {
      if (current == null) return false
      if (current.offsetParent == null) return false // 自身または親要素のいずれかが非表示ならnullになる
      return true
    }) as HTMLElement[]
    || []

  updated.getLastFocusItem = (tabId: string) => {
    const lastFocusControl = updated.lastFocused.get(tabId)
    if (lastFocusControl === undefined) return undefined
    return updated.registered.find(x => x.controlId === lastFocusControl)
  }

  // ---------------------------
  const activate = (item: RegisteredItem | undefined) => {
    updated.active = item
    item?.ref.current?.focus()
    if (item) updated.lastFocused.set(item.tabId, item.controlId)
  }

  switch (action.type) {
    case 'activate-by-id': {
      activate(updated.registered.find(x => x.controlId === action.controlId))
      break
    }
    case 'activate-by-element': {
      activate(updated.registered.find(x => x.ref.current === action.el))
      break
    }
    case 'move-to-next-tab': {
      const tabs = updated.getTabs()
      const index = updated.active === undefined ? 0 : tabs.findIndex(x => x.tabId === updated.active!.tabId)
      const nextTab = index === tabs.length - 1 ? tabs[0] : tabs[index + 1]
      activate(updated.getLastFocusItem(nextTab.tabId) || nextTab.items[0])
      break
    }
    case 'move-to-previous-tab': {
      const tabs = updated.getTabs()
      const index = updated.active === undefined ? 0 : tabs.findIndex(x => x.tabId === updated.active!.tabId)
      const previousTab = index === 0 ? tabs[tabs.length - 1] : tabs[index - 1]
      activate(updated.getLastFocusItem(previousTab.tabId) || previousTab.items[0])
      break
    }
    case 'activate-first-item': {
      const item = updated
        .getTabs()
        .find(x => x.tabId === action.tabId)
        ?.items[0]
      if (item) activate(item)
      break
    }
    default:
      break
  }

  // 何も選択されていなければ何かにフォーカスを当てる
  if (!updated.active && updated.registered.length > 0) activate(updated.registered[0])

  return updated
}

// -------------------------------------------
// * ページ単位 *
const GlobalFocusContext = createContext(null as unknown as [State, React.Dispatch<Action>])
export const useGlobalFocusContext = () => useContext(GlobalFocusContext)

export const GlobalFocusPage = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  const reducervalue = useReducer(reducer, {
    registered: [],
    lastFocused: new Map(),
    getTabs: () => [],
    getLastFocusItem: () => undefined,
    getHtmlElements: () => [],
  } as State)

  const onKeyDown = useCallback((e: React.KeyboardEvent) => {
    switch (e.key) {
      case 'ArrowUp':
      case 'ArrowDown':
      case 'ArrowLeft':
      case 'ArrowRight': {
        const elements = reducervalue[0].getHtmlElements()
        const nearestEl = findNearestElement(e.key, e.target as HTMLElement, elements)
        if (nearestEl) reducervalue[1]({ type: 'activate-by-element', el: nearestEl })
        e.preventDefault()
        break
      }
      case 'Tab': {
        if (e.shiftKey) reducervalue[1]({ type: 'move-to-previous-tab' })
        else reducervalue[1]({ type: 'move-to-next-tab' })
        e.preventDefault()
        break
      }
      default:
        break
    }
  }, [reducervalue[0].getHtmlElements])

  return (
    <GlobalFocusContext.Provider value={reducervalue}>
      <div className={`w-full h-full outline-none ${className}`} onKeyDown={onKeyDown} tabIndex={0}>
        {children}
      </div>
      <FocusBorder />
    </GlobalFocusContext.Provider>
  )
}

const FocusBorder = () => {
  const ref = useRef<HTMLDivElement>(null)
  const [{ active },] = useContext(GlobalFocusContext)

  const style = useMemo<React.CSSProperties>(() => {
    if (!active?.ref.current) return {}
    const rect = active.ref.current.getBoundingClientRect()
    return {
      top: rect.top + window.scrollY,
      left: rect.left + window.scrollX,
      width: rect.width,
      height: rect.height,
    }
  }, [active])

  return active
    ? (
      <div ref={ref}
        style={style}
        className="absolute pointer-events-none
          outline-black border border-2 border-black
          transition-all duration-100 ease-out
          before:absolute before:border before:border-white before:top-[-3px] before:left-[-3px] before:right-[-3px] before:bottom-[-3px]
          after:absolute after:border after:border-white after:top-0 after:left-0 after:right-0 after:bottom-0">
      </div>
    ) : (
      <></>
    )
}

// -------------------------------------------
// * タブ単位 *

type TabAreaContextValue = { tabId: string }
const TabAreaContext = createContext({} as TabAreaContextValue)

export const TabKeyJumpGroup = ({ id, children }: { id?: string, children?: React.ReactNode }) => {
  const tabIdIfNotProvided = useId()
  const tabId = id ?? tabIdIfNotProvided
  const contextValue = useMemo<TabAreaContextValue>(() => ({ tabId }), [tabId])

  return (
    <TabAreaContext.Provider value={contextValue}>
      {children}
    </TabAreaContext.Provider>
  )
}

// -------------------------------------------
// * コントロール単位 *

export const useFocusTarget = <T extends HTMLElement>(ref: React.RefObject<T>, additional?: {
  onMouseDown?: (e: React.MouseEvent) => void
}) => {
  const [, dispatch] = useContext(GlobalFocusContext)
  const { tabId } = useContext(TabAreaContext)
  const controlId = useId()

  // ページのコンテキストにこのエレメントを登録する
  useEffect(() => {
    dispatch({ type: 'register', item: { tabId, controlId, ref } })
    return () => dispatch({ type: 'unregister', controlId })
  }, [])

  // イベント
  const onMouseDown = useCallback((e: React.MouseEvent) => {
    dispatch({ type: 'activate-by-id', controlId })
    additional?.onMouseDown?.(e)
  }, [controlId, additional?.onMouseDown])

  return {
    onMouseDown,
  }
}

/**
 * 中の要素がinputじゃないとフォーカスが当たらないので注意
 */
export const Focusable = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  const ref = useRef<HTMLLabelElement>(null)
  return (
    <label
      ref={ref}
      {...useFocusTarget(ref)}
      className={`relative inline-block ${className}`}
      tabIndex={0} // キーで移動したとき、jsのfocus()で移動先にフォーカスするにはtabindexが-1のままだと無理
    >
      {children}
    </label>
  )
}

// -------------------------------------------
// * 以下、計算関数など *
type Point = { x: number; y: number }
type Direction = 'ArrowUp' | 'ArrowDown' | 'ArrowLeft' | 'ArrowRight'

/**
 * 指定の方向にある最も近い要素を返します。
 */
function findNearestElement(direction: Direction, thisEl: HTMLElement, targetsList: HTMLElement[]): HTMLElement | undefined {
  let isInRange: (angle: number) => boolean
  if (direction === 'ArrowRight') {
    isInRange = angle => angle >= -45 && angle <= 45
  } else if (direction === 'ArrowUp') {
    isInRange = angle => angle >= -135 && angle <= -45
  } else if (direction === 'ArrowLeft') {
    isInRange = angle => angle <= -135 || angle >= 135
  } else if (direction === 'ArrowDown') {
    isInRange = angle => angle >= 45 && angle <= 135
  } else {
    throw new Error
  }

  const currRect = thisEl?.getBoundingClientRect()
  const targetsWithRect = targetsList
    .map(element => ({ element, rect: element.getBoundingClientRect() }))
    .filter(x => x.element !== thisEl)

  // 上下左右の扇形の範囲内を検索
  const filtered1 = targetsWithRect
    .filter(x => isWithinSectorRange(currRect, x.rect, isInRange))
    .map(x => ({ ...x, distance: calculateDistance(currRect, x.rect) }))
  if (filtered1.length > 0) {
    // 最も直線距離が近い要素に飛ぶ
    filtered1.sort((a, b) => a.distance - b.distance)
    return filtered1[0].element
  }
  return undefined
}

/**
 * 2点間の直線距離を算出する
 */
function calculateDistance(point1: Point, point2: Point): number {
  const dx = point2.x - point1.x
  const dy = point2.y - point1.y
  return Math.sqrt(dx * dx + dy * dy)
}
/**
 * 点bが点aから見てsinceAngleからuntilAngleの範囲内に存在するかどうかを返します。
 * @param a 二次元空間上の座標
 * @param b 二次元空間上の座標
 * @param sinceAngle 閾値となる角度
 * @param untilAngle 閾値となる角度
 * @returns bがaから見てsinceAngleからuntilAngleの範囲内に存在するかどうか
 */
function isWithinSectorRange(a: Point, b: Point, checkAngle: (angle: number) => boolean): boolean {
  const abX = b.x - a.x
  const abY = b.y - a.y
  const angle = Math.atan2(abY, abX) * (180 / Math.PI)
  // console.log(angle)
  return checkAngle(angle)
}

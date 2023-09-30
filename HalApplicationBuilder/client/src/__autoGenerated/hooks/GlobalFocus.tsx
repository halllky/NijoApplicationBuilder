import React, { createContext, useCallback, useContext, useEffect, useId, useMemo, useRef, useState } from "react"
import { UUID } from "uuidjs"

// -------------------------------------------
// * ページ単位 *
type Registered = {
  readonly tabs: string[]
  readonly elements: [tabId: string, element: HTMLElement][]
}
type GlobalFocusContextValue = {
  registered: React.RefObject<Registered>
  activeTab?: String
  activeElement?: HTMLElement
  activate: ({ tab, el }: { tab?: string, el?: HTMLElement }) => void
  moveToNextTab: () => void
  moveToPrevTab: () => void
}
const GlobalFocusContext = createContext({} as GlobalFocusContextValue)

const GlobalFocusPage = ({ children }: { children?: React.ReactNode }) => {
  const registered = useRef<Registered>({ tabs: [], elements: [] })
  const activeTab = useRef<string>()
  const activeElement = useRef<HTMLElement>()
  const forceUpdate = useForceUpdate()

  const activate = useCallback(({ tab, el }: { tab?: string, el?: HTMLElement }) => {
    if (tab === undefined && registered.current.tabs.length > 0) {
      tab = registered.current.tabs[0]
    }
    if (el === undefined) {
      const elements = registered.current.elements.filter(x => x[0] === tab)
      el = elements.length === 0 ? undefined : elements[0][1]
    }
    activeTab.current = tab
    activeElement.current = el
    el?.focus()
    forceUpdate()
  }, [])

  const moveToNextTab = useCallback(() => {
    if (activeTab.current === undefined) { activate({}); return }
    const activeTabIndex = registered.current.tabs.indexOf(activeTab.current)
    if (activeTabIndex === -1) { activate({}); return }
    const nextTabIndex = activeTabIndex === registered.current.tabs.length - 1
      ? 0
      : (activeTabIndex + 1)
    activate({ tab: registered.current.tabs[nextTabIndex] })
  }, [])

  const moveToPrevTab = useCallback(() => {
    if (activeTab.current === undefined) { activate({}); return }
    const activeTabIndex = registered.current.tabs.indexOf(activeTab.current)
    if (activeTabIndex === -1) { activate({}); return }
    const nextTabIndex = activeTabIndex === 0
      ? registered.current.tabs.length - 1
      : (activeTabIndex - 1)
    activate({ tab: registered.current.tabs[nextTabIndex] })
  }, [])

  return (
    <GlobalFocusContext.Provider value={{
      registered,
      activeTab: activeTab.current,
      activeElement: activeElement.current,
      activate,
      moveToNextTab,
      moveToPrevTab,
    }}>
      {children}
      <FocusBorder />
    </GlobalFocusContext.Provider>
  )
}

const FocusBorder = () => {
  const ref = useRef<HTMLDivElement>(null)
  const { activeElement } = useContext(GlobalFocusContext)

  // この要素をactiveElementの子の位置に移動する。
  // 同じ1つのDOMを移動させることでCSSアニメーションを効かせるため
  useEffect(() => {
    if (!ref.current || !activeElement) return
    activeElement.appendChild(ref.current)
  }, [activeElement])

  return activeElement
    ? (
      <div ref={ref} className="absolute top-0 bottom-0 left-0 right-0
        pointer-events-none border-2 border-orange-600"></div>
    ) : (
      <></>
    )
}

// -------------------------------------------
// * タブ単位 *

type TabAreaContextValue = { tabId: string }
const TabAreaContext = createContext({} as TabAreaContextValue)

const TabArea = ({ children }: { children?: React.ReactNode }) => {
  const tabId = useId()
  const contextValue = useMemo<TabAreaContextValue>(() => ({ tabId }), [tabId])

  // ページのコンテキストにこのタブを登録する
  const { registered } = useContext(GlobalFocusContext)
  useEffect(() => {
    registered.current?.tabs.push(tabId)
    return () => {
      const index = registered.current?.tabs.indexOf(tabId)
      if (index === -1 || index === undefined) return
      registered.current?.tabs.splice(index, 1)
    }
  }, [tabId])

  return (
    <TabAreaContext.Provider value={contextValue}>
      {children}
    </TabAreaContext.Provider>
  )
}

// -------------------------------------------
// * コントロール単位 *

const Focusable = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  const { registered, activate, moveToNextTab, moveToPrevTab } = useContext(GlobalFocusContext)
  const { tabId } = useContext(TabAreaContext)

  const ref = useRef<HTMLLabelElement>(null)

  // ページのコンテキストにこのエレメントを登録する
  useEffect(() => {
    if (ref.current) {
      registered.current?.elements.push([tabId, ref.current])
    }
    return () => {
      if (ref.current) {
        const index = registered.current?.elements.findIndex(x => x[1] === ref.current)
        if (index === -1 || index === undefined) return
        registered.current?.elements.splice(index, 1)
      }
    }
  }, [tabId])

  // フォーカスがあたったとき
  const activateElement = useCallback((el: HTMLElement) => {
    activate({ tab: tabId, el })
  }, [tabId])

  // イベント
  const onClick = useCallback((e: React.MouseEvent) => {
    if (ref.current) activateElement(ref.current)
    e.preventDefault()
  }, [ref.current, activateElement])

  const onKeyDown = useCallback((e: React.KeyboardEvent) => {
    switch (e.key) {
      case 'ArrowUp':
      case 'ArrowDown':
      case 'ArrowLeft':
      case 'ArrowRight':
        if (!ref.current) return
        const elements = registered.current?.elements
          .filter(x => x[0] === tabId)
          .map(x => x[1])
          || []
        const nearestEl = findNearestElement(e.key, ref.current, elements)
        if (!nearestEl) return
        activateElement(nearestEl)
        e.preventDefault()
        break

      case 'Tab':
        if (e.shiftKey) {
          moveToPrevTab()
        } else {
          moveToNextTab()
        }
        e.preventDefault()
        break

      default:
        break
    }
  }, [tabId])

  return (
    <label
      ref={ref}
      onClick={onClick}
      onKeyDown={onKeyDown}
      className={`relative inline-block ${className}`}
    >
      {children}
    </label>
  )
}

// -------------------------------------------
// * 以下、計算関数など *
const useForceUpdate = () => {
  const [state, setState] = useState(UUID.generate())
  return useCallback(() => {
    setState(UUID.generate())
  }, [])
}

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

export default {
  GlobalFocusPage,
  TabArea,
  Focusable,
}

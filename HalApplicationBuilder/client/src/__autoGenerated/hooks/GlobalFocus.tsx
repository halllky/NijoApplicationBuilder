import React, { CSSProperties, createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react"

type KeyTravelContextValue = {
  registered: HTMLElement[]
  focusTarget?: HTMLElement
  setFocusTarget: (el: HTMLElement) => void
}
const KeyTravelContext = createContext({} as KeyTravelContextValue)

const ContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const [currentFocus, setCurrentFocus] = useState<HTMLElement | undefined>()
  const registered = useMemo(() => {
    return [] as HTMLElement[]
  }, [])
  const contextValue = useMemo<KeyTravelContextValue>(() => ({
    registered,
    focusTarget: currentFocus,
    setFocusTarget: setCurrentFocus,
  }), [registered, currentFocus, setCurrentFocus])

  return (
    <KeyTravelContext.Provider value={contextValue}>
      {children}
      <CurrentFocus focusTarget={currentFocus} />
    </KeyTravelContext.Provider>
  )
}

/**
 * 多分こうするより各フォーカスターゲットの中に現れたり消えたりする仕組みにした方がよい
 * フォーカス当たってるときに画面サイズかえられると追従できないので
 */
const CurrentFocus = (props: {
  focusTarget?: HTMLElement
}) => {
  const style = useMemo<CSSProperties>(() => {
    const rect = props.focusTarget?.getBoundingClientRect()
    return {
      zIndex: 10,
      top: (rect?.top ?? 0) + window.scrollY,
      left: (rect?.left ?? 0) + window.scrollX,
      width: rect?.width,
      height: rect?.height,
      transition: `all .1s ease-out 0s`,
    }
  }, [props.focusTarget])

  return (
    <div
      className="absolute pointer-events-none border-2 border-orange-600"
      style={style}></div>
  )
}

// -------------------------------------------
const Focusable = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  const { focusTarget, setFocusTarget, registered } = useContext(KeyTravelContext)

  const onClick = useCallback((e: React.MouseEvent) => {
    setFocusTarget(e.target as HTMLElement)
    e.preventDefault()
  }, [setFocusTarget])

  const onKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (!focusTarget) {
      if (registered.length > 0) setFocusTarget(registered[0])
      return
    }
    let nearestEl: HTMLElement | undefined
    switch (e.key) {
      case 'ArrowUp': nearestEl = findNearestElement('top', focusTarget, registered); break
      case 'ArrowDown': nearestEl = findNearestElement('bottom', focusTarget, registered); break
      case 'ArrowLeft': nearestEl = findNearestElement('left', focusTarget, registered); break
      case 'ArrowRight': nearestEl = findNearestElement('right', focusTarget, registered); break
      default: throw new Error()
    }
    if (!nearestEl) return
    setFocusTarget(nearestEl)
    nearestEl.focus()
    e.preventDefault()
  }, [focusTarget, registered, setFocusTarget])

  const ref = useRef<HTMLLabelElement>(null)
  useEffect(() => {
    if (ref.current) {
      registered.push(ref.current)
    }
    return () => {
      if (ref.current) {
        const index = registered.indexOf(ref.current)
        if (index !== -1) registered.splice(index, 1)
      }
    }
  }, [ref.current])

  return (
    <label ref={ref} onClick={onClick} onKeyDown={onKeyDown} className={className}>
      {children}
    </label>
  )
}

// -------------------------------------------
type Point = { x: number; y: number }
type Direction = 'left' | 'top' | 'right' | 'bottom'

/**
 * 指定の方向にある最も近い要素を返します。
 */
function findNearestElement(direction: Direction, thisEl: HTMLElement, targetsList: HTMLElement[]): HTMLElement | undefined {
  let isInRange: (angle: number) => boolean
  if (direction === 'right') {
    isInRange = angle => angle >= -45 && angle <= 45
  } else if (direction === 'top') {
    isInRange = angle => angle >= -135 && angle <= -45
  } else if (direction === 'left') {
    isInRange = angle => angle <= -135 || angle >= 135
  } else if (direction === 'bottom') {
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
  ContextProvider,
  Focusable,
}

import React, { createContext, useCallback, useContext, useEffect, useId, useMemo, useReducer, useRef, useState } from "react"
import { useIMEOpened } from "."

type RegisteredItem = {
  tabId: string
  controlId: string
  ref: React.RefObject<HTMLElement>
  borderHidden?: true
}
type TabGroup = { tabId: string, items: RegisteredItem[] }
type State = {
  registered: RegisteredItem[]
  active?: RegisteredItem
  lastFocused: Map<string, string>
  tabGroups: TabGroup[]
  tabMovingDirection?: 'prev' | 'next'
  editingControlId?: string
}
type Action
  = { type: 'register', item: RegisteredItem }
  | { type: 'unregister', controlId: string }
  | { type: 'activate-first-item', tabId: string }
  | { type: 'move-to-next-tab' }
  | { type: 'move-to-previous-tab' }
  | { type: 'skip' }
  | { type: 'activate-by-id', controlId: string }
  | { type: 'activate-by-element', el: HTMLElement }
  | { type: 'start-editing', controlId: string }
  | { type: 'end-editing' }

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

  updated.tabGroups = updated.registered.reduce((list, item) => {
    if (list.length === 0 || list[list.length - 1].tabId !== item.tabId) {
      list.push({ tabId: item.tabId, items: [item] })
    } else {
      list[list.length - 1].items.push(item)
    }
    return list
  }, [] as { tabId: string, items: RegisteredItem[] }[])

  // ---------------------------
  const activate = (item: RegisteredItem | undefined) => {
    updated.active = item
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
    case 'move-to-next-tab':
    case 'move-to-previous-tab':
    case 'skip': {
      const index = updated.active
        ? updated.tabGroups.findIndex(x => x.items.includes(updated.active!))
        : 0
      if (action.type === 'move-to-next-tab' || (action.type === 'skip' && updated.tabMovingDirection === 'next')) {
        const nextGroup = index === updated.tabGroups.length - 1
          ? updated.tabGroups[0]
          : updated.tabGroups[index + 1]
        activate(nextGroup.items[0])
        updated.tabMovingDirection = 'next'
      } else {
        const previousGroup = index === 0
          ? updated.tabGroups[updated.tabGroups.length - 1]
          : updated.tabGroups[index - 1]
        activate(previousGroup.items[previousGroup.items.length - 1])
        updated.tabMovingDirection = 'prev'
      }
      break
    }
    case 'activate-first-item': {
      const group = updated.tabGroups.find(x => x.tabId === action.tabId)
      if (group) {
        const visibleItems = getVisibleItems(group)
        if (visibleItems.length > 0) activate(visibleItems[0])
      }
      break
    }
    case 'start-editing': {
      updated.editingControlId = action.controlId
      break
    }
    case 'end-editing': {
      delete updated.editingControlId
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
    tabGroups: [],
    lastFocused: new Map(),
    getTabGroups: () => [],
  } as State)

  // activeが移動したらそのエレメントにフォーカスを当てる
  useEffect(() => {
    reducervalue[0].active?.ref.current?.focus()
  }, [reducervalue[0].active])

  const onKeyDown = useCallback((e: React.KeyboardEvent) => {
    const state = reducervalue[0]
    const dispatch = reducervalue[1]

    // 編集中は制御しない
    if (state.editingControlId) return

    switch (e.key) {
      case 'ArrowUp':
      case 'ArrowDown':
      case 'ArrowLeft':
      case 'ArrowRight': {
        if (!state.active) break
        if ((e.target as HTMLElement).closest('.ag-theme-alpine') != null) break //ag-gridのキー制御と衝突するので

        const group = state.tabGroups.find(x => x.items.includes(state.active!))
        if (!group) break

        const elements = getVisibleItems(group)
          .filter(x => x !== state.active && x.ref.current)
          .map(x => x.ref.current as HTMLElement)
        const nearestEl = findNearestElement(e.key, e.target as HTMLElement, elements)
        if (nearestEl) dispatch({ type: 'activate-by-element', el: nearestEl })
        e.preventDefault()
        break
      }
      case 'Tab': {
        if (e.shiftKey) dispatch({ type: 'move-to-previous-tab' })
        else dispatch({ type: 'move-to-next-tab' })
        e.preventDefault()
        break
      }
      default:
        break
    }
  }, [reducervalue[0].tabGroups])

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
        className={`absolute pointer-events-none
          outline-none border border-2 border-color-12
          transition-all duration-100 ease-out
          ${active.borderHidden ? 'hidden' : ''}`}>
        <div className="border border-color-0 absolute top-0 left-0 right-0 bottom-0"></div>
        <div className="border border-color-0 absolute top-[-3px] left-[-3px] right-[-3px] bottom-[-3px]"></div>
      </div>
    ) : (
      <></>
    )
}

// -------------------------------------------
// * タブ単位 *

type TabAreaContextValue = { tabId: string }
const TabAreaContext = createContext({} as TabAreaContextValue)

export const TabKeyJumpGroup = ({ id, children }: {
  id?: string
  children?: React.ReactNode
  onFocus?: () => void
}) => {
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
type UseFocusTargetOptions = {
  tabId?: string
  borderHidden?: true
  onMouseDown?: (e: React.MouseEvent) => void
  editable?: true
  onStartEditing?: () => void
  onEndEditing?: () => void
}
export const useFocusTarget = <T extends HTMLElement>(ref: React.RefObject<T>, options?: UseFocusTargetOptions) => {
  const [, dispatch] = useContext(GlobalFocusContext)
  const { tabId } = useContext(TabAreaContext)
  const controlId = useId()

  // ページのコンテキストにこのエレメントを登録する
  useEffect(() => {
    dispatch({
      type: 'register', item: {
        tabId: options?.tabId ?? tabId,
        controlId,
        ref,
        borderHidden: options?.borderHidden,
      }
    })
    return () => dispatch({ type: 'unregister', controlId })
  }, [])

  // 編集
  const editing = useEditing(ref, controlId, options)

  // イベント
  const onMouseDown = useCallback((e: React.MouseEvent) => {
    dispatch({ type: 'activate-by-id', controlId })
    options?.onMouseDown?.(e)
  }, [controlId, options?.onMouseDown])

  return {
    globalFocusEvents: {
      onMouseDown,
    },
    ...editing,
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
// 編集
const useEditing = <T extends HTMLElement>(ref: React.RefObject<T>, controlId: string, options?: UseFocusTargetOptions) => {
  const [, dispatch] = useGlobalFocusContext()

  const onEndEditingRef = useRef(options?.onEndEditing || null)
  const [currentEditing, setCurrentEditing] = useState<{ controlId: string, onEndEditingRef: typeof onEndEditingRef } | null>(null)

  // テキストボックスでEscを押したときに編集前の値に戻すための一時変数
  const [beforeEdit, setBeforeEdit] = useState('')

  // ------------------------------------------------------
  const isEditing = useMemo((): boolean => {
    return currentEditing?.controlId === controlId
  }, [controlId, currentEditing])

  const startEditing = useCallback(() => {
    if (!options?.editable) return
    currentEditing?.onEndEditingRef.current?.()
    setCurrentEditing({ controlId, onEndEditingRef })
    dispatch({ type: 'start-editing', controlId })
    options?.onStartEditing?.()

    // テキストボックス用
    setBeforeEdit((ref.current as HTMLInputElement | null)?.value ?? '')
  }, [options?.editable, controlId, currentEditing, options?.onStartEditing])

  const endEditing = useCallback(() => {
    if (!options?.editable) return
    if (currentEditing?.controlId !== controlId) return
    setCurrentEditing(null)
    dispatch({ type: 'end-editing' })
    options?.onEndEditing?.()
  }, [options?.editable, controlId, currentEditing, options?.onEndEditing])

  // -------------- テキストボックス用 --------------
  const ime = useIMEOpened()
  const onTextBoxKeyDown = useCallback((e: React.KeyboardEvent) => {
    // IME展開中は制御しない
    if (ime) return

    // 読み取り専用なら制御しない
    if (!options?.editable) return

    if (isEditing) {
      if (e.key === 'Escape') {
        // 編集前の値に戻す
        if (ref.current) (ref.current as unknown as HTMLInputElement).value = beforeEdit
        endEditing()
        e.preventDefault()

      } else if (e.key === 'Enter'
        || e.key === 'Tab') {
        endEditing()
        e.preventDefault()
      }
    } else {
      if (e.key.length === 1 // 文字か数字
        || e.key === 'Enter'
        || e.key === 'Space'
        || e.key === 'F2') {
        startEditing()
      }
    }
  }, [options?.editable, isEditing, ime, beforeEdit])

  // -------------------------------------------------------

  return {
    isEditing,
    startEditing,
    endEditing,
    textBoxEditEvents: {
      onDoubleClick: startEditing,
      onBlur: endEditing,
      onKeyDown: onTextBoxKeyDown,
    },
  }
}

// -------------------------------------------
// * 二次元計算 *
const getVisibleItems = (group: TabGroup): RegisteredItem[] => {
  return group.items.filter(x => {
    if (x.ref.current == null) return false
    if (x.ref.current.offsetParent == null) return false // 自身または親要素のいずれかが非表示ならnullになる
    return true
  })
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

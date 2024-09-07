import React, { useCallback, useEffect, useReducer } from "react"
import { useBeforeUnload } from "react-router-dom"

// useReducerの簡略化
type ReducerDef<S, M extends StateModifier<S>> = (state: S) => M
type StateModifier<S> = { [action: string]: (...args: any[]) => S }
type DispatchArg<S, M extends StateModifier<S>> = (modifier: M) => S
export const defineReducer = <S, M extends StateModifier<S>>(
  reducerDef: ReducerDef<S, M>
): React.Reducer<S, DispatchArg<S, M>> => {
  return (state, action) => {
    const modifier = reducerDef(state)
    const newState = action(modifier)
    return newState
  }
}

// ファイルを超えてDispatcherの型を推論したいことがあるので
export type DispatcherOf<TReducer>
  = TReducer extends React.Reducer<any, DispatchArg<any, infer TModifier>>
  ? Dispatcher<TModifier>
  : never
type Dispatcher<TModifier>
  = TModifier extends StateModifier<infer TState>
  ? ((modifier: DispatchArg<TState, TModifier>) => void)
  : never

// useContextの簡略化
type ReducerEx<S, M extends StateModifier<S>> = React.Reducer<S, DispatchArg<S, M>>
type ContextEx<S, M extends StateModifier<S>> = React.Context<readonly [S, React.Dispatch<DispatchArg<S, M>>]>
type ProviderComponentDefaultProp = { children?: React.ReactNode }
type ProviderComponent<P extends ProviderComponentDefaultProp> = (props: P) => JSX.Element
/** TODO: 有用性に疑問 */
export const defineContext = <S, M extends StateModifier<S>, P extends ProviderComponentDefaultProp = ProviderComponentDefaultProp>(
  getInitialState: () => S,
  reducerDef: ReducerDef<S, M>,
  craeteProviderContext?: (Context: ContextEx<S, M>, reducer: ReducerEx<S, M>) => ProviderComponent<P>
) => {
  const reducer = defineReducer(reducerDef)
  const dummyDispatcher = (() => { }) as React.Dispatch<DispatchArg<S, M>>
  const ContextEx = React.createContext([getInitialState(), dummyDispatcher] as const)
  /** App直下などに置く必要あり */
  const ContextProvider: ProviderComponent<P>
    = craeteProviderContext?.(ContextEx, reducer)
    // 既定のコンテキストプロバイダー
    ?? (({ children }) => {
      const contextValue = React.useReducer(reducer, getInitialState())
      return (
        <ContextEx.Provider value={contextValue}>
          {children}
        </ContextEx.Provider>
      )
    })

  /** コンテキスト使用側はこれを使う */
  const useContextEx = () => React.useContext(ContextEx)

  return [ContextProvider, useContextEx] as const
}

/** TODO: 有用性に疑問 */
export const defineContext2 = <S, M extends StateModifier<S>>(
  getInitialState: () => S,
  reducerDef: ReducerDef<S, M>,
) => {
  const dummyDispatcher = (() => { }) as React.Dispatch<DispatchArg<S, M>>
  const ContextEx = React.createContext([getInitialState(), dummyDispatcher] as const)
  return {
    reducer: defineReducer(reducerDef),
    ContextProvider: ContextEx.Provider,
    useContext: () => React.useContext(ContextEx),
  }
}

/** forwardRefの戻り値の型定義がややこしいので単純化するためのラッピング関数 */
export const forwardRefEx = <TRef, TProps>(
  fn: (props: TProps, ref: React.ForwardedRef<TRef>) => React.ReactNode
) => {
  return React.forwardRef(fn) as ForwardedRefEx<TRef, TProps>
}
export type ForwardedRefEx<TRef, TProps> = (props: React.PropsWithoutRef<TProps> & { ref?: React.Ref<TRef> }) => React.ReactNode

// --------------------------------------------------
// トグル
const toggleReducer = defineReducer((state: boolean) => ({
  toggle: () => !state,
  setValue: (v: boolean) => v,
}))
export const useToggle = (initialState?: boolean) => {
  return React.useReducer(toggleReducer, initialState ?? false)
}

// --------------------------------------------------
/** コンポーネントの外側のクリックの検知 */
export const useOutsideClick = (ref: React.RefObject<HTMLElement | null>, onOutsideClick: () => void, deps: React.DependencyList) => {
  const handleOutsideClick = useCallback(onOutsideClick, deps)

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (!ref.current) return
      if (ref.current.contains(e.target as HTMLElement)) return
      handleOutsideClick()
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [ref, handleOutsideClick])
}

// --------------------------------------------------
// 配列ref
export const useRefArray = <T,>(arr: any[]) => {
  const elementRefs = React.useRef<React.RefObject<T>[]>([])
  for (let i = 0; i < arr.length; i++) {
    elementRefs.current[i] = React.createRef()
  }
  return elementRefs
}

// --------------------------------------------------
// 配列選択
type SelectionState = {
  cursor: number | undefined
  selected: Set<number>
  isSelected: (index: number) => boolean
}
export const useListSelection = <T extends HTMLElement = HTMLElement>(arr: any[], elementRefs?: React.MutableRefObject<React.RefObject<T>[]>) => {
  const [{ cursor, isSelected }, dispatchSelection] = useReducer(selectionReducer, undefined, (): SelectionState => ({
    cursor: undefined,
    selected: new Set(),
    isSelected: () => false,
  }))

  const handleKeyNavigation: React.KeyboardEventHandler<HTMLElement> = useCallback(e => {
    if (e.key === 'ArrowDown') {
      let nextIndex: number
      if (cursor === undefined) {
        nextIndex = 0
      } else if (cursor >= arr.length - 1) {
        nextIndex = 0
      } else {
        nextIndex = cursor + 1
      }
      dispatchSelection(x => x.selectOne(nextIndex))
      elementRefs?.current[nextIndex].current?.scrollIntoView({ block: 'nearest' })
      e.preventDefault()

    } else if (e.key === 'ArrowUp') {
      let prevIndex: number
      if (cursor === undefined) {
        prevIndex = 0
      } else if (cursor <= 0) {
        prevIndex = arr.length - 1
      } else {
        prevIndex = cursor - 1
      }
      dispatchSelection(x => x.selectOne(prevIndex))
      elementRefs?.current[prevIndex].current?.scrollIntoView({ block: 'nearest' })
      e.preventDefault()
    }
  }, [cursor, arr, elementRefs])

  return {
    activeItemIndex: cursor,
    isSelected,
    dispatchSelection,
    handleKeyNavigation,
  }
}
const selectionReducer = defineReducer((state: SelectionState) => ({
  selectOne: (index: number) => ({
    ...state,
    cursor: index,
    selected: new Set([index]),
    isSelected: i => i === index,
  }),
  selectAll: () => ({
    ...state,
    cursor: undefined,
    selected: new Set<number>(),
    isSelected: () => true,
  }),
}))

// --------------------------------------------------
/** ページ離脱時の確認メッセージ */
export const usePageOutPrompt = (block: boolean = true) => {
  const handleBeforeUnload = useCallback((e: BeforeUnloadEvent) => {
    if (block) e.preventDefault()
  }, [block])
  useBeforeUnload(handleBeforeUnload)
}

// --------------------------------------------------
/**
 * 引数の値が変更されてから一定時間が経過した後に戻り値の値が切り替わります。
 * 例えばグリッドの一覧で選択されている行により画面上の別の個所の表示が変わるような場合に、
 * グリッドの選択行をカーソルキーで高速で切り替えると再レンダリングが頻繁に走ってパフォーマンスが落ちるような場合に使います。
 */
export const useDebounce = <T,>(value: T, delay: number) => {
  const [debouncedValue, setDebouncedValue] = React.useState(value)
  const [debouncing, setDebouncing] = React.useState(false)
  const timerRef = React.useRef<NodeJS.Timeout | null>(null)

  React.useEffect(() => {
    setDebouncing(true)
    // タイマーをクリアして新しいタイマーを設定
    if (timerRef.current) {
      clearTimeout(timerRef.current)
    }
    timerRef.current = setTimeout(() => {
      setDebouncedValue(value)
      setDebouncing(false)
    }, delay)

    // クリーンアップ関数でタイマーをクリア
    return () => {
      if (timerRef.current) {
        clearTimeout(timerRef.current)
      }
    }
  }, [value, delay])

  return { debouncedValue, debouncing }
}

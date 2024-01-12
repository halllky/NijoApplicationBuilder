import React from "react"

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
type ProviderComponent = (props: { children?: React.ReactNode }) => JSX.Element
export const defineContext = <S, M extends StateModifier<S>>(
  getInitialState: () => S,
  reducerDef: ReducerDef<S, M>,
  craeteProviderContext?: (Context: ContextEx<S, M>, reducer: ReducerEx<S, M>) => ProviderComponent
) => {
  const reducer = defineReducer(reducerDef)
  const dummyDispatcher = (() => { }) as React.Dispatch<DispatchArg<S, M>>
  const ContextEx = React.createContext([getInitialState(), dummyDispatcher] as const)
  /** App直下などに置く必要あり */
  const ContextProvider: ProviderComponent
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

/** forwardRefの戻り値の型定義がややこしいので単純化するためのラッピング関数 */
export const forwardRefEx = <TRef, TProps>(
  fn: (props: TProps, ref: React.ForwardedRef<TRef>) => React.ReactNode
) => {
  return React.forwardRef(fn) as (
    (props: React.PropsWithoutRef<TProps> & { ref?: React.Ref<TRef> }) => React.ReactNode
  )
}

// 強制アップデート
const forceUpdateReducer = (state: boolean, _?: undefined) => {
  return !state
}
export const useForceUpdate = () => {
  const [forceUpdateValue, triggerForceUpdate] = React.useReducer(forceUpdateReducer, false)
  return { forceUpdateValue, triggerForceUpdate }
}

// トグル
const toggleReducer = defineReducer((state: boolean) => ({
  toggle: () => !state,
  setValue: (v: boolean) => v,
}))
export const useToggle = (initialState?: boolean) => {
  return React.useReducer(toggleReducer, initialState ?? false)
}

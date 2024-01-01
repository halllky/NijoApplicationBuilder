import { ButtonHTMLAttributes, Dispatch, InputHTMLAttributes, PropsWithoutRef, Reducer, TextareaHTMLAttributes, createContext, forwardRef, useContext, useReducer } from 'react'

/** forwardRefの戻り値の型定義がややこしいので単純化するためのラッピング関数 */
export const forwardRefEx = <TRef, TProps>(
  fn: (props: TProps, ref: React.ForwardedRef<TRef>) => React.ReactNode
) => {
  return forwardRef(fn) as (
    (props: PropsWithoutRef<TProps> & { ref?: React.Ref<TRef> }) => React.ReactNode
  )
}
/** ラベルつきinputの詳細設定 */
type InputWithLabelAttributes = {
  labelText?: string
  labelClassName?: string
  inputClassName?: string
}

export const Text = forwardRefEx<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & InputWithLabelAttributes>((props, ref) => {
  const {
    labelText,
    labelClassName,
    inputClassName,
    className,
    autoComplete,
    ...rest
  } = props

  return (
    <label className={`flex ${className}`}>
      {(labelText || labelClassName) && (
        <span className={`select-none ${labelClassName}`}>
          {labelText}
        </span>)}
      <input ref={ref} {...rest}
        className={`flex-1 border border-1 border-slate-400 px-1 ${inputClassName}`}
        autoComplete={autoComplete ?? 'off'}
      />
    </label>
  )
})

export const Textarea = forwardRefEx<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement> & InputWithLabelAttributes>((props, ref) => {
  const {
    className,
    labelText,
    labelClassName,
    inputClassName,
    ...rest
  } = props

  return (
    <label className={`flex ${className}`}>
      {(labelText || labelClassName) && (
        <span className={`select-none ${labelClassName}`}>
          {labelText}
        </span>)}
      <textarea ref={ref} {...rest}
        className={`flex-1 border border-1 border-slate-400 px-1 ${inputClassName}`}
      ></textarea>
    </label>)
})

type ButtonAttrs = {
  submit?: boolean
}
export const Button = forwardRefEx<HTMLButtonElement, ButtonHTMLAttributes<HTMLButtonElement> & ButtonAttrs>((props, ref) => {
  const {
    type,
    submit,
    className,
    ...rest
  } = props

  return (
    <button ref={ref} {...rest}
      type={type ?? (submit ? 'submit' : 'button')}
      className={`text-white bg-slate-500 border border-1 border-slate-700 px-1 ${className}`}
    ></button>
  )
})

export const Separator = () => {
  return (
    <hr className="bg-slate-300 border-none h-[1px] m-2" />
  )
}


// --------------------------------------------------
// 状態の型定義からreducer等の型定義をするのを簡略化するための仕組み
type ActionParam<TState, TKey extends keyof TState = keyof TState> = {
  update: TKey
  value: TState[TKey]
}
type FlatObjectContext<TState> = React.Context<[TState, Dispatch<ActionParam<TState>>]>

const reducerForFlatObject: Reducer<{}, ActionParam<{}>> = (state, action) => {
  return { ...state, [action.update]: action.value }
}
export const createContextForFlatObject = <TState,>(defaultValue: TState): FlatObjectContext<TState> => {
  return createContext([
    defaultValue,
    (() => { }) as Dispatch<ActionParam<TState>>
  ] as const)
}
export const useContextForFlatObject = <TState,>(ctx: FlatObjectContext<TState>) => {
  return useContext(ctx) as [
    TState,
    <TKey extends keyof TState>(action: ActionParam<TState, TKey>) => TState
  ]
}
export const useReducerForFlatObject = <TState,>(initialState: TState) => {
  return useReducer(
    reducerForFlatObject as unknown as Reducer<TState, ActionParam<TState>>,
    initialState) as [
      TState,
      <TKey extends keyof TState>(action: ActionParam<TState, TKey>) => TState
    ]
}

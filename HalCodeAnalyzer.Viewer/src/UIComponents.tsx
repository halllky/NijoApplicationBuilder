import { ButtonHTMLAttributes, InputHTMLAttributes, PropsWithoutRef, TextareaHTMLAttributes, forwardRef } from 'react'

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

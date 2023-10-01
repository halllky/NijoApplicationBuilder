import { useCallback, useRef, forwardRef, useImperativeHandle, ForwardedRef, InputHTMLAttributes, TextareaHTMLAttributes } from "react"
import { useFocusTarget } from "../hooks"

const selectAll = (e: React.FocusEvent<HTMLInputElement>) => {
  e.target.select()
}

export const Word = forwardRef((props: InputHTMLAttributes<HTMLInputElement>, ref: ForwardedRef<HTMLInputElement | null>) => {
  // react-hook-formでregisterを使うにはforwardRefが必須。またフォーカスの制御にもuseRefが必須。
  // useImperativeHandleを使って通常のuseRefを親コンポーネントに渡すことで両立している
  const inputRef = useRef<HTMLInputElement>(null)
  useImperativeHandle(ref, () => inputRef.current!)

  const className = props.readOnly
    ? `w-full outline-none px-1 cursor-default ${props.className}`
    : `w-full outline-none px-1 border border-neutral-400 ${props.className}`
  return (
    <input
      onFocus={selectAll}
      {...props}
      ref={inputRef}
      type="text"
      className={className}
      autoComplete="off"
      spellCheck={false}
      {...useFocusTarget(inputRef)}
    />
  )
})

export const Description = forwardRef((props: TextareaHTMLAttributes<HTMLTextAreaElement>, ref: ForwardedRef<HTMLTextAreaElement>) => {
  // react-hook-formでregisterを使うにはforwardRefが必須。またフォーカスの制御にもuseRefが必須。
  // useImperativeHandleを使って通常のuseRefを親コンポーネントに渡すことで両立している
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  useImperativeHandle(ref, () => textareaRef.current!)

  const className = props.readOnly
    ? `block w-full outline-none px-1 cursor-default ${props.className}`
    : `block w-full outline-none px-1 border border-neutral-400 ${props.className}`
  const selectIfReadOnly = useCallback((e: React.FocusEvent<HTMLTextAreaElement>) => {
    if (props.readOnly) e.target.select()
  }, [props.readOnly])

  return (
    <textarea
      {...props}
      ref={textareaRef}
      className={className}
      autoComplete="off"
      spellCheck={false}
      rows={props.rows || 3}
      onFocus={selectIfReadOnly}
      {...useFocusTarget(textareaRef)}
    />
  )
})

export const Num = () => {

}

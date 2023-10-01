import { useCallback, useRef, forwardRef, useImperativeHandle, ForwardedRef, InputHTMLAttributes, TextareaHTMLAttributes } from "react"
import { useFocusTarget } from "../hooks"

const selectAll = (e: React.FocusEvent<HTMLInputElement>) => {
  e.target.select()
}

export const Word = forwardRef((props: InputHTMLAttributes<HTMLInputElement>, ref: ForwardedRef<HTMLInputElement | null>) => {
  const inputRef = useRef<HTMLInputElement>(null)
  useImperativeHandle(ref, () => inputRef.current!)

  const className = props.readOnly
    ? `w-full cursor-default outline-none px-1 ${props.className}`
    : `w-full border border-neutral-400 px-1 ${props.className}`
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
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  useImperativeHandle(ref, () => textareaRef.current!)

  const className = props.readOnly
    ? `block w-full cursor-default outline-none px-1 ${props.className}`
    : `block w-full border border-neutral-400 px-1 ${props.className}`
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

import { useCallback, forwardRef, ForwardedRef, InputHTMLAttributes, TextareaHTMLAttributes } from "react"

const selectAll = (e: React.FocusEvent<HTMLInputElement>) => {
  e.target.select()
}

export const Word = forwardRef((props: InputHTMLAttributes<HTMLInputElement>, ref: ForwardedRef<HTMLInputElement>) => {
  const className = `border border-neutral-400 px-1 ${props.className}`
  return (
    <input
      onFocus={selectAll}
      {...props}
      ref={ref}
      type="text"
      className={className}
      autoComplete="off"
      spellCheck={false}
    />
  )
})

export const Description = forwardRef((props: TextareaHTMLAttributes<HTMLTextAreaElement>, ref: ForwardedRef<HTMLTextAreaElement>) => {
  const className = `border border-neutral-400 px-1 ${props.className}`
  const selectIfReadOnly = useCallback((e: React.FocusEvent<HTMLTextAreaElement>) => {
    if (props.readOnly) e.target.select()
  }, [props.readOnly])

  return (
    <textarea
      {...props}
      ref={ref}
      className={className}
      autoComplete="off"
      spellCheck={false}
      rows={props.rows || 3}
      onFocus={selectIfReadOnly}
    />
  )
})

export const Num = () => {

}

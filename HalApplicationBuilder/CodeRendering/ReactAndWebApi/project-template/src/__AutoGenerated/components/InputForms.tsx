import { forwardRef, ForwardedRef, InputHTMLAttributes, TextareaHTMLAttributes } from "react"

const Word = forwardRef((props: InputHTMLAttributes<HTMLInputElement>, ref: ForwardedRef<HTMLInputElement>) => {
  const className = `border border-neutral-400 ${props.className}`
  return (
    <input
      {...props}
      ref={ref}
      type="text"
      className={className}
      autoComplete="off"
      spellCheck={false}
    />
  )
})

const Description = forwardRef((props: TextareaHTMLAttributes<HTMLTextAreaElement>, ref: ForwardedRef<HTMLTextAreaElement>) => {
  const className = `border border-neutral-400 ${props.className}`
  return (
    <textarea
      {...props}
      ref={ref}
      className={className}
      autoComplete="off"
      spellCheck={false}
      rows={props.rows || 3}
    />
  )
})

const Num = () => {

}

export const InputForms = {
  Word,
  Description,
  Num,
}

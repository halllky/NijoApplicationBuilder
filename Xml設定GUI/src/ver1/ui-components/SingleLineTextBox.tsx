import React from "react"
import useEvent from "react-use-event-hook"

export const SingleLineTextBox = React.forwardRef((props: {
  value: string | undefined
  onChange: (value: string) => void
  onBlur?: () => void
  placeholder?: string
  className?: string
}, ref: React.ForwardedRef<HTMLInputElement>) => {

  const handleChange: React.ChangeEventHandler<HTMLInputElement> = useEvent(e => {
    props.onChange?.(e.target.value)
  })

  return (
    <input type="text"
      ref={ref}
      value={props.value}
      onChange={handleChange}
      onBlur={props.onBlur}
      placeholder={props.placeholder}
      className={`border border-color-4 ${props.className ?? ''}`}
    />
  )
})

import React from "react"
import useEvent from "react-use-event-hook"

export const SqlTextarea = (props: React.ComponentPropsWithoutRef<"textarea">) => {

  // Tab, Shift + Tab でインデントを変更する
  const handleKeyDown = useEvent((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Tab') {
      e.preventDefault()

      if (e.shiftKey) {
        const start = e.currentTarget.selectionStart
        const end = e.currentTarget.selectionEnd
        const value = e.currentTarget.value
        e.currentTarget.value = value.slice(0, start) + value.slice(end)
        e.currentTarget.selectionStart = start
        e.currentTarget.selectionEnd = start
      } else {
        const start = e.currentTarget.selectionStart
        const end = e.currentTarget.selectionEnd
        const value = e.currentTarget.value
        const indent = '  '
        e.currentTarget.value = value.slice(0, start) + indent + value.slice(end)
        e.currentTarget.selectionStart = start + indent.length
        e.currentTarget.selectionEnd = start + indent.length
      }
    }
    props.onKeyDown?.(e)
  })

  return (
    <textarea
      {...props}
      onKeyDown={handleKeyDown}
      spellCheck={props.spellCheck ?? false}
      className={`resize-none field-sizing-content whitespace-nowrap font-mono outline-none ${props.className ?? ''}`}
    />
  )
}
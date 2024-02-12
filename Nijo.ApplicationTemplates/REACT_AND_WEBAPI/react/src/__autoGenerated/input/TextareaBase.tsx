import React, { TextareaHTMLAttributes, useCallback, useImperativeHandle, useRef } from "react"
import { defineCustomComponent } from "./InputBase"

export const TextareaBase = defineCustomComponent<string, {}, TextareaHTMLAttributes<HTMLTextAreaElement>>((props, ref) => {

  const {
    onChange,
    value,
    readOnly,
    onFocus,
    rows,
    className,
    ...rest
  } = props

  const textareaRef = useRef<HTMLTextAreaElement>(null)

  useImperativeHandle(ref, () => ({
    getValue: () => textareaRef.current?.value,
    focus: () => textareaRef.current?.focus(),
  }), [])

  const handleFocus: React.FocusEventHandler<HTMLTextAreaElement> = useCallback(e => {
    if (readOnly) textareaRef.current?.select()
    onFocus?.(e)
  }, [onFocus, readOnly])
  const onTextChange: React.ChangeEventHandler<HTMLTextAreaElement> = useCallback(e => {
    onChange?.(e.target.value)
  }, [onChange])

  return (
    <textarea
      {...rest}
      ref={textareaRef}
      value={value ?? ''}
      className={readOnly
        ? `block w-full outline-none px-1 border border-color-4 bg-transparent cursor-default ${className}`
        : `block w-full outline-none px-1 border border-color-5 bg-color-base  ${className}`}
      autoComplete="off"
      spellCheck={false}
      rows={rows || 3}
      readOnly={readOnly}
      onFocus={handleFocus}
      onChange={onTextChange}
    />
  )
})

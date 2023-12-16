import React, { TextareaHTMLAttributes, useCallback, useImperativeHandle, useRef } from "react"
import { defineCustomComponent } from "./util"

export const TextareaBase = defineCustomComponent<string, {}, TextareaHTMLAttributes<HTMLTextAreaElement>>((props, ref) => {

  const {
    onChange,
    value,
    ...rest
  } = props

  const textareaRef = useRef<HTMLTextAreaElement>(null)

  useImperativeHandle(ref, () => ({
    getValue: () => textareaRef.current?.value,
    focus: () => textareaRef.current?.focus(),
  }), [])

  const onFocus: React.FocusEventHandler<HTMLTextAreaElement> = useCallback(e => {
    if (props.readOnly) textareaRef.current?.select()
    props.onFocus?.(e)
  }, [props.onFocus, props.readOnly])
  const onTextChange: React.ChangeEventHandler<HTMLTextAreaElement> = useCallback(e => {
    onChange?.(e.target.value)
  }, [onChange])

  return (
    <textarea
      {...rest}
      ref={textareaRef}
      value={value ?? ''}
      className={props.readOnly
        ? `block w-full outline-none px-1 border border-color-4 bg-transparent cursor-default ${props.className}`
        : `block w-full outline-none px-1 border border-color-5 bg-color-base  ${props.className}`}
      autoComplete="off"
      spellCheck={false}
      rows={props.rows || 3}
      onFocus={onFocus}
      onChange={onTextChange}
    />
  )
})

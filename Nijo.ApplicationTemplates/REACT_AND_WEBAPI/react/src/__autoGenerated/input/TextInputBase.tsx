import { ChevronUpDownIcon } from "@heroicons/react/24/solid";
import React, { useCallback, useEffect, useImperativeHandle, useRef, useState } from "react";
import { ValidationHandler, defineCustomComponent } from "./InputBase";

export type TextInputBaseArgs = Parameters<typeof TextInputBase>['0']
export type DropDownBody = (props: { focusRef: React.RefObject<never> }) => React.ReactNode
export type DropDownApi = { isOpened: boolean, open: () => void, close: () => void }

export const TextInputBase = defineCustomComponent<string, {
  dropdownBody?: DropDownBody
  onValidate?: ValidationHandler
  dropdownRef?: React.RefObject<DropDownApi>
}>((props, ref) => {

  const {
    dropdownBody,
    onValidate,
    dropdownRef,
    value,
    readOnly,
    placeholder,
    className,
    onChange: onChangeFormattedText,
    onFocus,
    onBlur,
    onKeyDown,
    ...rest
  } = props

  const inputRef = useRef<HTMLInputElement>(null)

  // フォーマット、バリデーション
  const [unFormatText, setUnFormatText] = useState(value ?? '')
  useEffect(() => {
    setUnFormatText(value ?? '')
  }, [value])

  const getFormatted = useCallback((rawText: string | undefined) => {
    if (!rawText) return ''
    if (!onValidate) return rawText
    var result = onValidate(rawText)
    return result.ok ? result.formatted : ''
  }, [onValidate])

  // ドロップダウン開閉
  const [open, setOpen] = useState(false)
  if (dropdownRef) (dropdownRef as React.MutableRefObject<DropDownApi>).current = {
    isOpened: open,
    open: () => setOpen(true),
    close: () => setOpen(false),
  }
  const onSideButtonClick = useCallback(() => {
    setOpen(!open)
  }, [open])
  const onClose = useCallback(() => {
    setOpen(false)
    inputRef.current?.focus()
  }, [])

  // イベント
  const onChange: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    setUnFormatText(e.target.value)
    if (onValidate === undefined) onChangeFormattedText?.(e.target.value)
  }, [onValidate, onChangeFormattedText])

  const handleFocus: React.FocusEventHandler<HTMLInputElement> = useCallback(e => {
    inputRef.current?.select()
    onFocus?.(e)
  }, [onFocus])

  const divRef = useRef<HTMLDivElement>(null)
  const handleBlur: React.FocusEventHandler<HTMLInputElement> = useCallback(e => {
    // フォーマットされた値を表示に反映
    const formatted = getFormatted(unFormatText)
    setUnFormatText(formatted)
    if (onValidate) onChangeFormattedText?.(formatted)

    // コンボボックスではテキストにフォーカスが当たったままドロップダウンが展開されることがあるため
    // blur先がdivの外に移った時に強制的にドロップダウンを閉じる
    if (divRef.current && e.relatedTarget && !divRef.current.contains(e.relatedTarget)) {
      setOpen(false)
    }
    onBlur?.(e)
  }, [onChangeFormattedText, onBlur, getFormatted, unFormatText, onValidate])

  const handleKeyDown: React.KeyboardEventHandler<HTMLInputElement> = useCallback(e => {
    if (!open && e.altKey && e.key === 'ArrowDown') {
      setOpen(true)
      e.preventDefault()
    }
    if (open && e.key === 'Escape') {
      setOpen(false)
      e.preventDefault()
    }
    if (e.key === 'Enter') {
      e.preventDefault() // Enterキーでsubmitされるのを防ぐ
    }
    onKeyDown?.(e)
  }, [onKeyDown, open])

  useImperativeHandle(ref, () => ({
    getValue: () => getFormatted(unFormatText),
    focus: () => inputRef.current?.select(),
  }), [getFormatted, unFormatText])

  return (
    <div
      ref={divRef}
      className={readOnly
        ? `inline-flex relative ${className} border border-transparent`
        : `inline-flex relative ${className} border border-color-5 bg-color-base text-color-12`}
    >
      <input
        {...rest}
        type="text"
        ref={inputRef}
        value={unFormatText}
        className="outline-none flex-1 min-w-0 px-1 bg-transparent"
        spellCheck="false"
        autoComplete="off"
        readOnly={readOnly}
        placeholder={readOnly ? undefined : placeholder}
        onKeyDown={handleKeyDown}
        onFocus={handleFocus}
        onBlur={handleBlur}
        onChange={onChange}
      />
      {!readOnly && dropdownBody &&
        <ChevronUpDownIcon
          className="w-6 text-color-5 border-l border-color-5 cursor-pointer"
          onClick={onSideButtonClick}
        />}

      {open && !readOnly && dropdownBody &&
        <Dropdown onClose={onClose}>
          {dropdownBody}
        </Dropdown>}
    </div>
  )
})


const Dropdown = ({ onClose, children }: {
  onClose?: () => void
  children?: DropDownBody
}) => {
  const divRef = useRef<HTMLDivElement>(null)
  const focusRef = useRef<never | null>(null)

  useEffect(() => {
    // ドロップダウン内の要素にフォーカスを当てる
    const htmlElement = focusRef.current as { focus: () => void } | null
    if (typeof htmlElement?.focus === 'function') {
      htmlElement.focus()
    }

    // 外部クリックでドロップダウンを閉じる処理を仕込む
    const handleClickOutside = (e: MouseEvent) => {
      if (!divRef.current) return
      if (divRef.current.contains(e.target as HTMLElement)) return
      onClose?.()
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [divRef, onClose])

  const onBlur: React.FocusEventHandler = useCallback(e => {
    onClose?.()
  }, [onClose])
  const onKeyDown: React.KeyboardEventHandler = useCallback(e => {
    if (e.key === 'Escape') {
      onClose?.()
      e.preventDefault()
    }
  }, [onClose])

  return (
    <div
      ref={divRef}
      className="absolute top-[calc(100%+2px)] left-[-1px] min-w-[calc(100%+2px)] z-10 bg-color-base border border-color-5 outline-none"
      onBlur={onBlur}
      onKeyDown={onKeyDown}
    >
      {children?.({ focusRef })}
    </div>
  )
}

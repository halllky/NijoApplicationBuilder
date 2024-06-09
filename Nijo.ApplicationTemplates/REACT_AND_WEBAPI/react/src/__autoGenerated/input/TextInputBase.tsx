import { ChevronUpDownIcon } from "@heroicons/react/24/solid";
import React, { useCallback, useEffect, useImperativeHandle, useRef, useState } from "react";
import { ValidationHandler, ValidationResult, defineCustomComponent } from "./InputBase";
import { useOutsideClick, useUserSetting } from "..";

export type TextInputBaseArgs = Parameters<typeof TextInputBase>['0']
export type DropDownBody = (props: { focusRef: React.RefObject<never> }) => React.ReactNode
export type DropDownApi = { isOpened: boolean, open: () => void, close: () => void }

export const TextInputBase = defineCustomComponent<string, {
  dropdownBody?: DropDownBody
  onValidate?: ValidationHandler
  onDropdownOpened?: () => void
  dropdownRef?: React.RefObject<DropDownApi>
  dropdownAutoOpen?: boolean
}>((props, ref) => {

  const {
    dropdownBody,
    onDropdownOpened,
    onValidate,
    dropdownRef,
    dropdownAutoOpen,
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
  const [formatError, setFormatError] = useState(false)
  const { data: { darkMode } } = useUserSetting()
  const bgColor = formatError
    ? (darkMode ? 'bg-rose-900' : 'bg-rose-200')
    : 'bg-color-base'

  useEffect(() => {
    if (formatError && value === '') return // 不正なテキストが入力されたことによる値変更の場合
    setUnFormatText(value ?? '')
  }, [value])

  const getValidationResult = useCallback((rawText: string | undefined): ValidationResult => {
    if (!rawText) return { ok: true, formatted: '' }
    if (!onValidate) return { ok: true, formatted: rawText }
    return onValidate(rawText)
  }, [onValidate])

  const executeFormat = useCallback(() => {
    if (onValidate) {
      const result = getValidationResult(unFormatText)
      if (result.ok) {
        setUnFormatText(result.formatted)
        onChangeFormattedText?.(result.formatted)
        setFormatError(false)
      } else {
        onChangeFormattedText?.('')
        setFormatError(true)
      }
    } else {
      setFormatError(false)
    }
  }, [onValidate, unFormatText, setUnFormatText, onChangeFormattedText])

  // ドロップダウン開閉
  const [open, setOpen] = useState(false)
  if (dropdownRef) (dropdownRef as React.MutableRefObject<DropDownApi>).current = {
    isOpened: open || (dropdownAutoOpen ?? false),
    open: () => setOpen(true),
    close: () => setOpen(false),
  }
  const onSideButtonClick = useCallback(() => {
    setOpen(!open)
    onDropdownOpened?.()
  }, [open, onDropdownOpened])
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
    executeFormat()

    // コンボボックスではテキストにフォーカスが当たったままドロップダウンが展開されることがあるため
    // blur先がdivの外に移った時に強制的にドロップダウンを閉じる
    if (divRef.current && e.relatedTarget && !divRef.current.contains(e.relatedTarget)) {
      setOpen(false)
    }

    onBlur?.(e)
  }, [onBlur, executeFormat])

  // バリデーションのルールが変わったときに再評価
  useEffect(() => {
    executeFormat()
  }, [onValidate])

  const handleKeyDown: React.KeyboardEventHandler<HTMLInputElement> = useCallback(e => {
    if (!dropdownAutoOpen && open && e.key === 'Escape') {
      setOpen(false)
      e.preventDefault()
    }
    if (e.key === 'Enter') {
      e.preventDefault() // Enterキーでsubmitされるのを防ぐ
    }
    onKeyDown?.(e)
  }, [onKeyDown, open, dropdownAutoOpen])

  useImperativeHandle(ref, () => ({
    getValue: () => {
      const result = getValidationResult(unFormatText)
      return result.ok ? result.formatted : ''
    },
    focus: () => inputRef.current?.select(),
  }), [getValidationResult, unFormatText])

  return (
    <div
      ref={divRef}
      className={readOnly
        ? `inline-flex relative ${className} border border-transparent`
        : `inline-flex relative ${className} border border-color-5 text-color-12 ${bgColor}`}
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

      {(open || dropdownAutoOpen) && !readOnly && dropdownBody &&
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
  }, [])

  useOutsideClick(divRef, () => {
    onClose?.()
  }, [onClose])

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

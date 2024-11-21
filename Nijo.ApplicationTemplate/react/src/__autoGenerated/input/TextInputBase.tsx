import React, { useEffect, useImperativeHandle, useRef, useState } from "react";
import { DefaultElementAttrs, ValidationHandler, ValidationResult, defineCustomComponent } from "./InputBase";
import { useIMEOpened, useUserSetting } from "..";
import useEvent from "react-use-event-hook";

/** TextInputBase特有の属性 */
export type TextInputBaseAdditionalAttrs = {
  onValidate?: ValidationHandler
  /** onChangeと異なり1文字入力されるごとに発火する */
  onOneCharChanged?: (value: string) => void
  /** テキストボックスの左側に表示されるコンポーネント */
  AtStart?: React.ReactNode
  /** テキストボックスの右側に表示されるコンポーネント */
  AtEnd?: React.ReactNode
  inputClassName?: string
}
/** TextInputBase特有のRef */
export type TextInputBaseAdditionalRef = {
  element?: HTMLElement | null
}

export const TextInputBase = defineCustomComponent<
  string,
  TextInputBaseAdditionalAttrs,
  DefaultElementAttrs,
  TextInputBaseAdditionalRef
>((props, ref) => {

  const {
    onValidate,
    value,
    readOnly,
    placeholder,
    className: propsClassName,
    inputClassName,
    onChange: onChangeFormattedText,
    onOneCharChanged,
    onFocus,
    onBlur,
    onKeyDown,
    AtStart,
    AtEnd,
    ...rest
  } = props

  const inputRef = useRef<HTMLInputElement>(null)

  // フォーマット、バリデーション
  const [unFormatText, setUnFormatText] = useState(value ?? '')
  const [formatError, setFormatError] = useState(false)
  const { data: { darkMode } } = useUserSetting()

  useEffect(() => {
    if (formatError && value === '') return // 不正なテキストが入力されたことによる値変更の場合
    setUnFormatText(value ?? '')
  }, [value])

  const getValidationResult = useEvent((rawText: string | undefined): ValidationResult => {
    if (!rawText) return { ok: true, formatted: '' }
    if (!onValidate) return { ok: true, formatted: rawText }
    return onValidate(rawText)
  })

  const executeFormat = useEvent(() => {
    let formatted: string
    if (onValidate) {
      const result = getValidationResult(unFormatText)
      if (result.ok) {
        formatted = result.formatted
        setFormatError(false)
        setUnFormatText(formatted)
      } else {
        formatted = ''
        setFormatError(true)
      }
    } else {
      formatted = unFormatText
      setFormatError(false)
    }
    if ((value ?? '') !== formatted) {
      onChangeFormattedText?.(formatted)
    }
  })

  // イベント
  const onChange: React.ChangeEventHandler<HTMLInputElement> = useEvent(e => {
    onOneCharChanged?.(e.target.value)
    setUnFormatText(e.target.value)
  })

  const handleFocus: React.FocusEventHandler<HTMLInputElement> = useEvent(e => {
    inputRef.current?.select()
    onFocus?.(e)
  })

  const divRef = useRef<HTMLDivElement>(null)
  const handleBlur: React.FocusEventHandler<HTMLInputElement> = useEvent(e => {
    // サイドボタンのクリックでblurが発火してしまうのを防ぐ
    if (divRef.current?.contains(e.relatedTarget)) return

    // フォーマットされた値を表示に反映
    executeFormat()
    onBlur?.(e)
  })

  // バリデーションのルールが変わったときに再評価
  useEffect(() => {
    executeFormat()
  }, [onValidate])

  const [{ isImeOpen }] = useIMEOpened()
  const handleKeyDown: React.KeyboardEventHandler<HTMLInputElement> = useEvent(e => {
    if (e.key === 'Enter' && !isImeOpen) {
      executeFormat()
    }
    onKeyDown?.(e)
  })

  useImperativeHandle(ref, () => ({
    element: divRef.current,
    getValue: () => {
      const result = getValidationResult(unFormatText)
      return result.ok ? result.formatted : ''
    },
    focus: opt => {
      inputRef.current?.focus(opt)
      inputRef.current?.select()
    },
  }), [getValidationResult, unFormatText, inputRef])

  let className = `inline-flex relative min-w-0 border`
  if (readOnly) {
    className += ' border-transparent'
  } else {
    className += ' border-color-5 text-color-12'
    className += formatError
      ? (darkMode ? ' bg-rose-900' : ' bg-rose-200') :
      ' bg-color-base'
  }
  if (propsClassName === undefined || propsClassName?.indexOf('max-w-') === -1) {
    // propsで max-width が指定されている場合でもfullが勝ってしまうのでfullを設定するかどうかは慎重にする
    className += ' max-w-full'
  }
  if (propsClassName) {
    className += ' ' + propsClassName
  }

  return (
    <div
      ref={divRef}
      className={className}
      onBlur={handleBlur}
    >
      {AtStart}
      <input
        {...rest}
        type="text"
        ref={inputRef}
        value={unFormatText}
        className={`outline-none flex-1 min-w-0 px-1 bg-transparent ${inputClassName ?? ''}`}
        spellCheck="false"
        autoComplete="off"
        readOnly={readOnly}
        placeholder={readOnly ? undefined : placeholder}
        onKeyDown={handleKeyDown}
        onFocus={handleFocus}
        onChange={onChange}
      />
      {AtEnd}
    </div>
  )
})

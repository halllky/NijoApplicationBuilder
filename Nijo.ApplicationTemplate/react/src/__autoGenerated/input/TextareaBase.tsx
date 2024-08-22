import React, { HTMLAttributes, TextareaHTMLAttributes, useCallback, useImperativeHandle, useRef } from 'react'
import SimpleMDE, { GetMdeInstance } from 'react-simplemde-editor'
import 'easymde/dist/easymde.min.css'
import { defineCustomComponent } from './InputBase'

/**
 * HTML標準のシンプルなテキストエリア
 */
export const TextareaBase = defineCustomComponent<string, {}, TextareaHTMLAttributes<HTMLTextAreaElement>>((props, ref) => {
  const {
    onChange,
    value,
    readOnly,
    onFocus,
    rows,
    className: propsClassName,
    ...rest
  } = props

  const textareaRef = useRef<HTMLTextAreaElement>(null)

  useImperativeHandle(ref, () => ({
    getValue: () => textareaRef.current?.value,
    focus: opt => textareaRef.current?.focus(opt),
  }), [textareaRef])

  const handleFocus: React.FocusEventHandler<HTMLTextAreaElement> = useCallback(e => {
    if (readOnly) textareaRef.current?.select()
    onFocus?.(e)
  }, [onFocus, readOnly])
  const onTextChange: React.ChangeEventHandler<HTMLTextAreaElement> = useCallback(e => {
    onChange?.(e.target.value)
  }, [onChange])

  const className = readOnly
    ? `block w-full outline-none px-1 bg-transparent cursor-default ${propsClassName}`
    : `block w-full outline-none px-1 bg-color-base border border-color-5 ${propsClassName}`

  return (
    <textarea
      {...rest}
      ref={textareaRef}
      value={value ?? ''}
      className={className}
      autoComplete="off"
      spellCheck={false}
      rows={rows || 3}
      readOnly={readOnly}
      onFocus={handleFocus}
      onChange={onTextChange}
    />
  )
})


/**
 * react-simplemde-editor によるリッチなテキストエリア
 */
export const SimpleMdeTextareaBase = defineCustomComponent<string, {}, HTMLAttributes<HTMLDivElement>>((props, ref) => {

  const {
    onChange,
    value,
    readOnly,
    onFocus,
    className,
    ...rest
  } = props

  const mdeInstance = useRef<Parameters<GetMdeInstance>[0]>()
  const getMdeInstanceCallback: GetMdeInstance = useCallback(mde => {
    mdeInstance.current = mde
  }, [])

  useImperativeHandle(ref, () => ({
    getValue: () => value,
    focus: opt => {
      // focusが呼ばれるタイミングの方が早いのでsetTimeoutで待つ
      const waitForInstanceReady = () => setTimeout(() => {
        if (mdeInstance.current) {
          mdeInstance.current.codemirror.focus()
        } else {
          waitForInstanceReady()
        }
      }, 10)
      waitForInstanceReady()
    },
  }), [mdeInstance, value])

  const handleFocus: React.FocusEventHandler<HTMLDivElement> = useCallback(e => {
    onFocus?.(e)
  }, [onFocus, readOnly])
  const onTextChange = useCallback((value: string) => {
    onChange?.(value)
  }, [onChange])

  // TODO: ver0.1.1現在はreadonlyになることは無いので適当に実装している
  if (readOnly) return (
    <span>
      {value}
    </span>
  )

  return (
    <SimpleMDE
      getMdeInstance={getMdeInstanceCallback}
      value={value ?? ''}
      onChange={onTextChange}
      className={`w-full ${className}`}
      onFocus={handleFocus}
      spellCheck="false"
      options={OPTIONS}
      {...rest}
    />
  )
})

const OPTIONS: EasyMDE.Options = {
  spellChecker: false, // trueだと日本語の部分が全部チェックに引っかかってしまう
  toolbar: [], // ツールバー全部非表示
  status: false, // フッター非表示
  minHeight: '3rem',
}

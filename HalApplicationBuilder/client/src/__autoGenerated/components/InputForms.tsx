import React, { useEffect, useCallback, useRef, forwardRef, useImperativeHandle, ForwardedRef, InputHTMLAttributes, TextareaHTMLAttributes, useState } from "react"
import { CheckIcon } from "@heroicons/react/24/solid"
import { useFocusTarget, useIMEOpened } from "../hooks"

export const Word = forwardRef((props: InputHTMLAttributes<HTMLInputElement>, ref: ForwardedRef<HTMLInputElement | null>) => {

  // react-hook-formでregisterを使うにはforwardRefが必須。またフォーカスの制御にもuseRefが必須。
  // useImperativeHandleを使って通常のuseRefを親コンポーネントに渡すことで両立している
  const inputRef = useRef<HTMLInputElement>(null)
  useImperativeHandle(ref, () => inputRef.current!)

  const { globalFocusEvents, isEditing, textBoxEditEvents } = useFocusTarget(inputRef, { editable: true })
  const isReadOnly = props.readOnly || !isEditing

  useEffect(() => {
    if (isEditing) inputRef.current?.select()
  }, [isEditing])

  const className = isReadOnly
    ? `bg-color-base w-full outline-none px-1 cursor-default ${props.className}`
    : `bg-color-base w-full outline-none px-1 border border-color-5 ${props.className}`

  return (
    <input
      {...props}
      ref={inputRef}
      type="text"
      className={className}
      readOnly={isReadOnly}
      autoComplete="off"
      spellCheck={false}
      {...globalFocusEvents}
      {...textBoxEditEvents}
    />
  )
})

export const Description = forwardRef((props: TextareaHTMLAttributes<HTMLTextAreaElement>, ref: ForwardedRef<HTMLTextAreaElement>) => {
  // react-hook-formでregisterを使うにはforwardRefが必須。またフォーカスの制御にもuseRefが必須。
  // useImperativeHandleを使って通常のuseRefを親コンポーネントに渡すことで両立している
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  useImperativeHandle(ref, () => textareaRef.current!)

  const {
    globalFocusEvents,
    isEditing,
    textBoxEditEvents,
    startEditing,
    endEditing,
  } = useFocusTarget(textareaRef, { editable: true })
  const isReadOnly = props.readOnly || !isEditing

  const ime = useIMEOpened()
  const onKeyDown = useCallback((e: React.KeyboardEvent) => {
    // IME展開中は制御しない
    if (ime) return
    // 読み取り専用なら制御しない
    if (props.readOnly) return

    if (isEditing) {
      if (e.key === 'Escape'
        || e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
        endEditing()
        e.preventDefault()
        e.stopPropagation() // formのsubmitを防ぐ
      }
    } else {
      if (e.key.length === 1 // 文字か数字
        || e.key === 'Enter'
        || e.key === 'Space'
        || e.key === 'F2') {
        startEditing()
        e.preventDefault()
      }
    }
  }, [ime, isEditing, startEditing, endEditing])

  const className = isReadOnly
    ? `bg-color-base block w-full outline-none px-1 cursor-default ${props.className}`
    : `bg-color-base block w-full outline-none px-1 border border-color-5 ${props.className}`

  return (
    <textarea
      {...props}
      ref={textareaRef}
      className={className}
      readOnly={isReadOnly}
      autoComplete="off"
      spellCheck={false}
      rows={props.rows || 3}
      {...globalFocusEvents}
      {...textBoxEditEvents}
      onKeyDown={onKeyDown}
    />
  )
})

export const Num = () => {

}

export const CheckBox = forwardRef((props: InputHTMLAttributes<HTMLInputElement>, ref) => {
  // react-hook-formでregisterを使うにはforwardRefが必須。またフォーカスの制御にもuseRefが必須。
  // useImperativeHandleを使って通常のuseRefを親コンポーネントに渡すことで両立している
  const inputRef = useRef<HTMLInputElement>(null)
  const labelRef = useRef<HTMLLabelElement>(null)
  useImperativeHandle(ref, () => props.readOnly ? labelRef.current! : inputRef.current!)

  const { globalFocusEvents: labelGlobalFocusEvents } = useFocusTarget(labelRef)
  const { globalFocusEvents: inputGlobalFocusEvents } = useFocusTarget(inputRef)

  // readOnlyのときはcheckbox要素自体を消す。
  // disableやreadOnlyのときは値nullかundefinedでonChangeイベントが走ってしまうため。
  if (props.readOnly) return (
    <label
      ref={labelRef}
      className="relative w-6 h-6 inline-flex justify-center items-center"
      {...labelGlobalFocusEvents}
      tabIndex={0} // readOnlyのときはラベルにフォーカスを当てるため0にする
    >
      <span className={`w-5 h-5 inline-block
        ${props.checked ? 'bg-color-8' : ''}`}
      >
        <CheckIcon className={props.checked ? 'text-color-0' : 'invisible'} />
      </span>
    </label>
  )

  return (
    <label ref={labelRef} className="relative w-6 h-6 inline-flex justify-center items-center">
      <input type="checkbox"
        className="opacity-0 absolute top-0 left-0 right-0 bottom-0"
        ref={inputRef}
        {...props}
        checked={props.checked || false} // nullが入るとuncontrolledコンポーネントになってしまうので
        {...inputGlobalFocusEvents}
      />
      <span className={`w-5 h-5 inline-block
        border border-color-5 rounded-sm
        ${props.checked ? 'bg-color-8' : ''}`}
      >
        <CheckIcon className={props.checked ? 'text-color-0' : 'invisible'} />
      </span>
    </label>
  )
})

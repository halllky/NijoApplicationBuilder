import { useCallback, useRef, forwardRef, useImperativeHandle, ForwardedRef, InputHTMLAttributes, TextareaHTMLAttributes } from "react"
import { CheckIcon } from "@heroicons/react/24/solid"
import { useFocusTarget } from "../hooks"

const selectAll = (e: React.FocusEvent<HTMLInputElement>) => {
  e.target.select()
}

export const Word = forwardRef((props: InputHTMLAttributes<HTMLInputElement>, ref: ForwardedRef<HTMLInputElement | null>) => {
  // react-hook-formでregisterを使うにはforwardRefが必須。またフォーカスの制御にもuseRefが必須。
  // useImperativeHandleを使って通常のuseRefを親コンポーネントに渡すことで両立している
  const inputRef = useRef<HTMLInputElement>(null)
  useImperativeHandle(ref, () => inputRef.current!)

  const { globalFocusEvents } = useFocusTarget(inputRef)
  const className = props.readOnly
    ? `bg-color-base w-full outline-none px-1 cursor-default ${props.className}`
    : `bg-color-base w-full outline-none px-1 border border-color-5 ${props.className}`
  return (
    <input
      onFocus={selectAll}
      {...props}
      ref={inputRef}
      type="text"
      className={className}
      autoComplete="off"
      spellCheck={false}
      {...globalFocusEvents}
    />
  )
})

export const Description = forwardRef((props: TextareaHTMLAttributes<HTMLTextAreaElement>, ref: ForwardedRef<HTMLTextAreaElement>) => {
  // react-hook-formでregisterを使うにはforwardRefが必須。またフォーカスの制御にもuseRefが必須。
  // useImperativeHandleを使って通常のuseRefを親コンポーネントに渡すことで両立している
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  useImperativeHandle(ref, () => textareaRef.current!)

  const className = props.readOnly
    ? `bg-color-base block w-full outline-none px-1 cursor-default ${props.className}`
    : `bg-color-base block w-full outline-none px-1 border border-color-5 ${props.className}`
  const selectIfReadOnly = useCallback((e: React.FocusEvent<HTMLTextAreaElement>) => {
    if (props.readOnly) e.target.select()
  }, [props.readOnly])

  const { globalFocusEvents } = useFocusTarget(textareaRef)

  return (
    <textarea
      {...props}
      ref={textareaRef}
      className={className}
      autoComplete="off"
      spellCheck={false}
      rows={props.rows || 3}
      onFocus={selectIfReadOnly}
      {...globalFocusEvents}
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

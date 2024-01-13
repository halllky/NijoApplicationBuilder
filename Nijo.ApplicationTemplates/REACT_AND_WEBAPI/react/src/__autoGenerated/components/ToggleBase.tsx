import { CheckIcon } from "@heroicons/react/24/outline"
import { useRef, useImperativeHandle, useState, useCallback, createRef } from "react"
import { CustomComponentProps, CustomComponentRef, defineCustomComponent } from "../util"
import { TextInputBase } from "./TextInputBase"

export const ToggleBase = defineCustomComponent<boolean>((props, ref) => {
  const {
    children,
    onChange: onChangeEx,
    value: valueEx,
    ...rest
  } = props

  const inputRef = useRef<HTMLInputElement>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => inputRef.current?.checked ?? false,
    focus: () => inputRef.current?.focus(),
  }), [])


  const forceRendering = useForceRendering()
  const emitChange = useCallback((checked: boolean) => {
    if (!inputRef.current) return
    if (props.readOnly) return
    inputRef.current.checked = checked
    props.onChange?.(inputRef.current.checked)
    onChangeEx?.(checked)
    forceRendering()
  }, [props.onChange, props.readOnly, onChangeEx, forceRendering])

  const onChange: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    emitChange(e.target.checked)
  }, [emitChange])

  const onKeyDown: React.KeyboardEventHandler<HTMLInputElement> = useCallback(e => {
    if (e.key === 'Enter' || e.key === ' ') {
      emitChange(!inputRef.current?.checked)
      e.preventDefault()
    }
  }, [emitChange, inputRef.current?.checked])

  return (
    <label className="relative inline-flex justify-center items-center focus-within:outline outline-1">
      <span className={`w-4 h-4 inline-block border rounded-sm
        ${props.readOnly ? 'ml-1' : ''}
        ${valueEx ? 'border-color-8' : 'border-color-5'}
        ${valueEx ? 'bg-color-8' : 'bg-color-base'}`}>
        <CheckIcon className={valueEx ? 'text-color-1' : 'invisible'} />
      </span>
      <input
        {...rest}
        ref={inputRef}
        type={inputRef.current?.type ?? 'checkbox'}
        className={`opacity-0 absolute top-0 left-0 right-0 bottom-0
          ${inputRef.current?.readOnly ? 'hidden' : ''}`}
        checked={valueEx}
        onChange={onChange}
        onKeyDown={onKeyDown}
      />
      {children && <span className="mx-1 select-none">
        {children}
      </span>}
    </label>
  )
})


export const RadioGroupBase = defineCustomComponent(<T extends {}>(
  props: CustomComponentProps<T, {
    options: T[]
    keySelector: (item: T) => string
    textSelector: (item: T) => string
  }>,
  ref: React.ForwardedRef<CustomComponentRef<T>>
) => {

  // 選択
  const setItem = useCallback((value: string | undefined) => {
    const found = props.options.find(item => props.keySelector(item) === value)
    props.onChange?.(found)
  }, [props.options, props.keySelector])

  // リスト選択
  const liRefs = useRef<React.RefObject<HTMLLIElement>[]>([])
  for (let i = 0; i < props.options.length; i++) {
    liRefs.current[i] = createRef()
  }

  // イベント
  const onChange: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    setItem(e.target.value)
  }, [setItem])
  const onKeyDown = useCallback((e: React.KeyboardEvent, item: T, index: number) => {
    if (e.key === 'Enter' || e.key === ' ') {
      props.onChange?.(item)
      e.preventDefault()
    } else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
      if (index > 0) liRefs.current[index - 1].current?.focus()
      e.preventDefault()
    } else if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
      if (index < props.options.length - 1) liRefs.current[index + 1].current?.focus()
      e.preventDefault()
    }
  }, [props.options, props.onChange])

  useImperativeHandle(ref, () => ({
    getValue: () => props.value,
    focus: () => liRefs.current[0]?.current?.focus(),
  }), [props.value, setItem])

  if (props.readOnly) return (
    <TextInputBase value={props.value ? props.textSelector(props.value) : ''} readOnly />
  )

  return (
    <ul className="flex flex-wrap gap-x-2 gap-y-1">
      {props.options.map((item, index) => (
        <li
          ref={liRefs.current[index]}
          key={props.keySelector(item)}
          className={`inline-flex items-center gap-1
            cursor-pointer select-none
            focus-within:outline outline-1`}
          tabIndex={0}
          onClick={() => setItem(props.keySelector(item))}
          onKeyDown={e => onKeyDown(e, item, index)}
        >
          <input
            type="radio"
            className="hidden"
            name={props.name}
            value={props.keySelector(item)}
            checked={!!props.value && props.keySelector(item) === props.keySelector(props.value)}
            onChange={onChange}
          />
          <RadioButton checked={!!props.value && props.keySelector(item) === props.keySelector(props.value)} />
          {props.textSelector(item)}
        </li>
      ))}
    </ul>
  )
})

const RadioButton = ({ checked }: { checked: boolean }) => {
  return (
    <div className={`inline-flex justify-center items-center w-3 h-3 border border-color-5 rounded-[50%] bg-color-base`}>
      {checked && (
        <div className="inline-flex w-2 h-2 rounded-[50%] bg-color-8">
        </div>
      )}
    </div>
  )
}

const useForceRendering = () => {
  const [bln, setBln] = useState(false)
  const forceRendering = useCallback(() => setBln(!bln), [bln])
  return forceRendering
}

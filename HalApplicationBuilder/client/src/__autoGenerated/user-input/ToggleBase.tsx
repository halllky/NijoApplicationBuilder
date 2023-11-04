import { CheckIcon } from "@heroicons/react/24/outline"
import { useRef, useImperativeHandle, useState, useCallback, createRef } from "react"
import { SelectionItem, defineCustomComponent } from "./util"
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
    inputRef.current.checked = checked
    props.onChange?.(inputRef.current.checked)
    onChangeEx?.(checked)
    forceRendering()
  }, [props.onChange, onChangeEx, forceRendering])

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
        ${inputRef.current?.checked ? 'border-color-8' : 'border-color-5'}
        ${inputRef.current?.checked ? 'bg-color-8' : 'bg-color-base'}`}>
        <CheckIcon className={inputRef.current?.checked ? 'text-color-1' : 'invisible'} />
      </span>
      <input
        {...rest}
        ref={inputRef}
        type={inputRef.current?.type ?? 'checkbox'}
        className={`opacity-0 absolute top-0 left-0 right-0 bottom-0
          ${inputRef.current?.readOnly ? 'hidden' : ''}`}
        onChange={onChange}
        onKeyDown={onKeyDown}
      />
      {children && <span className="mx-1 select-none">
        {children}
      </span>}
    </label>
  )
})


export const RadioGroupBase = defineCustomComponent<SelectionItem, { options: SelectionItem[] }>((props, ref) => {

  // 選択
  const setItem = useCallback((value: string | undefined) => {
    const found = props.options.find(item => item.value === value)
    props.onChange?.(found)
  }, [props.options])

  // リスト選択
  const liRefs = useRef<React.RefObject<HTMLLIElement>[]>([])
  for (let i = 0; i < props.options.length; i++) {
    liRefs.current[i] = createRef()
  }

  // イベント
  const onChange: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    setItem(e.target.value)
  }, [setItem])
  const onKeyDown = useCallback((e: React.KeyboardEvent, item: SelectionItem, index: number) => {
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
    <TextInputBase value={props.value?.text} readOnly />
  )

  return (
    <ul className="flex flex-wrap gap-x-2 gap-y-1">
      {props.options.map((item, index) => (
        <li
          ref={liRefs.current[index]}
          key={item.value}
          className={`inline-flex items-center gap-1
            cursor-pointer select-none
            focus-within:outline outline-1`}
          tabIndex={0}
          onClick={() => setItem(item.value)}
          onKeyDown={e => onKeyDown(e, item, index)}
        >
          <input
            type="radio"
            className="hidden"
            name={props.name}
            value={item.value}
            checked={item.value === props.value?.value}
            onChange={onChange}
          />
          <RadioButton checked={item.value === props.value?.value} />
          {item.text}
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

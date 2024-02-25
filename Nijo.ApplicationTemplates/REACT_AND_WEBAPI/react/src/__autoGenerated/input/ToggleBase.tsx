import { CheckIcon } from "@heroicons/react/24/outline"
import { useRef, useImperativeHandle, useState, useCallback, createRef } from "react"
import { CustomComponentProps, CustomComponentRef, defineCustomComponent } from "./InputBase"
import { TextInputBase } from "./TextInputBase"

export const ToggleBase = defineCustomComponent<boolean>((props, ref) => {
  const {
    children,
    onChange: onChangeEx,
    value: valueEx,
    readOnly,
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
    if (readOnly) return
    inputRef.current.checked = checked
    onChangeEx?.(checked)
    forceRendering()
  }, [readOnly, onChangeEx, forceRendering])

  const onChange: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    emitChange(e.target.checked)
  }, [emitChange])

  const onKeyDown: React.KeyboardEventHandler<HTMLInputElement> = useCallback(e => {
    if (e.key === 'Enter' || e.key === ' ') {
      emitChange(!inputRef.current?.checked)
      e.preventDefault()
    }
  }, [emitChange])

  // コンポーネントの描画後にbooleanかundefinedかが切り替わってはいけないのでundefinedはfalseに読み替える
  const boundValue = valueEx === undefined ? false : valueEx

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
        checked={boundValue}
        readOnly={readOnly}
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
  const {
    options,
    keySelector,
    textSelector,
    value,
    onChange,
    name,
    readOnly,
  } = props

  // 選択
  const setItem = useCallback((value: string | undefined) => {
    const found = options.find(item => keySelector(item) === value)
    onChange?.(found)
  }, [options, keySelector, onChange])

  // リスト選択
  const liRefs = useRef<React.RefObject<HTMLLIElement>[]>([])
  for (let i = 0; i < options.length; i++) {
    liRefs.current[i] = createRef()
  }

  // イベント
  const handleChange: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    setItem(e.target.value)
  }, [setItem])
  const onKeyDown = useCallback((e: React.KeyboardEvent, item: T, index: number) => {
    if (e.key === 'Enter' || e.key === ' ') {
      onChange?.(item)
      e.preventDefault()
    } else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
      if (index > 0) liRefs.current[index - 1].current?.focus()
      e.preventDefault()
    } else if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
      if (index < options.length - 1) liRefs.current[index + 1].current?.focus()
      e.preventDefault()
    }
  }, [options, onChange])

  useImperativeHandle(ref, () => ({
    getValue: () => value,
    focus: () => liRefs.current[0]?.current?.focus(),
  }), [value])

  if (readOnly) return (
    <TextInputBase value={value ? textSelector(value) : ''} readOnly />
  )

  return (
    <ul className="flex flex-wrap gap-x-2 gap-y-1">
      {options.map((item, index) => (
        <li
          ref={liRefs.current[index]}
          key={keySelector(item)}
          className={`inline-flex items-center gap-1
            cursor-pointer select-none
            focus-within:outline outline-1`}
          tabIndex={0}
          onClick={() => setItem(keySelector(item))}
          onKeyDown={e => onKeyDown(e, item, index)}
        >
          <input
            type="radio"
            className="hidden"
            name={name}
            value={keySelector(item)}
            checked={!!value && keySelector(item) === keySelector(value)}
            onChange={handleChange}
          />
          <RadioButton checked={!!value && keySelector(item) === keySelector(value)} />
          {textSelector(item)}
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

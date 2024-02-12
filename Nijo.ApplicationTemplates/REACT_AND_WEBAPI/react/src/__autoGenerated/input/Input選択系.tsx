import { useMemo, useCallback, useRef, useImperativeHandle, useState } from "react"
import { useQuery } from "react-query"
import { CustomComponentProps, CustomComponentRef, defineCustomComponent, useMsgContext } from "../util"
import { ComboBoxBase } from "./ComboBoxBase"
import { RadioGroupBase, ToggleBase } from "./ToggleBase"

/** ラジオボタン or コンボボックス */
export const Selection = defineCustomComponent(<T extends {}>(
  props: CustomComponentProps<T, {
    options: T[]
    keySelector: (item: T) => string
    textSelector: (item: T) => string
  }>,
  ref: React.ForwardedRef<CustomComponentRef<T>>
) => {
  return props.options.length > 5
    ? <ComboBoxBase ref={ref} {...props} />
    : <RadioGroupBase ref={ref} {...props} />
})

/** ラジオボタン（選択された要素ではなく選択された要素のキーを登録するためのもの） */
export const SelectionEmitsKey = defineCustomComponent(<TItem extends {}, TKey extends string = string>(
  props: CustomComponentProps<TKey, {
    options: TItem[]
    keySelector: (item: TItem) => TKey
    textSelector: (item: TItem) => string
  }>,
  ref: React.ForwardedRef<CustomComponentRef<TKey>>
) => {
  const { options, keySelector, value, onChange, ...rest } = props

  // value
  const objValue = useMemo(() => {
    return options.find(item => keySelector(item) === value)
  }, [options, keySelector, value])
  const handleChange = useCallback((item: TItem | undefined) => {
    onChange?.(item ? keySelector(item) : undefined)
  }, [onChange, keySelector])

  // ref
  const radioRef = useRef<CustomComponentRef<TItem>>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => {
      const selectedItem = radioRef.current?.getValue()
      return selectedItem ? keySelector(selectedItem) : undefined
    },
    focus: () => radioRef.current?.focus(),
  }))

  return options.length > 5
    ? (
      <ComboBoxBase
        ref={radioRef}
        {...rest}
        options={options}
        keySelector={keySelector}
        value={objValue}
        onChange={handleChange}
      />
    ) : (
      <RadioGroupBase
        ref={radioRef}
        {...rest}
        options={options}
        keySelector={keySelector}
        value={objValue}
        onChange={handleChange}
      />
    )
})

/** ラジオボタン */
export const RadioGroup = RadioGroupBase

/** コンボボックス（同期） */
export const ComboBox = ComboBoxBase

/** コンボボックス（非同期） */
export const AsyncComboBox = defineCustomComponent(<T extends {},>(
  props: CustomComponentProps<T, {
    queryKey?: string
    query: ((keyword: string | undefined) => Promise<T[]>)
    keySelector: (item: T) => string
    textSelector: (item: T) => string
  }>,
  ref: React.ForwardedRef<CustomComponentRef<T>>
) => {
  const [, dispatchMsg] = useMsgContext()

  // 検索結結果取得
  const { data, refetch } = useQuery({
    queryKey: props.queryKey,
    queryFn: async () => await props.query(keyword),
    onError: error => {
      dispatchMsg(msg => msg.error(`ERROR!: ${JSON.stringify(error)}`))
    },
  })

  // 検索処理発火
  const [keyword, setKeyword] = useState<string | undefined>()
  const [setTimeoutHandle, setSetTimeoutHandle] = useState<NodeJS.Timeout | undefined>(undefined)
  const onKeywordChanged = useCallback((value: string | undefined) => {
    setKeyword(value)
    if (setTimeoutHandle !== undefined) clearTimeout(setTimeoutHandle)
    setSetTimeoutHandle(setTimeout(() => {
      refetch()
      setSetTimeoutHandle(undefined)
    }, 300))
  }, [refetch, setTimeoutHandle])

  return (
    <ComboBoxBase
      ref={ref}
      {...props}
      options={data ?? []}
      onKeywordChanged={onKeywordChanged}
    />
  )
})

/** チェックボックス */
export const CheckBox = defineCustomComponent<boolean>((props, ref) => {
  return <ToggleBase ref={ref} {...props} />
})

/** チェックボックス（グリッド用） */
export const BooleanComboBox = defineCustomComponent<boolean>((props, ref) => {
  const { value, onChange, ...rest } = props

  const objValue = useMemo(() => {
    if (value === true) return booleanComboBoxOptions[0]
    if (value === false) return booleanComboBoxOptions[1]
    return undefined
  }, [value])
  const handleChange = useCallback((value: (typeof booleanComboBoxOptions[0]) | undefined) => {
    onChange?.(value?.boolValue)
  }, [onChange])

  const comboRef = useRef<CustomComponentRef>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => {
      const selectedItem = comboRef.current?.getValue()
      if (selectedItem === booleanComboBoxOptions[0]) return true
      if (selectedItem === booleanComboBoxOptions[1]) return false
      return undefined
    },
    focus: () => comboRef.current?.focus(),
  }))

  return (
    <ComboBox
      ref={comboRef}
      {...rest}
      options={booleanComboBoxOptions}
      keySelector={item => item.text}
      textSelector={item => item.text}
      value={objValue}
      onChange={handleChange}
    />
  )
})
const booleanComboBoxOptions = [
  { text: "○", boolValue: true },
  { text: "-", boolValue: false },
]

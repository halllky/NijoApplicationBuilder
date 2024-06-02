import { useMemo, useCallback, useRef, useImperativeHandle, useState } from "react"
import { useQuery } from "react-query"
import { useMsgContext } from "../util"
import { CustomComponentProps, CustomComponentRef, defineCustomComponent } from "./InputBase"
import { ComboBoxBase } from "./ComboBoxBase"
import { RadioGroupBase, ToggleBase } from "./ToggleBase"

/** コンボボックス */
export const ComboBox = ComboBoxBase // nijoが自動生成するコードでは使われないがカスタマイズされる際に使われる可能性がある

/** ラジオボタン or コンボボックス */
export const Selection = defineCustomComponent(<TItem extends string = string>(
  props: CustomComponentProps<TItem, {
    options: TItem[]
    textSelector: (item: TItem) => string
    radio?: boolean
    combo?: boolean
  }>,
  ref: React.ForwardedRef<CustomComponentRef<TItem>>
) => {
  const { options, radio, combo, ...rest } = props

  const radioRef = useRef<CustomComponentRef<TItem>>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => radioRef.current?.getValue(),
    focus: () => radioRef.current?.focus(),
  }))

  const type = useMemo(() => {
    if (radio) return 'radio' as const
    if (combo) return 'combo' as const
    return options.length > 5
      ? 'combo' as const
      : 'radio' as const
  }, [radio, combo, options])

  const selector = useCallback((value: TItem) => {
    return value
  }, [])

  return type === 'combo' ? (
    <ComboBoxBase
      {...rest}
      ref={radioRef}
      options={options}
      matchingKeySelectorFromOption={selector}
      matchingKeySelectorFromEmitValue={selector}
      emitValueSelector={selector}
    />
  ) : (
    <RadioGroupBase
      {...rest}
      ref={radioRef}
      options={options}
      keySelector={selector}
    />
  )
})

/** コンボボックス（非同期） */
export const AsyncComboBox = defineCustomComponent(<TOption, TEmitValue, TMatchingKey extends string = string>(
  props: CustomComponentProps<TEmitValue, {
    queryKey?: string
    query: ((keyword: string | undefined) => Promise<TOption[]>)
    matchingKeySelectorFromOption: (item: TOption) => TMatchingKey | undefined
    matchingKeySelectorFromEmitValue: (value: TEmitValue) => TMatchingKey | undefined
    emitValueSelector: (item: TOption) => TEmitValue | undefined
    textSelector: (item: TOption) => string
  }>,
  ref: React.ForwardedRef<CustomComponentRef<TEmitValue>>
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

  const getBoolValue = useCallback((item: typeof booleanComboBoxOptions[0]) => {
    return item.boolValue
  }, [])
  const getText = useCallback((item: typeof booleanComboBoxOptions[0]) => {
    return item.text
  }, [])
  const getTextFromBool = useCallback((boolValue: boolean) => {
    if (boolValue) return booleanComboBoxOptions[0].text
    else return booleanComboBoxOptions[1].text
  }, [])

  return (
    <ComboBoxBase
      ref={comboRef}
      {...props}
      options={booleanComboBoxOptions}
      emitValueSelector={getBoolValue}
      matchingKeySelectorFromEmitValue={getTextFromBool}
      matchingKeySelectorFromOption={getText}
      textSelector={getText}
    />
  )
})
const booleanComboBoxOptions = [
  { text: "○", boolValue: true },
  { text: "-", boolValue: false },
]

import React, { useMemo, useCallback, useRef, useImperativeHandle } from "react"
import useEvent from "react-use-event-hook"
import { normalize, useMsgContext } from "../util"
import { CustomComponentProps, CustomComponentRef, defineCustomComponent } from "./InputBase"
import { ComboBoxBase, ComboProps2 } from "./ComboBoxBase"
import { RadioGroupBase, ToggleBase } from "./ToggleBase"

/** コンボボックス */
export const ComboBox = ComboBoxBase

/** ラジオボタンまたはコンボボックス。選択肢がリテラル型の場合のみ使用可能。 */
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
    focus: opt => radioRef.current?.focus(opt),
  }), [radioRef])

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

  const handleFiltering = useEvent(async (keyword: string | undefined): Promise<TItem[]> => {
    if (!keyword) {
      return options
    } else {
      const normalizedKeyword = normalize(keyword)
      return options.filter(opt => normalize(opt).includes(normalizedKeyword))
    }
  })

  return type === 'combo' ? (
    <ComboBoxBase
      {...rest}
      ref={radioRef}
      onFilter={handleFiltering}
      getOptionText={selector}
      getValueFromOption={selector}
      getValueText={selector}
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
export const AsyncComboBox = defineCustomComponent(<TOption, TEmitValue>(
  props: CustomComponentProps<TEmitValue, ComboProps2<TOption, TEmitValue>>,
  ref: React.ForwardedRef<CustomComponentRef<TEmitValue>>
) => {

  return (
    <ComboBoxBase
      ref={ref}
      {...props}
      onFilter={props.onFilter}
      waitTimeMS={props.waitTimeMS ?? 300}
      getOptionText={props.getOptionText}
      getValueFromOption={props.getValueFromOption}
      getValueText={props.getValueText}
    />
  )
})

/** 非同期コンボボックスのプロパティ */
export type AsyncComboBoxProps<TOption, TEmitValue = TOption> = {
  query: (keyword: string | undefined) => Promise<TOption[]>
} & Pick<ComboProps2<TOption, TEmitValue>, 'getOptionText' | 'getValueFromOption' | 'getValueText'>

/** チェックボックス */
export const CheckBox = defineCustomComponent<boolean, { label?: React.ReactNode }>((props, ref) => {
  const { label, className, style, ...rest } = props
  return (
    <label className={`flex items-center gap-1 ${className ?? ''}`} style={style}>
      <ToggleBase ref={ref} {...rest} />
      <span className="select-none">
        {label}
        &nbsp;
      </span>
    </label>
  )
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
    focus: opt => comboRef.current?.focus(opt),
  }), [comboRef])

  return (
    <ComboBoxBase
      ref={comboRef}
      {...props}
      {...boolComboProps}
    />
  )
})
const booleanComboBoxOptions = [
  { text: "○", boolValue: true },
  { text: "-", boolValue: false },
]
const boolComboProps: ComboProps2<typeof booleanComboBoxOptions[0], boolean> = {
  onFilter: () => Promise.resolve(booleanComboBoxOptions),
  getOptionText: opt => opt.text,
  getValueFromOption: opt => opt.boolValue,
  getValueText: value => value ? "○" : "-",
}

import React, { useCallback, useImperativeHandle, useMemo, useRef, useState } from "react";
import { TextInputBase, ValidationHandler } from "./TextInputBase";
import "dayjs/locale/ja";
import { ComboBoxBase } from "./ComboBoxBase";
import { CustomComponentProps, CustomComponentRef, defineCustomComponent, normalize, parseAsDate } from "./util";
import { TextareaBase } from "./TextareaBase";
import { RadioGroupBase, ToggleBase } from "./ToggleBase";
import { useAppContext } from "../application";
import { useQuery } from "react-query";

export * from "./AggregateComboBox"
export * from "./AgGridWrapper"
export * from "./IconButton"
export * from "./util"

/** 単語 */
export const Word = defineCustomComponent<string>((props, ref) => {
  return <TextInputBase ref={ref} {...props} />
})

/** 文章 */
export const Description = defineCustomComponent<string>((props, ref) => {
  return <TextareaBase ref={ref} {...props} />
})

/** 数値 */
export const Num = defineCustomComponent<number>((props, ref) => {

  const strValue = useMemo(() => {
    return props.value?.toString() ?? ''
  }, [props.value])

  const onValidate: ValidationHandler = useCallback(value => {
    const normalized = normalize(value).replace(',', '') // 桁区切りのカンマを無視
    if (normalized === '') return { ok: true, formatted: '' }
    const num = Number(normalized)
    return isNaN(num) ? { ok: false } : { ok: true, formatted: num.toString() }
  }, [])
  const onChange = useCallback((value: string | undefined) => {
    // TODO: TextAreaBaseのonBlurでもバリデーションをかけているので冗長
    const validated = onValidate(value ?? '')
    props.onChange?.(validated.ok && validated.formatted !== ''
      ? Number(validated.formatted)
      : undefined)
  }, [props.onChange])

  const textRef = useRef<CustomComponentRef<string>>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => {
      const validated = onValidate(textRef.current?.getValue() ?? '')
      return validated.ok && validated.formatted !== ''
        ? Number(validated.formatted)
        : undefined
    },
    focus: () => textRef.current?.focus(),
  }), [onValidate])

  return <TextInputBase
    ref={textRef}
    {...props}
    value={strValue}
    onChange={onChange}
    onValidate={onValidate}
  />
})

/** 年月日 */
export const Date = defineCustomComponent<string>((props, ref) => {
  const value = useMemo(() => {
    const validated = dateValidation(props.value ?? '')
    return validated.ok ? validated.formatted : ''
  }, [props.value])
  const overrideProps = {
    ...props,
    value,
    className: `w-24 ${props.className}`,
    placeholder: props.placeholder ?? '0000-00-00',
    onValidate: dateValidation,
  }
  return <TextInputBase ref={ref} {...overrideProps} />
})
const dateValidation: ValidationHandler = value => {
  const normalized = normalize(value)
  if (normalized === '') return { ok: true, formatted: '' }
  const parsed = parseAsDate(normalized, 'YYYY-MM-DD')
  return parsed
}

/** 年月 */
export const YearMonth = defineCustomComponent<number>((props, ref) => {
  const textRef = useRef<CustomComponentRef<string>>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => yearMonthConversion(textRef.current?.getValue() ?? ''),
    focus: () => textRef.current?.focus(),
  }), [])

  const value = useMemo(() => {
    if (props.value == undefined) return ''
    const year = Math.floor(props.value / 100)
    const month = props.value % 100
    return `${year.toString().padStart(4, '0')}-${month.toString().padStart(2, '0')}`
  }, [props.value])
  const onChange = useCallback((value: string | undefined) => {
    props.onChange?.(yearMonthConversion(value ?? ''))
  }, [props.onChange])

  const overrideProps = {
    ...props,
    value,
    onChange,
    className: `w-20 ${props.className}`,
    placeholder: props.placeholder ?? '0000-00',
    onValidate: yearMonthValidation,
  }

  return <TextInputBase ref={textRef} {...overrideProps} />
})
const yearMonthValidation: ValidationHandler = value => {
  const normalized = normalize(value)
  if (normalized === '') return { ok: true, formatted: '' }
  const parsed = parseAsDate(normalized, 'YYYY-MM')
  return parsed
}
const yearMonthConversion = (value: string) => {
  const validated = yearMonthValidation(value)
  if (!validated.ok || validated.formatted === '') return undefined
  const splitted = validated.formatted.split('-')
  return (Number(splitted[0]) * 100) + Number(splitted[1])
}

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
  // value
  const value = useMemo(() => {
    return props.options.find(item => props.keySelector(item) === props.value)
  }, [props.options, props.keySelector, props.value])
  const onChange = useCallback((item: TItem | undefined) => {
    props.onChange?.(item ? props.keySelector(item) : undefined)
  }, [props.onChange, props.keySelector])

  // ref
  const radioRef = useRef<CustomComponentRef<TItem>>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => {
      const selectedItem = radioRef.current?.getValue()
      return selectedItem ? props.keySelector(selectedItem) : undefined
    },
    focus: () => radioRef.current?.focus(),
  }))

  return props.options.length > 5
    ? (
      <ComboBoxBase
        ref={radioRef}
        {...props}
        value={value}
        onChange={onChange}
      />
    ) : (
      <RadioGroupBase
        ref={radioRef}
        {...props}
        value={value}
        onChange={onChange}
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
  // エラー処理
  const [, dispatch] = useAppContext()

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
  }, [])

  // 検索結結果取得
  const { data, refetch } = useQuery({
    queryKey: props.queryKey,
    queryFn: async () => await props.query(keyword),
    onError: error => {
      dispatch({ type: 'pushMsg', msg: `ERROR!: ${JSON.stringify(error)}` })
    },
  })

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

  const value = useMemo(() => {
    if (props.value === true) return booleanComboBoxOptions[0]
    if (props.value === false) return booleanComboBoxOptions[1]
    return undefined
  }, [props.value])
  const onChange = useCallback((value: (typeof booleanComboBoxOptions[0]) | undefined) => {
    props.onChange?.(value?.boolValue)
  }, [props.onChange])

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
      {...props}
      options={booleanComboBoxOptions}
      keySelector={item => item.text}
      textSelector={item => item.text}
      value={value}
      onChange={onChange}
    />
  )
})
const booleanComboBoxOptions = [
  { text: "○", boolValue: true },
  { text: "-", boolValue: false },
]

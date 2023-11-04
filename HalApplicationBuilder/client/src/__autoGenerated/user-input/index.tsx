import { useCallback, useId, useState } from "react";
import { TextInputBase, ValidationHandler } from "./TextInputBase";
import "dayjs/locale/ja";
import { ComboBoxBase } from "./ComboBoxBase";
import { SelectionItem, defineCustomComponent, normalize, parseAsDate } from "./util";
import { TextareaBase } from "./TextareaBase";
import { RadioGroupBase, ToggleBase } from "./ToggleBase";
import { useAppContext } from "../hooks/AppContext";
import { useQuery } from "react-query";

export * from "./AgGridWrapper"
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
export const Num = defineCustomComponent<string>((props, ref) => {
  const onValidate: ValidationHandler = useCallback(value => {
    const normalized = normalize(value).replace(',', '') // 桁区切りのカンマを無視
    if (normalized === '') return { ok: true, formatted: '' }
    const num = Number(normalized)
    return isNaN(num) ? { ok: false } : { ok: true, formatted: num.toString() }
  }, [])
  return <TextInputBase ref={ref} {...props} onValidate={onValidate} />
})

/** 年月日 */
export const Date = defineCustomComponent<string>((props, ref) => {
  const onValidate: ValidationHandler = useCallback(value => {
    const normalized = normalize(value)
    if (normalized === '') return { ok: true, formatted: '' }
    const parsed = parseAsDate(normalized, 'YYYY-MM-DD')
    return parsed
  }, [])
  const overrideProps = {
    ...props,
    className: `w-24 ${props.className}`,
    placeholder: props.placeholder ?? '0000-00-00',
  }
  return <TextInputBase ref={ref} {...overrideProps} onValidate={onValidate} />
})

/** 年月 */
export const YearMonth = defineCustomComponent<string>((props, ref) => {
  const onValidate: ValidationHandler = useCallback(value => {
    const normalized = normalize(value)
    if (normalized === '') return { ok: true, formatted: '' }
    const parsed = parseAsDate(normalized, 'YYYY-MM')
    return parsed
  }, [])
  const overrideProps = {
    ...props,
    className: `w-20 ${props.className}`,
    placeholder: props.placeholder ?? '0000-00',
  }
  return <TextInputBase ref={ref} {...overrideProps} onValidate={onValidate} />
})

/** ラジオボタン or コンボボックス */
export const Selection = defineCustomComponent<SelectionItem, { options: SelectionItem[] }>((props, ref) => {
  return props.options.length > 5
    ? <ComboBoxBase ref={ref} {...props} />
    : <RadioGroupBase ref={ref} {...props} />
})

/** コンボボックス（同期） */
export const ComboBox = defineCustomComponent<SelectionItem, { options: SelectionItem[] }>((props, ref) => {
  return <ComboBoxBase ref={ref} {...props} />
})

/** コンボボックス（非同期） */
export const AsyncComboBox = defineCustomComponent<SelectionItem, {
  query: (keyword: string | undefined) => Promise<SelectionItem[]>
  responseHandler: (data: unknown) => SelectionItem[]
}>((props, ref) => {
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
  const id = useId()
  const { data, refetch } = useQuery({
    queryKey: id,
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

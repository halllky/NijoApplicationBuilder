import { useMemo, useRef, useImperativeHandle, useCallback } from "react"
import dayjs from "dayjs"
import { CustomComponentRef, ValidationHandler, defineCustomComponent } from "./InputBase"
import { TextInputBase } from "./TextInputBase"
import { normalize } from "../util/JsUtil"

import "dayjs/locale/ja"

/** 日付時刻 */
export const DateTime = defineCustomComponent<string>((props, ref) => {
  const value = useMemo(() => {
    const validated = dateTimeValidation(props.value ?? '')
    return validated.ok ? validated.formatted : ''
  }, [props.value])
  const overrideProps = {
    ...props,
    value,
    placeholder: props.placeholder ?? '0000-00-00 00:00:00',
    onValidate: dateTimeValidation,
  }
  return <TextInputBase ref={ref} {...overrideProps} />
})
const dateTimeValidation: ValidationHandler = value => {
  const normalized = normalize(value)
  if (normalized === '') return { ok: true, formatted: '' }
  const parsed = parseAsDate(normalized, 'YYYY-MM-DD HH:mm:ss')
  return parsed
}

/** 年月日 */
export const Date = defineCustomComponent<string>((props, ref) => {
  const value = useMemo(() => {
    const validated = dateValidation(props.value ?? '')
    return validated.ok ? validated.formatted : ''
  }, [props.value])
  const overrideProps = {
    ...props,
    value,
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
  const { value, onChange, ...rest } = props

  const textRef = useRef<CustomComponentRef<string>>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => yearMonthConversion(textRef.current?.getValue() ?? ''),
    focus: opt => textRef.current?.focus(opt),
  }), [textRef])

  const strValue = useMemo(() => {
    if (value == undefined) return ''
    const year = Math.floor(value / 100)
    const month = value % 100
    return `${year.toString().padStart(4, '0')}-${month.toString().padStart(2, '0')}`
  }, [value])
  const handleChange = useCallback((value: string | undefined) => {
    onChange?.(yearMonthConversion(value ?? ''))
  }, [onChange])

  const overrideProps = {
    ...rest,
    value: strValue,
    onChange: handleChange,
    placeholder: props.placeholder ?? '0000-00',
    onValidate: yearMonthValidation,
  }

  return <TextInputBase ref={textRef} {...overrideProps} />
})

// TODO: JsUtilと重複
const yearMonthValidation: ValidationHandler = value => {
  const normalized = normalize(value)
  if (normalized === '') return { ok: true, formatted: '' }
  const parsed = parseAsDate(normalized, 'YYYY-MM')
  return parsed
}

// TODO: JsUtilと重複
const yearMonthConversion = (value: string) => {
  const validated = yearMonthValidation(value)
  if (!validated.ok || validated.formatted === '') return undefined
  const splitted = validated.formatted.split('-')
  return (Number(splitted[0]) * 100) + Number(splitted[1])
}

// -----------------------------

// TODO: JsUtilと重複
const parseAsDate = (normalized: string, format: string): ReturnType<ValidationHandler> => {
  let parsed = dayjs(normalized, { format, locale: 'ja' })
  if (!parsed.isValid()) return { ok: false }
  if (parsed.year() == 2001 && !normalized.includes('2001')) {
    // 年が未指定の場合、2001年ではなくシステム時刻の年と解釈する
    parsed = parsed.set('year', dayjs().year())
  }
  return { ok: true, formatted: parsed.format(format) }
}

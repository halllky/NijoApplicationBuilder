import { useMemo, useRef, useImperativeHandle, useCallback } from "react"
import { CustomComponentRef, ValidationHandler, defineCustomComponent, normalize, parseAsDate } from "./InputBase"
import { TextInputBase } from "./TextInputBase"

import "dayjs/locale/ja"

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
  const { value, onChange, ...rest } = props

  const textRef = useRef<CustomComponentRef<string>>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => yearMonthConversion(textRef.current?.getValue() ?? ''),
    focus: () => textRef.current?.focus(),
  }), [])

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

import { useMemo, useCallback, useRef, useImperativeHandle } from "react"
import { CustomComponentRef, ValidationHandler, defineCustomComponent, normalize } from "./InputBase"
import { TextInputBase } from "./TextInputBase"
import { TextareaBase } from "./TextareaBase"

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
  const { value, onChange, ...rest } = props

  const strValue = useMemo(() => {
    return value?.toString() ?? ''
  }, [value])

  const handleChange = useCallback((value: string | undefined) => {
    // TODO: TextAreaBaseのonBlurでもバリデーションをかけているので冗長
    const { num } = tryParseAsNumberOrEmpty(value)
    onChange?.(num)
  }, [onChange])

  const textRef = useRef<CustomComponentRef<string>>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => {
      const { num } = tryParseAsNumberOrEmpty(textRef.current?.getValue() ?? '')
      return num
    },
    focus: () => textRef.current?.focus(),
  }), [])

  return <TextInputBase
    ref={textRef}
    {...rest}
    value={strValue}
    onChange={handleChange}
    onValidate={tryParseAsNumberOrEmpty}
  />
})

/** 数値として入力された文字列をC#やDBで扱える形にパースします。 */
export const tryParseAsNumberOrEmpty = (value: string | undefined): { ok: boolean, num: number | undefined, formatted: string } => {
  if (value === undefined) return { ok: true, num: undefined, formatted: '' }

  const normalized = normalize(value).replace(',', '') // 桁区切りのカンマを無視
  if (normalized === '') return { ok: true, num: undefined, formatted: '' }

  const num = Number(normalized)
  if (isNaN(num)) return { ok: false, num: undefined, formatted: normalized }
  if (num === Infinity) return { ok: false, num: undefined, formatted: normalized }
  return { ok: true, num, formatted: num.toString() }
}

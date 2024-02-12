import { useMemo, useCallback, useRef, useImperativeHandle } from "react"
import { CustomComponentRef, ValidationHandler, defineCustomComponent, normalize } from "../util"
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

  const onValidate: ValidationHandler = useCallback(value => {
    const normalized = normalize(value).replace(',', '') // 桁区切りのカンマを無視
    if (normalized === '') return { ok: true, formatted: '' }
    const num = Number(normalized)
    return isNaN(num) ? { ok: false } : { ok: true, formatted: num.toString() }
  }, [])
  const handleChange = useCallback((value: string | undefined) => {
    // TODO: TextAreaBaseのonBlurでもバリデーションをかけているので冗長
    const validated = onValidate(value ?? '')
    onChange?.(validated.ok && validated.formatted !== ''
      ? Number(validated.formatted)
      : undefined)
  }, [onChange, onValidate])

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
    {...rest}
    value={strValue}
    onChange={handleChange}
    onValidate={onValidate}
  />
})

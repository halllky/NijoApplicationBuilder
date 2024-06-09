import { useMemo, useCallback, useRef, useImperativeHandle } from "react"
import { CustomComponentRef, defineCustomComponent } from "./InputBase"
import { TextInputBase } from "./TextInputBase"
import { TextareaBase } from "./TextareaBase"
import { tryParseAsNumberOrEmpty } from "../util/JsUtil"

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

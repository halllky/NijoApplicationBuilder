import React, { HTMLAttributes, createContext, useCallback, useContext, useEffect, useState } from "react"
import dayjs from "dayjs"
import { ValidationHandler } from "./TextInputBase"
import { FieldValues, UseFormProps, useForm, FieldPath, PathValue, useFormContext } from "react-hook-form"
import { forwardRefEx } from "../util"

// ---------------------------------------------
// カスタムコンポーネント共通定義
// ※○○Base: 通常のHTMLのinputとやりとりをする
// ※カスタムコンポーネント: 通常のHTMLのinputとやりとりをしない
export const defineCustomComponent = <
  TValue,
  TAdditionalProp extends {} = {},
  TElementAttrs extends HTMLAttributes<HTMLElement> = HTMLAttributes<HTMLElement>
>(fn: (
  props: CustomComponentProps<TValue, TAdditionalProp, TElementAttrs>,
  ref: React.ForwardedRef<CustomComponentRef<TValue>>) => React.ReactNode
) => {
  return forwardRefEx(fn)
}

export type CustomComponent<TValue = any, TAdditionalProp extends {} = {}, TElementAttrs extends HTMLAttributes<HTMLElement> = HTMLAttributes<HTMLElement>>
  = ReturnType<typeof defineCustomComponent<TValue, TAdditionalProp, TElementAttrs>>

export interface CustomComponentRef<T = any> {
  /**
   * ag-gridのエディターとして表示されたときの編集終了時に参照される。
   * getValueはblurイベントより先に呼び出される
   */
  getValue: () => T | undefined
  /** ag-gridのエディターとして表示されたときの初回フォーカスに使う */
  focus: () => void
}

export type CustomComponentProps<
  TValue = any,
  TAdditionalProp extends {} = {},
  TElementAttrs extends HTMLAttributes<HTMLElement> = HTMLAttributes<HTMLElement>
>
  = Omit<TElementAttrs, 'value' | 'onChange'>
  & TAdditionalProp
  & {
    value?: TValue
    onChange?: (value: TValue | undefined) => void
    name?: string // 既定値であるHTMLAttributes<HTMLElement>にはnameがないので
    readOnly?: boolean // あったりなかったりするので
  }

// ---------------------------------------------
// カスタムコンポーネント共通に対して使うuseForm
export const useFormEx = <T extends FieldValues = FieldValues>(props: UseFormProps<T>) => {
  const useFormReturns = useForm(props)
  return {
    ...useFormReturns,
    registerEx: <TFieldName extends FieldPath<T>>(name: TFieldName) => ({
      value: useFormReturns.watch(name),
      onChange: (value: PathValue<T, TFieldName>) => {
        useFormReturns.setValue(name, value)
      },
    }),
  }
}

export const useFormContextEx = <T extends FieldValues = FieldValues>() => {
  const useFormContextReturns = useFormContext<T>()
  return {
    ...useFormContextReturns,
    registerEx: <TFieldName extends FieldPath<T>>(name: TFieldName) => ({
      value: useFormContextReturns.watch(name),
      onChange: (value: PathValue<T, TFieldName>) => {
        useFormContextReturns.setValue(name, value)
      },
    }),
  }
}

// ---------------------------------------------
export const normalize = (str: string) => str
  .replace(/(\s|　)/gm, '') // 空白を除去
  .normalize('NFKC') // 全角を半角に変換

export const parseAsDate = (normalized: string, format: string): ReturnType<ValidationHandler> => {
  let parsed = dayjs(normalized, { format, locale: 'ja' })
  if (!parsed.isValid()) return { ok: false }
  if (parsed.year() == 2001 && !normalized.includes('2001')) {
    // 年が未指定の場合、2001年ではなくシステム時刻の年と解釈する
    parsed = parsed.set('year', dayjs().year())
  }
  return { ok: true, formatted: parsed.format(format) }
}

// ---------------------------------------------
// IME
const ImeCheckerContext = createContext({ isImeOpen: false })
export const ImeContextProvider = <T extends HTMLElement>({ elementRef, children }: {
  elementRef: React.RefObject<T>
  children?: React.ReactNode
}) => {
  const [isImeOpen, setIsImeOpen] = useState(false)
  const onCompositionStart = useCallback(() => setIsImeOpen(true), [])
  const onCompositionEnd = useCallback(() => setIsImeOpen(false), [])
  useEffect(() => {
    elementRef.current?.addEventListener('compositionstart', onCompositionStart)
    elementRef.current?.addEventListener('compositionend', onCompositionEnd)
    return () => {
      elementRef.current?.removeEventListener('compositionstart', onCompositionStart)
      elementRef.current?.removeEventListener('compositionend', onCompositionEnd)
    }
  }, [elementRef.current])

  return (
    <ImeCheckerContext.Provider value={{ isImeOpen }}>
      {children}
    </ImeCheckerContext.Provider>
  )
}
export const useIMEOpened = () => {
  const { isImeOpen } = useContext(ImeCheckerContext)
  return isImeOpen
}

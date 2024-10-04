import { HTMLAttributes } from "react"
import { forwardRefEx } from "../util/ReactUtil"

export type ValidationHandler = (value: string) => ({ ok: true, formatted: string } | { ok: false })
export type ValidationResult = ReturnType<ValidationHandler>

// ---------------------------------------------
// カスタムコンポーネント共通定義
// ※○○Base: 通常のHTMLのinputとやりとりをする
// ※カスタムコンポーネント: 通常のHTMLのinputとやりとりをしない
export const defineCustomComponent = <
  TValue,
  TAdditionalProp extends {} = {},
  TElementAttrs extends HTMLAttributes<HTMLElement> = HTMLAttributes<HTMLElement>,
  TAdditionalRef extends {} = {},
>(fn: (
  props: CustomComponentProps<TValue, TAdditionalProp, TElementAttrs>,
  ref: React.ForwardedRef<CustomComponentRef<TValue> & TAdditionalRef>) => React.ReactNode
) => {
  return forwardRefEx(fn)
}

export type CustomComponent<
  TValue = any,
  TAdditionalProp extends {} = {},
  TElementAttrs extends HTMLAttributes<HTMLElement> = HTMLAttributes<HTMLElement>
> = ReturnType<typeof defineCustomComponent<TValue, TAdditionalProp, TElementAttrs>>

export interface CustomComponentRef<T = any> {
  /**
   * DataTableのエディターとして表示されたときの編集終了時に参照される。
   * getValueはblurイベントより先に呼び出される
   */
  getValue: () => T | undefined
  /** DataTableのエディターとして表示されたときの初回フォーカスに使う */
  focus: (opt?: FocusOptions) => void
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
export type SyncComboProps<TOption, TEmitValue = TOption, TMatchingKey extends string = string> = {
  options: TOption[]
  matchingKeySelectorFromOption: (item: TOption) => TMatchingKey | undefined
  matchingKeySelectorFromEmitValue: (value: TEmitValue) => TMatchingKey | undefined
  emitValueSelector: (item: TOption) => TEmitValue | undefined
  textSelector: (item: TOption) => string
  onKeywordChanged?: (keyword: string | undefined) => void
  dropdownAutoOpen?: boolean
}

export type AsyncComboProps<TOption, TEmitValue = TOption, TMatchingKey extends string = string> = {
  queryKey?: string
  query: ((keyword: string | undefined) => Promise<TOption[]>)
  matchingKeySelectorFromOption: (item: TOption) => TMatchingKey | undefined
  matchingKeySelectorFromEmitValue: (value: TEmitValue) => TMatchingKey | undefined
  emitValueSelector: (item: TOption) => TEmitValue | undefined
  textSelector: (item: TOption) => string
  dropdownAutoOpen?: boolean
}

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
  TElementAttrs extends HTMLAttributes<HTMLElement> = HTMLAttributes<HTMLElement>
>(fn: (
  props: CustomComponentProps<TValue, TAdditionalProp, TElementAttrs>,
  ref: React.ForwardedRef<CustomComponentRef<TValue>>) => React.ReactNode
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
/** 日付や数値などの表記ゆれを補正する */
export const normalize = (str: string) => str
  .replace(/(\s|　)/gm, '') // 空白を除去
  .replace('ー', '-') // NFKCで正規化されないので手動で正規化
  .normalize('NFKC') // 全角を半角に変換

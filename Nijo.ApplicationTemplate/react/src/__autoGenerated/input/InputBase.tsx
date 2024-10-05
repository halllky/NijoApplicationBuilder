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

/** コンボボックスのプロパティ */
export type ComboProps<TOption, TValue = TOption> = {
  /**
   * キーワードにヒットする選択肢を返す処理。
   * サイドボタンクリックなどキーワード入力に依らない場合は引数がundefinedになる。
  */
  onFilter: (keyword: string | undefined) => Promise<TOption[]>
  /**
   * 短時間で連続してクエリが発行されるのを防ぐための待ち時間。
   * 単位はミリ秒。規定値は0。
   */
  waitTimeMS?: number
  /** ドロップダウンの選択肢からvalueを抜き出す */
  getValueFromOption: (opt: TOption) => TValue
  /** ドロップダウンの選択肢から画面上に表示されるテキストを抜き出す */
  getOptionText: (opt: TOption) => string
  /** valueから画面上に表示されるテキストを抜き出す */
  getValueText: (value: TValue) => string
}

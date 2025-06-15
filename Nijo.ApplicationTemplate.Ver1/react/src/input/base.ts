import * as ReactHookForm from "react-hook-form"

/**
 * このアプリケーションで使われるカスタムの入力コンポーネントのプロパティの型
 *
 * @typeparam TField - フォーム（画面全体）のフィールドの型
 * @typeparam TPath - 画面全体からこのフィールドへのパス
 * @typeparam TValue - このフィールドが入出力する値の型
 */
export type CustomInputComponentProps<
  TValue,
  TField extends ReactHookForm.FieldValues,
  TPath extends ReactHookForm.FieldPathByValue<TField, TValue>
> = {
  /** 画面全体からこのフィールドへのパス */
  name: TPath
  /** react-hook-formの`useForm`の`control` */
  control: ReactHookForm.Control<TField>
  /** className。レイアウトの微調整に使用する */
  className?: string
  /** react-hook-formの`Controller`に渡されるバリデーションルール */
  rules?: Omit<
    ReactHookForm.RegisterOptions<TField, TPath>,
    "valueAsNumber" | "valueAsDate" | "setValueAs" | "disabled"
  >
  /** 読み取り専用か否か */
  readOnly?: boolean
}

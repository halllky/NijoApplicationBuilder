import * as ReactHookForm from "react-hook-form"
import * as React from "react"
import { CustomInputComponentProps } from "./base"
import { FieldErrorView, ClientSideValidatorContext } from "./FieldErrorView"

/**
 * 改行なしの単語の入力フォーム。
 * 改行がある複数行テキストは `Description` を使用する。
 */
export const Word = <
  TField extends ReactHookForm.FieldValues,
  TPath extends ReactHookForm.FieldPathByValue<TField, string | undefined>
>(props: CustomInputComponentProps<string | undefined, TField, TPath>) => {
  return (
    <ReactHookForm.Controller
      control={props.control}
      name={props.name}
      rules={props.rules}
      render={({ field }) => (
        <div className={`flex flex-col ${props.className ?? ''}`}>
          <input
            type="text"
            {...field}
            readOnly={props.readOnly}
            className="border border-gray-300 p-1"
          />
          <FieldErrorView name={props.name} />
        </div>
      )}
    />
  )
}

/**
 * 文章テキストエリア
 */
export const Description = <
  TField extends ReactHookForm.FieldValues,
  TPath extends ReactHookForm.FieldPathByValue<TField, string | undefined>
>(props: CustomInputComponentProps<string | undefined, TField, TPath>) => {
  return (
    <ReactHookForm.Controller
      control={props.control}
      name={props.name}
      rules={props.rules}
      render={({ field }) => (
        <div className={`flex flex-col ${props.className ?? ''}`}>
          <textarea
            {...field}
            className="border border-gray-300 p-1"
          />
          <FieldErrorView name={props.name} />
        </div>
      )}
    />
  )
}

/**
 * 数値入力フォーム
 */
export const NumberInput = <
  TField extends ReactHookForm.FieldValues,
  TPath extends ReactHookForm.FieldPathByValue<TField, number | undefined>
>(props: CustomInputComponentProps<number | undefined, TField, TPath>) => {
  // クライアント側のバリデーションコンテキスト（実装されていると仮定）
  const clientSideValidator = React.useContext(ClientSideValidatorContext);

  return (
    <ReactHookForm.Controller
      control={props.control}
      name={props.name}
      render={({ field }) => (
        <div className={`flex flex-col ${props.className ?? ''}`}>
          <input
            type="number"
            {...field}
            className="border border-gray-300 p-1"
            onBlur={(e) => {
              // フォーカスアウト時にノーマライズとバリデーションを発火
              if (clientSideValidator && clientSideValidator.validate) {
                // 入力値を数値に変換
                const numericValue = e.target.value ? parseFloat(e.target.value) : undefined;
                clientSideValidator.validate(props.name, numericValue);
              }
              // 元のonBlurも呼び出す
              field.onBlur();
            }}
          />
          <FieldErrorView name={props.name} />
        </div>
      )}
    />
  )
}

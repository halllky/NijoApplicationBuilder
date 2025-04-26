import * as ReactHookForm from "react-hook-form"
import * as React from "react"
import { CustomInputComponentProps } from "./base"
import { FieldErrorView, ClientSideValidatorContext } from "./FieldErrorView"

/**
 * 日付入力フォーム
 */
export const DateInput = <
  TField extends ReactHookForm.FieldValues,
  TPath extends ReactHookForm.FieldPathByValue<TField, string | undefined>
>(props: CustomInputComponentProps<string | undefined, TField, TPath>) => {
  // クライアント側のバリデーションコンテキスト
  const clientSideValidator = React.useContext(ClientSideValidatorContext);

  return (
    <ReactHookForm.Controller
      control={props.control}
      name={props.name}
      render={({ field }) => (
        <div className={`flex flex-col ${props.className ?? ''}`}>
          <input
            type="date"
            {...field}
            className="border border-gray-300 p-1"
            onBlur={(e) => {
              // フォーカスアウト時にノーマライズとバリデーションを発火
              if (clientSideValidator && clientSideValidator.validate) {
                clientSideValidator.validate(props.name, e.target.value);
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

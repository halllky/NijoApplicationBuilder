import * as ReactHookForm from "react-hook-form"
import * as React from "react"
import { CustomInputComponentProps } from "./base"
import { FieldErrorView, ClientSideValidatorContext } from "./FieldErrorView"

export type DateInputProps<
  TField extends ReactHookForm.FieldValues,
  TPath extends ReactHookForm.FieldPathByValue<TField, string | undefined>
> = CustomInputComponentProps<string | undefined, TField, TPath> & {
  /** 日付でなく年月の場合はこれを指定 */
  yearMonth?: boolean
}

/**
 * 日付入力フォーム
 */
export const DateInput = <
  TField extends ReactHookForm.FieldValues,
  TPath extends ReactHookForm.FieldPathByValue<TField, string | undefined>
>(props: DateInputProps<TField, TPath>) => {
  // クライアント側のバリデーションコンテキスト
  const clientSideValidator = React.useContext(ClientSideValidatorContext);

  return (
    <ReactHookForm.Controller
      control={props.control}
      name={props.name}
      render={({ field }) => (
        <div className={`flex flex-col ${props.className ?? ''}`}>
          <input
            type={props.yearMonth ? 'month' : 'date'}
            {...field}
            value={field.value ?? ''}
            className="bg-white border border-gray-300 p-1"
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

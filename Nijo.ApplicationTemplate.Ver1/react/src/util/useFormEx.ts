import * as ReactHookForm from "react-hook-form"

/**
 * react-hook-form の useForm を、既定の設定で初期化したもの。
 */
export const useFormEx = <
  TField extends ReactHookForm.FieldValues
>(props: ReactHookForm.UseFormProps<TField>) => {

  return ReactHookForm.useForm<TField>({
    mode: 'onSubmit',
    ...props,
  })
}

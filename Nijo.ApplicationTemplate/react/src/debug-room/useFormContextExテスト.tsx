import React from 'react'
import useEvent from 'react-use-event-hook'
import * as ReactHookForm from 'react-hook-form'
import * as Icon from '@heroicons/react/24/solid'
import * as Util from '../__autoGenerated/util'
import * as Input from '../__autoGenerated/input'
import * as Layout from '../__autoGenerated/collection'
import { VForm2 } from '../__autoGenerated/collection'

type PageData = {
  rootId: string | undefined
  rootName: string | undefined
  partialData: PartialData | undefined
  partialDataArray: PartialData[]
}

type PartialData = {
  partialName: string | undefined
  children: { childProp: string | undefined }[]
}

// ***********************************

export default function () {

  const rhf = ReactHookForm.useForm<PageData>()

  // // ------------------
  // // useFormPartial を使った案
  // const { register } = useFormPartial<PageData, PartialData>({ name: 'partialData', ...rhf })
  // register('children')

  // ------------------
  // Controller を使った案

  const handleClick = useEvent(() => {
    rhf.setError('partialData.partialName', { types: { 'ERROR-0': 'えらーよ' } })
    console.log(rhf.getValues())
  })

  return (
    <VForm2.Root>
      <VForm2.Item>
        <Input.IconButton onClick={handleClick}>console.log</Input.IconButton>
      </VForm2.Item>
      <PartialDataControllerView name='partialData' displayName='aaa' control={rhf.control} />
    </VForm2.Root>
  )
}

// ***********************************
// Controller を使った案
type PartialDataViewType = <
  TFieldValues extends ReactHookForm.FieldValues,
  TFieldName extends ReactHookForm.FieldPath<TFieldValues>,
>(props: {
  name: ReactHookForm.PathValue<TFieldValues, TFieldName> extends (PartialData | undefined) ? TFieldName : never
  control: ReactHookForm.Control<TFieldValues>
  displayName: string
}) => React.ReactNode

const PartialDataControllerView: PartialDataViewType = ({ name, control, displayName }) => {
  const getFullPath = (path: ReactHookForm.FieldPath<PartialData>) => `${name}.${path}` as typeof name

  const { field: { } } = ReactHookForm.useController({ name, control })

  return (
    <VForm2.Indent label={displayName}>
      <ReactHookForm.Controller
        name={getFullPath('partialName')}
        control={control}
        render={({ field, formState: { errors } }) => (
          <>
            {/* <input type="text" {...field} /> */}
            <Input.Word {...field} />
            <Input.FormItemMessage name={field.name} errors={errors} />
          </>
        )}
      />
    </VForm2.Indent>
  )
}

// ***********************************
// useFormPartial を使った案
const PartialDataView = <
  TFieldValues extends ReactHookForm.FieldValues,
  TFieldName extends ReactHookForm.FieldPath<TFieldValues>,
>({ name, ...rhf }: ReactHookForm.UseFormReturn<TFieldValues> & {
  name: ReactHookForm.PathValue<TFieldValues, TFieldName> extends (PartialData | undefined)
  ? TFieldName
  : never
}) => {
  const { register } = useFormPartial<TFieldValues, PartialData>({ name, ...rhf })

  register('partialName')

  return (
    <VForm2.Indent label={name}>

    </VForm2.Indent>
  )
}

/**
 * React hook form を使った画面のうち一部分だけ別コンポーネントに切り出す場合、
 * 画面ルートからのフィールドまでのパスが画面側と当該別コンポーネントで分断されるため、
 * asを使ってキャストせざるを得ないなど、TypeScriptの型安全性が損なわれてしまう。
 * その問題を簡単な記述で解決するためのフック。
 * 当該切り出された別コンポーネント側で使用する。
 */
const useFormPartial = <
  TFieldValues extends ReactHookForm.FieldValues,
  TFieldValuesPartial extends ReactHookForm.FieldValues,
  TFieldName extends ReactHookForm.FieldPath<TFieldValues> = ReactHookForm.FieldPath<TFieldValues>,
>({
  name,
  clearErrors,
  control,
  formState,
  getFieldState,
  getValues,
  handleSubmit,
  register,
  reset,
  resetField,
  setError,
  setFocus,
  setValue,
  trigger,
  unregister,
  watch,
}: ReactHookForm.UseFormReturn<TFieldValues> & {
  /** TFieldValuesPartial型と矛盾していてもエラーが出ないので注意（TypeScriptの型定義が難しすぎて挫折した） */
  name: TFieldName
}) => {
  // 切り出された子オブジェクトのメンバーのパス
  type PartialPath = ReactHookForm.FieldPath<TFieldValuesPartial>

  // 子オブジェクトのパスで指定できるようにReact hook formの各種関数をオーバーライドする
  const clearErrorsInternal = React.useCallback((path: PartialPath) => {
    clearErrors(`${name}.${path}` as TFieldName)
  }, [name, clearErrors])

  const getFieldStateInternal = React.useCallback((fieldName: PartialPath) => {
    return getFieldState(`${name}.${fieldName}` as TFieldName, formState)
  }, [name, getFieldState, formState])

  const getValuesInternal = React.useCallback((fieldName: PartialPath) => {
    return getValues(`${name}.${fieldName}` as TFieldName)
  }, [name, getValues])

  const registerInternal = React.useCallback((fieldName: PartialPath, options?: ReactHookForm.RegisterOptions<TFieldValuesPartial, PartialPath>) => {
    register(`${name}.${fieldName}` as TFieldName, options as ReactHookForm.RegisterOptions<TFieldValues, TFieldName> | undefined)
  }, [name, register])

  const resetFieldInternal = React.useCallback((fieldName: PartialPath, options?: Parameters<ReactHookForm.UseFormResetField<TFieldValues>>['1']) => {
    resetField(`${name}.${fieldName}` as TFieldName, options)
  }, [name, resetField])

  const setErrorInternal = React.useCallback((fieldName: PartialPath, error: ReactHookForm.ErrorOption, options?: Parameters<ReactHookForm.UseFormSetError<TFieldValues>>['2']) => {
    setError(`${name}.${fieldName}` as TFieldName, error, options)
  }, [name, setError])

  const setValueInternal = React.useCallback((fieldName: PartialPath, value: TFieldValuesPartial, options?: Parameters<ReactHookForm.UseFormSetValue<TFieldValues>>['2']) => {
    setValue(`${name}.${fieldName}` as TFieldName, value as ReactHookForm.PathValue<TFieldValues, TFieldName>, options)
  }, [name, setValue])

  const triggerInternal = React.useCallback((fieldName?: PartialPath) => {
    return trigger(fieldName ? `${name}.${fieldName}` as TFieldName : undefined)
  }, [name, trigger])

  const unregisterInternal = React.useCallback((fieldName: PartialPath) => {
    unregister(`${name}.${fieldName}` as TFieldName)
  }, [name, unregister])

  return {
    control,
    clearErrors: clearErrorsInternal,
    getFieldState: getFieldStateInternal,
    getValues: getValuesInternal,
    register: registerInternal,
    resetField: resetFieldInternal,
    setError: setErrorInternal,
    setValue: setValueInternal,
    trigger: triggerInternal,
    unregister: unregisterInternal,
    handleSubmit,// 使う場面がなさそうなのでそのまま返している（使うとしても部分コンポーネント内ではなく画面ルートでやるはず）
    reset,// 使う場面がなさそうなのでそのまま返している（使うとしても部分コンポーネント内ではなく画面ルートでやるはず）
    setFocus, // 使う場面がなさそうなのでそのまま返している（使うとしても部分コンポーネント内ではなく画面ルートでやるはず）
    watch, // 使う場面がなさそうなのでそのまま返している（使うとしてもパフォーマンス上の理由からこちらではなくuseWatchの方がよい）
  }
}

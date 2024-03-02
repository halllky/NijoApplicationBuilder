import { FieldPath, FieldValues, PathValue, UseFormProps, useForm, useFormContext } from 'react-hook-form'

// ---------------------------------------------
// 値の自動保存のためにコンテキスト内の値の変化を拾いたい
// TODO: 全カスタムコンポーネントにトリガーを仕込む
// TODO: FormProviderがハンドラを受け取れるように拡張する
type OnAnyValueChangeEvent = (name: string, value: unknown) => void

// ---------------------------------------------
// カスタムコンポーネント共通に対して使うuseForm
export type UseFormExProps<T extends FieldValues = FieldValues> = UseFormProps<T> & {
  onAnyValueChange?: OnAnyValueChangeEvent
}
export const useFormEx = <T extends FieldValues = FieldValues>(props: UseFormExProps<T>) => {
  const useFormReturns = useForm(props)
  return {
    ...useFormReturns,
    registerEx: <TFieldName extends FieldPath<T>>(name: TFieldName) => ({
      name,
      // TODO: watchを使うとページ全体に再レンダリングが走ってしまう
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
      name,
      // TODO: watchを使うとページ全体に再レンダリングが走ってしまう
      value: useFormContextReturns.watch(name),
      onChange: (value: PathValue<T, TFieldName>) => {
        useFormContextReturns.setValue(name, value)
      },
    }),
    onAnyValueChange: (useFormContextReturns as { onAnyValueChange?: OnAnyValueChangeEvent }).onAnyValueChange,
  }
}

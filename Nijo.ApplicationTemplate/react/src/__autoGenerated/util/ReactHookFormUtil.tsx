import { FieldPath, FieldValues, PathValue, UseFormGetValues, UseFormProps, useForm, useFormContext } from 'react-hook-form'

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

  const registerEx: UseFormExRegisterEx<T> = <TFieldName extends FieldPath<T>>(name: TFieldName) => ({
    name,
    // TODO: watchを使うとページ全体に再レンダリングが走ってしまう
    value: useFormContextReturns.watch(name),
    onChange: (value: PathValue<T, TFieldName>) => {
      useFormContextReturns.setValue(name, value)
    },
  })

  return {
    ...useFormContextReturns,
    /**
     * react hook form の通常のregisterはonChangeの引数がhtmlのものそのままなので変更後の値以外も入ってくる。
     * 値のみが入るようにしたものがこちら
     */
    registerEx,
    onAnyValueChange: (useFormContextReturns as { onAnyValueChange?: OnAnyValueChangeEvent }).onAnyValueChange,
  }
}

/** registerExの型 */
export type UseFormExRegisterEx<TFieldValues extends FieldValues>
  = <TFieldName extends FieldPath<TFieldValues>>(name: TFieldName) => RegisterExReturns<TFieldValues, TFieldName>

/** registerExの戻り値の型 */
export type RegisterExReturns<
  TFieldValues extends FieldValues,
  TFieldName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>
> = {
  name: TFieldName
  value: PathValue<TFieldValues, TFieldName>
  onChange: (value: PathValue<TFieldValues, TFieldName>) => void
}

// ---------------------------------------------
/**
 * getValuesを使用して、項目が読み取り専用か否かを返します。
 * 引数のパス自体が読み取り専用である場合以外にも、
 * 祖先要素のうちのいずれかのオブジェクトの'allReadOnly'がtrueに設定されている場合でも読み取り専用になります。
 */
export const isReadOnlyField = <T extends FieldValues = FieldValues,>(path: FieldPath<T>, getValues: UseFormGetValues<T>): boolean => {
  if (getValues(path) === true) return true

  // 後ろから1文字ずつ削っていって、いずれかの祖先要素の'allReadOnly'が指定されてるかを調べる
  let partialPath = path
  while (true) {
    if (partialPath.length === 0) {
      // ルート要素のパス
      if (getValues(`readOnly.allReadOnly` as FieldPath<T>) === true) return true
      break
    } else if (partialPath.endsWith('.')) {
      // ルート要素以外の祖先要素のパス
      partialPath = partialPath.substring(0, partialPath.length - 1) as FieldPath<T>
      if (getValues(`${partialPath}.allReadOnly` as FieldPath<T>) === true) return true
      if (getValues(`${partialPath}.readOnly.allReadOnly` as FieldPath<T>) === true) return true
    } else {
      // 祖先の名前の途中の中途半端な部分
      partialPath = partialPath.substring(0, partialPath.length - 1) as FieldPath<T>
    }
  }
  return false
}

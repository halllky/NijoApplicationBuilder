import { useCallback } from 'react'
import { FieldErrors, FieldName, FieldPath } from 'react-hook-form'
import { FieldValuesFromFieldErrors, ErrorMessage as HookFormErrorMessage } from '@hookform/error-message'
import * as Util from '../util'

/**
 * 画面全体ではなく特定の入力項目につくエラーメッセージ等の表示。
 * `@hookform/error-message` のErrorMessageコンポーネントのラッパー
 */
export const FormItemMessage = <T extends {} = {},>({ name, errors, className }: {
  /** メッセージ一覧 */
  errors?: FieldErrors<T>
  /**
   * errorsのオブジェクト中からこのnameに合致する項目のエラーが抽出される。
   * @hookform/error-message に合わせるなら FieldName<FieldValuesFromFieldErrors<T>> だが、それだと型検査が働いてくれないのでこの型にしている。
   * 'root' はルート要素に対するエラーメッセージが格納されるフィールドの名前。
   */
  name: 'root' | FieldPath<T>
  className?: string
}) => {
  const { data: { darkMode } } = Util.useUserSetting()
  const getTextColor = useCallback((typeKey: string) => {
    if (typeKey.startsWith('WARN-')) {
      return darkMode ? 'text-amber-200' : 'text-amber-700' // 警告の文字色
    } else if (typeKey.startsWith('INFO-')) {
      return darkMode ? 'text-sky-200' : 'text-sky-600' // インフォメーションの文字色
    } else {
      return darkMode ? 'text-rose-200' : 'text-rose-600' // エラーの文字色
    }
  }, [darkMode])

  const renderer: MessageRenderer = useCallback(({ message, messages }) => {
    if (!message && !messages) return undefined
    return (
      <ul className={`flex flex-col text-sm whitespace-normal ${className ?? ''}`}>
        {message && (
          <li className="select-all">{message}</li>
        )}
        {messages && Object.entries(messages).map(([typeKey, text], i) => (
          <li key={i} className={`select-all ${getTextColor(typeKey)}`}>{text}</li>
        ))}
      </ul>
    )
  }, [getTextColor, className])

  return (
    <HookFormErrorMessage
      name={name as FieldName<FieldValuesFromFieldErrors<FieldErrors<T>>>}
      errors={errors}
      render={renderer}
    />
  )
}

type MessageRenderer = Exclude<Parameters<typeof HookFormErrorMessage>['0']['render'], undefined>

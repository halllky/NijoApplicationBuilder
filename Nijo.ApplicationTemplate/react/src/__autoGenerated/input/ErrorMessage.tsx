import { useCallback } from 'react'
import { FieldErrors, FieldName, FieldValues } from 'react-hook-form'
import { FieldValuesFromFieldErrors, ErrorMessage as HookFormErrorMessage } from '@hookform/error-message'
import * as Util from '../util'

/** `@hookform/error-message` のErrorMessageコンポーネントのラッパー */
export const ErrorMessage = <T extends FieldValues = {},>({ name, errors, className }: {
  name: FieldName<FieldValuesFromFieldErrors<T>>
  errors?: FieldErrors<T>
  className?: string
}) => {
  const { data: { darkMode } } = Util.useUserSetting()
  const renderer: MessageRenderer = useCallback(({ message, messages }) => {
    const textColor = darkMode
      ? 'text-rose-200'
      : 'text-rose-600'

    if (!message && !messages) return undefined
    return (
      <ul className={`flex flex-col text-sm whitespace-normal ${textColor} ${className ?? ''}`}>
        {message && (
          <li className="select-all">{message}</li>
        )}
        {messages && Object.entries(messages).map(([, msg], i) => (
          <li key={i} className="select-all">{msg}</li>
        ))}
      </ul>
    )
  }, [darkMode, className])

  return (
    <HookFormErrorMessage
      name={name}
      errors={errors}
      render={renderer}
    />
  )
}

type MessageRenderer = Exclude<Parameters<typeof HookFormErrorMessage>['0']['render'], undefined>

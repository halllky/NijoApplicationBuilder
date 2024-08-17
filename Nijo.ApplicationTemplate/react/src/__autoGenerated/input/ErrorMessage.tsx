import { useMemo } from 'react'
import * as Util from '../util'

export const ErrorMessage = ({ value, className }: {
  value?: string[]
  className?: string
}) => {
  const { data: { darkMode } } = Util.useUserSetting()
  const textColor = useMemo(() => {
    return darkMode ? 'text-rose-200' : 'text-rose-600'
  }, [darkMode])


  if (!value || value.length === 0) return undefined

  return (
    <ul className={`flex flex-col text-sm whitespace-normal ${textColor} ${className ?? ''}`}>
      {value.map((msg, i) => (
        <li key={i} className="select-all">
          {msg}
        </li>
      ))}
    </ul>
  )
}

import * as ReactHookForm from "react-hook-form"
import { ApplicationState } from "./types"

/**
 * エラーメッセージ表示欄
 */
export default function ({ formState, className }: {
  formState: ReactHookForm.FormState<ApplicationState>
  className?: string
}) {
  return (
    <ul className={`flex flex-col gap-1 ${className ?? ''}`}>
      <ErrorMessage>
        {formState.errors.root?.message}
      </ErrorMessage>
    </ul>
  )
}

const ErrorMessage = ({ children }: {
  children: React.ReactNode
}) => {
  return (
    <li className="text-sm truncate text-amber-600">
      {children}
    </li>
  )
}
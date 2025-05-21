import React from "react"
import * as ReactHookForm from "react-hook-form"
import { ApplicationState } from "./types"
import { useValidationContext, ValidationResult, ValidationResultToElement } from "./useValidationContext"

/**
 * エラーメッセージ表示欄。
 * すべての要素のエラーメッセージを羅列する。
 */
export default function ({ getValues, className }: {
  getValues: ReactHookForm.UseFormGetValues<ApplicationState>
  className?: string
}) {
  const { validationResult } = useValidationContext()

  // エラーメッセージをプレーンな配列に変換する。
  const errorMessageList = React.useMemo(() => {

    // IDから当該要素の名前を引き当てるための辞書
    const xmlElements = getValues(`xmlElementTrees`)?.flatMap(x => x.xmlElements) ?? []
    const idToName = new Map(xmlElements.map(x => [x.uniqueId, x.localName]))

    const messages: string[] = []
    for (const [id, obj] of Object.entries(validationResult)) {
      const name = idToName.get(id)

      for (const [objKey, attrMessages] of Object.entries(obj)) {
        const attrName = objKey === '_own' ? '' : `${objKey}.`
        messages.push(...attrMessages.map(x => `${name ?? ''}${attrName}: ${x}`))
      }
    }
    return messages
  }, [getValues, validationResult])

  console.log(errorMessageList)

  return (
    <ul className={`flex flex-col gap-1 ${className ?? ''}`}>
      {errorMessageList.map((message, i) => (
        <ErrorMessage key={i}>
          {message}
        </ErrorMessage>
      ))}
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

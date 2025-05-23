import React from "react"
import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import { ApplicationState } from "./types"
import { SERVER_DOMAIN } from "../NijoUi"

/** 入力検証のコンテキストのデフォルト値。 */
export const DEFAULT_VALIDATION_CONTEXT_VALUE: ValidationContextType = {
  getValidationResult: () => ({ _own: [] }),
  trigger: () => {
    console.log('ValidationContext未定義')
    return Promise.resolve()
  },
  validationResult: {},
}

/** 入力検証のコンテキスト。 */
export const ValidationContext = React.createContext<ValidationContextType>(DEFAULT_VALIDATION_CONTEXT_VALUE)

/** 入力検証のコンテキストを提供する。 */
export const useValidationContextProvider = (
  getValues: ReactHookForm.UseFormGetValues<ApplicationState>
) => {
  // 短時間で繰り返し実行するとサーバーに負担がかかるため、
  // 最後にリクエストした時間から一定時間以内はリクエストをしないようにする。
  const [lastRequestTime, setLastRequestTime] = React.useState<number>(0)

  // サーバーから返ってくる検証結果。
  // 独特な形をしているのでReact Hook Formのエラーとは別に管理する。
  const [validationResult, setValidationResult] = React.useState<ValidationResult>({})

  // 入力検証を実行する。
  const trigger = useEvent(async () => {
    // 最後にリクエストした時間から一定時間以内はリクエストをしない
    const now = Date.now()
    if (now - lastRequestTime < 1000) return
    setLastRequestTime(now)

    // サーバーに問い合わせ。ステータスコード202ならエラーあり。200ならエラーなしなのでエラーをクリアする。
    const result = await fetch(`${SERVER_DOMAIN}/validate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(getValues()),
    })
    if (result.status === 202) {
      const errors: ValidationResult = await result.json()
      setValidationResult(errors)
    } else {
      setValidationResult({})
    }
  })

  // 特定のXML要素に対する検証結果を取得する処理
  const getValidationResult = React.useCallback((id: string): ValidationResultToElement => {
    return validationResult?.[id] ?? { _own: [] }
  }, [validationResult])

  const contextValue: ValidationContextType = React.useMemo(() => ({
    validationResult,
    getValidationResult,
    trigger,
  }), [validationResult, getValidationResult, trigger])

  return contextValue
}

/** 入力検証のコンテキスト。 */
export type ValidationContextType = {
  /** 検証を実行する */
  trigger: () => Promise<void>
  /** 検証結果を取得する */
  validationResult: ValidationResult
  /** 特定のXML要素に対する検証結果を取得する */
  getValidationResult: (id: string) => ValidationResultToElement
}

/**
 * サーバーから返ってくる検証結果の型。
 * この形は、C#側の ToReactErrorObject で生成されるJsonObjectの型と一致する。
 */
export type ValidationResult = {
  [uniqueId: string]: ValidationResultToElement
}
/**
 * サーバーから返ってくる検証結果のうち特定のXML要素に対するもの。
 */
export type ValidationResultToElement = {
  /** この要素自体に対するエラー */
  _own: string[]
  /** この要素の属性に対するエラー */
  [attributeName: string]: string[]
}

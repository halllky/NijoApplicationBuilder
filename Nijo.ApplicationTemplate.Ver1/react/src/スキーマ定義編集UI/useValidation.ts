import React from "react"
import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import { ApplicationState } from "./types"
import { SERVER_DOMAIN } from "./NijoUi"

/**
 * サーバーの入力検証エンドポイントを呼び出し、
 * 返ってきた内容で react hook form の formState の error を書き換える。
 */
export const useValidation = (formMethods: ReactHookForm.UseFormReturn<ApplicationState>) => {

  // 短時間で繰り返し実行するとサーバーに負担がかかるため、
  // 最後にリクエストした時間から一定時間以内はリクエストをしないようにする。
  const [lastRequestTime, setLastRequestTime] = React.useState<number>(0)

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
      body: JSON.stringify(formMethods.getValues()),
    })
    if (result.status === 202) {
      const errors = await result.json()
      console.log(errors)
      formMethods.setError('root', { message: errors.join('\n') })
    } else {
      formMethods.clearErrors('root')
    }
  })

  return {
    /** 入力検証を実行する */
    trigger,
  }
}


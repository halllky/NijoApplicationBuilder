import { useCallback, useMemo } from "react"
import { useUserSetting } from "./UserSetting"
import { useMsgContext } from "./Notification"

type HttpSendResult<T>
  = { ok: true, data: T }
  | { ok: false, errors: unknown }
type ResponseHandler = (response: Response) => Promise<{ success: boolean } | void>

export const useHttpRequest = () => {
  const { data: { apiDomain } } = useUserSetting()
  const [, dispatchMsg] = useMsgContext()

  const dotnetWebApiDomain = useMemo(() => {
    const domain = apiDomain
      ? apiDomain
      : (import.meta.env.VITE_BACKEND_API as string) // .envファイルで指定
    return domain.endsWith('/')
      ? domain.substring(0, domain.length - 1)
      : domain
  }, [apiDomain])

  const sendHttpRequest = useCallback(async <T>(
    /** URLと追加パラメータ */
    [url, option]: Parameters<typeof fetch>,
    /** レスポンスを細かく解釈したい場合に用いる。未指定の場合は既定の解釈処理にかけられる。 */
    responseHandler?: ResponseHandler
  ): Promise<HttpSendResult<T>> => {
    // HTTPリクエスト実行
    let response: Response
    try {
      response = await fetch(url, option)
    } catch (errors) {
      dispatchMsg(msg => msg.error(`通信でエラーが発生しました(${url})\n${parseUnknownErrors(errors).join('\n')}`))
      return { ok: false, errors }
    }
    // 処理結果解釈
    try {
      // レスポンスを解釈する処理が指定されている場合の処理
      const handled = await responseHandler?.(response)
      if (handled) return { ok: handled.success, data: undefined as T, errors: undefined }

      // ここから既定のレスポンス解釈処理
      const data = response.headers.get('content-type')?.toLowerCase().includes('application/json')
        ? await response.json() as unknown
        : await response.text()
      // 正常終了
      if (response.ok) {
        return { ok: true, data: data as T }
      }
      // ASP.NET Core のControllerの return BadRequest ではエラー詳細は response.json() の結果そのまま
      if (response.status >= 400 && response.status <= 499) {
        return { ok: false, errors: data }
      }
      // ASP.NET Core のControllerの return Problem ではエラー詳細はcontentやdetailという名前のプロパティの中に入っている
      if (response.status >= 500 && response.status <= 599) {
        const { detail } = data as { detail: unknown }
        if (detail) throw detail
        const { content } = data as { content: unknown }
        if (content) throw content
      }
      // 処理結果解釈不能
      throw data

    } catch (errors) {
      dispatchMsg(msg => response.ok
        ? msg.warn(`処理は成功しましたが処理結果の解釈に失敗しました。\n${parseUnknownErrors(errors).join('\n')}`)
        : msg.error(`処理に失敗しました。\n${parseUnknownErrors(errors).join('\n')}`))
      return { ok: false, errors }
    }
  }, [dispatchMsg])

  const get = useCallback(async <T = object>(url: string, param: { [key: string]: unknown } = {}): Promise<HttpSendResult<T>> => {
    const query = new URLSearchParams()
    for (const key of Object.keys(param)) {
      let value: string
      switch (typeof param[key]) {
        case 'undefined': value = ''; break
        case 'object': value = JSON.stringify(param[key]); break
        default: value = String(param[key]); break
      }
      query.append(key, value)
    }
    const queryString = query.toString()
    return await sendHttpRequest([queryString ? `${dotnetWebApiDomain}${url}?${queryString}` : `${dotnetWebApiDomain}${url}`])
  }, [dotnetWebApiDomain, sendHttpRequest])

  const post = useCallback(async <T>(url: string, data: object = {}): Promise<HttpSendResult<T>> => {
    return await sendHttpRequest([`${dotnetWebApiDomain}${url}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }])
  }, [dotnetWebApiDomain, sendHttpRequest])

  const postWithHandler = useCallback(async (url: string, data: object, responseHandler: ResponseHandler) => {
    const result = await sendHttpRequest([`${dotnetWebApiDomain}${url}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }], responseHandler)
    return result.ok
  }, [dotnetWebApiDomain, sendHttpRequest])

  const httpDelete = useCallback(async (url: string, data: object = {}) => {
    return await sendHttpRequest([`${dotnetWebApiDomain}${url}`, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }])
  }, [dotnetWebApiDomain, sendHttpRequest])

  const postAndDownload = useCallback(async (url: string, data: object = {}, filename?: string) => {
    return await postWithHandler(url, data, async response => {
      const a = document.createElement('a')
      let blobUrl: string | undefined = undefined
      try {
        const blob = await response.blob()
        const blobUrl = window.URL.createObjectURL(blob)
        a.href = blobUrl
        a.download = filename ?? ''
        document.body.appendChild(a)
        a.click()
        return { success: true }
      } catch (error) {
        throw `ファイルダウンロードに失敗しました: ${error}`
      } finally {
        // 後処理
        if (blobUrl !== undefined) window.URL.revokeObjectURL(blobUrl)
        document.body.removeChild(a)
      }
    })
  }, [postWithHandler])

  return {
    get,
    post,
    /** HTTPレスポンスの内容次第で細かく制御を分けたい場合はこちらを使用 */
    postWithHandler,
    /** POSTリクエストを送信し、レスポンスをデータでなくファイルとして解釈してダウンロードを開始します。 */
    postAndDownload,
    httpDelete,
  }
}

const parseUnknownErrors = (err: unknown): string[] => {
  const type = typeof err
  if (type === 'string') {
    try {
      const parsed = JSON.parse(err as string)
      return parseUnknownErrors(parsed)
    } catch {
      return [err as string]
    }

  } else if (type === 'boolean'
    || type === 'number'
    || type === 'bigint'
    || type === 'function'
    || type === 'symbol') {
    return [String(err)]

  } else if (type === 'undefined' || err === undefined) {
    return []

  } else if (err === null) {
    return []

  } else if (Array.isArray(err)) {
    return err.flatMap(e => parseUnknownErrors(e))

  } else {
    const objectErrorStr = err.toString()
    return objectErrorStr === '[object Object]'
      ? [JSON.stringify(err)]
      : [objectErrorStr]
  }
}

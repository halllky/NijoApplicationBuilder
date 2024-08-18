import { useCallback, useMemo } from "react"
import { useUserSetting } from "./UserSetting"
import { useMsgContext } from "./Notification"

type HttpSendResult<T>
  = { ok: true, data: T }
  | { ok: false, errors: unknown }

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

  const sendHttpRequest = useCallback(async <T>([url, option]: Parameters<typeof fetch>): Promise<HttpSendResult<T>> => {
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
  }, [dotnetWebApiDomain, sendHttpRequest, dispatchMsg])

  const post = useCallback(async <T>(url: string, data: object = {}): Promise<HttpSendResult<T>> => {
    return await sendHttpRequest([`${dotnetWebApiDomain}${url}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }])
  }, [dotnetWebApiDomain, sendHttpRequest, dispatchMsg])

  const httpDelete = useCallback(async (url: string, data: object = {}) => {
    return await sendHttpRequest([`${dotnetWebApiDomain}${url}`, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }])
  }, [dotnetWebApiDomain, sendHttpRequest])

  const download = useCallback(async (url: string, filename?: string) => {
    const a = document.createElement('a')
    let blobUrl: string | undefined = undefined
    try {
      const response = await fetch(`${dotnetWebApiDomain}${url}`)
      const blob = await response.blob()
      const blobUrl = window.URL.createObjectURL(blob)
      a.href = blobUrl
      a.download = filename ?? ''
      document.body.appendChild(a)
      a.click()

    } catch (error) {
      dispatchMsg(msg => msg.error(`ファイルダウンロードに失敗しました: ${error}`))
    } finally {
      if (blobUrl !== undefined) window.URL.revokeObjectURL(blobUrl)
      document.body.removeChild(a)
    }
  }, [dotnetWebApiDomain, dispatchMsg])

  return { get, post, httpDelete, download }
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

  } else if (type === 'undefined') {
    return []

  } else if (err === null) {
    return []

  } else if (Array.isArray(err)) {
    return err.flatMap(e => parseUnknownErrors(e))

  } else {
    return [JSON.stringify(err)]
  }
}

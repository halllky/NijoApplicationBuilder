import { useCallback, useMemo } from "react"
import { useUserSetting } from "./UserSetting"
import { useMsgContext } from "./Notification"

type HttpSendResult<T> = { ok: true, data: T } | { ok: false }

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
    try {
      const response = await fetch(url, option)
      if (response.ok) {
        const text = await response.text()
        try {
          const data = text === ''
            ? '' as T
            : JSON.parse(text) as T
          return { ok: true, data }
        } catch {
          dispatchMsg(msg => msg.warn(`処理は成功しましたがサーバーからのレスポンスを解釈できませんでした: ${text}`))
          return { ok: false }
        }
      } else {
        dispatchMsg(msg => msg.error(`ERROR(${url})`))

        // エラーメッセージの抽出
        let errorMessages: string[]
        const text = await response.text()
        try {
          const parsed = JSON.parse(text)
          if (typeof parsed.content === 'string') {
            errorMessages = [parsed.content]
          } else if (Array.isArray(parsed.content)) {
            errorMessages = parsed.content as string[]
          } else if (Array.isArray(parsed.errors)) {
            errorMessages = parsed.errors as string[]
          } else if (typeof parsed.errors === 'object') {
            errorMessages = Object.values(parsed.errors).flatMap(err => err as string[])
          } else {
            console.log(parsed)
            errorMessages = [text]
          }
        } catch {
          errorMessages = [text]
        }
        dispatchMsg(msg => msg.error(...errorMessages))
        return { ok: false }
      }

    } catch (error) {
      dispatchMsg(msg => msg.error(`${error}(${url})`))
      return { ok: false }
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

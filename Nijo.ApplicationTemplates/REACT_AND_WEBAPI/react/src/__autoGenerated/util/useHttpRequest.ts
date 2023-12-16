import { useCallback } from "react"
import { UUID } from "uuidjs"
import { useAppContext } from "../application/AppContext"
import { BarMessage } from "../decoration/InlineMessageBar"

type HttpSendResult<T> = { ok: true, data: T } | { ok: false, errors: BarMessage[] }

export const useHttpRequest = () => {
  const [{ apiDomain }, dispatch] = useAppContext()

  const sendHttpRequest = useCallback(async <T>([url, option]: Parameters<typeof fetch>): Promise<HttpSendResult<T>> => {
    try {
      const response = await fetch(url, option)
      if (response.ok) {
        const text = await response.text()
        const data = JSON.parse(text) as T
        return { ok: true, data }
      } else {
        dispatch({ type: 'pushMsg', msg: `ERROR(${url})` })

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
        const errors: BarMessage[] = errorMessages.map(text => ({ uuid: UUID.generate(), text }))
        return { ok: false, errors }
      }

    } catch (error) {
      dispatch({ type: 'pushMsg', msg: `${error}(${url})` })
      return { ok: false, errors: [] }
    }
  }, [dispatch])

  const get = useCallback(async <T = object>(url: string, param: { [key: string]: unknown } = {}): Promise<HttpSendResult<T>> => {
    if (!apiDomain) return { ok: false, errors: [{ uuid: UUID.generate(), text: 'サーバーが設定されていません。' }] }
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
    return await sendHttpRequest([queryString ? `${apiDomain}${url}?${queryString}` : `${apiDomain}${url}`])
  }, [apiDomain, sendHttpRequest])

  const post = useCallback(async <T>(url: string, data: object = {}): Promise<HttpSendResult<T>> => {
    if (!apiDomain) return { ok: false, errors: [{ uuid: UUID.generate(), text: 'サーバーが設定されていません。' }] }
    return await sendHttpRequest([`${apiDomain}${url}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }])
  }, [apiDomain, dispatch])

  return { get, post }
}

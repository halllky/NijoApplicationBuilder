import { useCallback } from "react"
import { UUID } from "uuidjs"
import { useAppContext } from "./AppContext"
import { BarMessage } from "../components/InlineMessageBar"

type HttpGetResult<T> = { ok: true, data: T } | { ok: false, errors: BarMessage[] }
type HttpPostResult<T = object> = { ok: true, data: T } | { ok: false, errors: BarMessage[] }

export const useHttpRequest = () => {
  const [{ apiDomain }, dispatch] = useAppContext()

  const get = useCallback(async <T = object>(url: string, param: { [key: string]: unknown } = {}): Promise<HttpGetResult<T>> => {
    const query = new URLSearchParams()
    for (const key of Object.keys(param)) {
      query.append(key, JSON.stringify(param[key]))
    }
    const queryString = query.toString()
    const fullUrl = queryString ? `${apiDomain}${url}?${queryString}` : `${apiDomain}${url}`
    const response = await fetch(fullUrl)
    if (response.ok) {
      const data = JSON.parse(await response.text()) as T
      return { ok: true, data }
    } else {
      dispatch({ type: 'pushMsg', msg: `ERROR(${fullUrl})` })
      const errorText: string[] = Array.from(JSON.parse(await response.text()))
      const errors: BarMessage[] = errorText.map(text => ({ uuid: UUID.generate(), text }))
      return { ok: false, errors }
    }
  }, [apiDomain, dispatch])

  const post = useCallback(async <T>(url: string, data: object = {}): Promise<HttpPostResult<T>> => {
    const fullUrl = `${apiDomain}${url}`
    const response = await fetch(fullUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    })
    if (response.ok) {
      const data = JSON.parse(await response.text()) as T
      return { ok: true, data }
    } else {
      dispatch({ type: 'pushMsg', msg: `ERROR(${fullUrl})` })
      const errorText: string[] = Array.from(JSON.parse(await response.text()))
      const errors: BarMessage[] = errorText.map(text => ({ uuid: UUID.generate(), text }))
      return { ok: false, errors }
    }
  }, [apiDomain, dispatch])

  return { get, post }
}

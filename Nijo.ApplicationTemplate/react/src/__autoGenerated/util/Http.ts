import { useCallback, useMemo } from "react"
import useEvent from "react-use-event-hook"
import { useNavigate } from "react-router-dom"
import * as ReactHookForm from "react-hook-form"
import { UUID } from "uuidjs"
import { useUserSetting } from "./UserSetting"
import { useMsgContext, useToastContext } from "./Notification"
import { FileAttachment } from "../input/FileAttachment"

type HttpSendResult<T>
  = { ok: true, data: T }
  | { ok: false, errors: unknown }
type ResponseHandler = (response: Response) => Promise<{ success: boolean } | void>

export type ComplexPostOptions<TParam extends object> = {
  /** React hook form の setError。エラー情報を画面項目の脇など細かい場所に表示したい場合に指定。 */
  setError?: ReactHookForm.UseFormSetError<TParam>
  /** 「～ですがよろしいですか？」の確認を強制的に無視する */
  ignoreConfirm?: boolean
  /** 処理結果を細かくカスタマイズしたい場合に指定 */
  responseHandler?: (res: Response) => Promise<ResponseHandlerReturns>
  /** 処理結果がファイルダウンロードの場合の既定のファイル名 */
  defaultFileName?: string
}
type ResponseHandlerReturns = {
  /** 処理結果がハンドリング済みか否かを表します。falseの場合、結果未処理として共通処理側でのハンドリングを試みます。 */
  handled: boolean
}

export const useHttpRequest = () => {
  const { data: { apiDomain } } = useUserSetting()
  const [, dispatchMsg] = useMsgContext()
  const [, dispatchToast] = useToastContext()
  const navigate = useNavigate()

  const dotnetWebApiDomain = useMemo(() => {
    const domain = apiDomain
      ? apiDomain
      : (import.meta.env.VITE_BACKEND_API as string) // .envファイルで指定
    return domain?.endsWith('/')
      ? domain.substring(0, domain.length - 1)
      : domain
  }, [apiDomain])

  const handleUnknownResponse = useEvent(async (res: Response) => {
    const response = res.bodyUsed ? res.clone() : res
    const body = response.headers.get('content-type')?.toLowerCase().includes('application/json')
      ? await response.json() as unknown
      : await response.text()
    const summary = res.ok
      ? '処理は成功しましたが処理結果の解釈に失敗しました。'
      : '処理に失敗しました。'

    // ASP.NET Core のControllerの return BadRequest ではエラー詳細は response.json() の結果そのまま
    if (response.status >= 400 && response.status <= 499) {
      dispatchMsg(msg => msg.error([summary, ...parseUnknownErrors(body)].join('\n')))
      return
    }
    // ASP.NET Core のControllerの return Problem ではエラー詳細はcontentやdetailという名前のプロパティの中に入っている
    if (response.status >= 500 && response.status <= 599) {
      const { detail } = body as { detail: unknown }
      if (detail) {
        dispatchMsg(msg => msg.error([summary, ...parseUnknownErrors(detail)].join('\n')))
        return
      }
      const { content } = body as { content: unknown }
      if (content) {
        dispatchMsg(msg => msg.error([summary, ...parseUnknownErrors(content)].join('\n')))
        return
      }
    }
    // 処理結果解釈不能
    dispatchMsg(msg => msg.error([summary, ...parseUnknownErrors(body)].join('\n')))
  })

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

  const post2 = useEvent(async (url: string, data: object = {}) => {
    try {
      return await fetch(`${dotnetWebApiDomain}${url}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    } catch (errors) {
      dispatchMsg(msg => msg.error(`通信でエラーが発生しました(${url})\n${parseUnknownErrors(errors).join('\n')}`))
    }
  })

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

  const complexPost = useEvent(async <
    TResult = unknown,
    TParam extends object = object
  >(
    url: string,
    data: TParam,
    options?: ComplexPostOptions<TParam>,
  ): Promise<{ ok: true, data: TResult } | { ok: false }> => {
    // --------------------------------
    // 送信内容の組み立て

    // 送信内容にファイルが含まれることがあるのでFormData（Content-Type: multipart/form-data）で送信する
    const formData = new FormData()
    const bodyJson = JSON.stringify(data, (_, value) => {
      if (!isFileAttachment(value)) return value // ファイルでない値はそのままJSON化
      if (value.file.length === 0) return null // ファイルが1件も添付されていない場合はnull

      // 項目がファイルの場合、 multipart/form-data の別のパートに分けて送信する。
      // サーバー側では、ここで発番したUUIDを使って、別パートに分かれたファイルを1つのオブジェクトに組み立てる。
      const fileId = UUID.generate()
      formData.append(fileId, value.file[0])
      return { ...value, file: fileId }
    })
    formData.append('data', bodyJson) // 項目のキーはサーバー側と合わせる必要あり
    if (options?.ignoreConfirm) formData.set('ignoreConfirm', 'true') // 項目のキーはサーバー側と合わせる必要あり

    // もし「～ですがよろしいですか？」の確認が発生した場合、
    // 同じパラメータで2回目のHTTPリクエストを行う必要があるため、whileループする
    while (true) {
      let response: Response
      try {
        response = await fetch(`${dotnetWebApiDomain}${url}`, {
          method: 'POST',
          body: formData, // bodyにFormDataのインスタンスを設定した場合は Content-Type は自動的に設定される
          credentials: 'include', // 認証用のCookieを含む
        })
      } catch (errors) {
        dispatchMsg(msg => msg.error(`通信でエラーが発生しました(${url})\n${parseUnknownErrors(errors).join('\n')}`))
        return { ok: false }
      }

      // 任意のハンドリング処理
      if (options?.responseHandler) {
        const { handled } = await options.responseHandler(response)
        if (handled) return { ok: false }
        if (response.bodyUsed) response = response.clone()
      }

      // ******************* 処理成功していないパターン ここから *******************

      // 項目1個に対するエラーメッセージの型。React hook form のsetErrorの仕組みに従ってこの形になっている。サーバー側と合わせる必要あり。
      type ErrorsForOneField = [ReactHookForm.FieldPath<TParam>, { types: { [key: string]: string } }]

      // ---------------------------------------------------
      // Accepted. 「～してもよいですか？」の確認メッセージ表示を意味する
      if (response.status === 202) {
        const data = (await response.json()) as { confirm: string[], detail: ErrorsForOneField[] }

        // 画面上の各項目に警告メッセージ等を表示する
        if (options?.setError) {
          for (const [name, error] of data.detail) {
            options.setError(name, error)
          }
        }

        // ブラウザのコンファームを使って確認メッセージを表示する
        const joinedMessage = data.confirm.join('\n')
        if (!joinedMessage) {
          return { ok: false } // 確認メッセージの文字列が空のケースはあり得ないが念のため
        } else if (!window.confirm(joinedMessage)) {
          return { ok: false } // "OK"が選択されなかった場合は処理中断
        } else {
          // 確認メッセージに対して"OK"が選択されたら確認メッセージを無視するオプションを追加して同じリクエストをもう一度実行する
          formData.set('ignoreConfirm', 'true')
          continue
        }
      }

      // ---------------------------------------------------
      // Unprocessable Content. 入力内容エラー
      if (response.status === 422) {
        const data = (await response.json()) as { detail: ErrorsForOneField[] }
        if (options?.setError) {
          for (const [name, error] of data.detail) {
            options.setError(name, error)
          }
        } else {
          // setError関数未設定の場合
          const messages = data.detail.flatMap((x) => Object.values(x[1].types))
          dispatchMsg(msg => msg.error(messages.join('\n')))
        }
        return { ok: false }
      }

      // ---------------------------------------------------
      // 上記以外のエラー
      if (!response.ok) {
        await handleUnknownResponse(response)
        return { ok: false }
      }

      // ******************* 処理成功のパターン ここから *******************

      // ---------------------------------------------------
      // 単に処理成功した場合
      const contentType = response.headers.get('Content-Type')?.toLowerCase()
      if (!contentType) {
        return { ok: true, data: undefined as TResult }
      }

      // ---------------------------------------------------
      // ファイルダウンロード
      if (contentType !== 'application/json') {
        const a = document.createElement('a')
        let blobUrl: string | undefined = undefined
        try {
          const blob = await response.blob()
          blobUrl = window.URL.createObjectURL(blob)
          a.href = blobUrl
          a.download = options?.defaultFileName ?? ''
          document.body.appendChild(a)
          a.click()
        } catch (error) {
          dispatchMsg(msg => msg.error(`ファイルダウンロードに失敗しました: ${parseUnknownErrors(error).join('\n')}`))
        } finally {
          if (blobUrl !== undefined) window.URL.revokeObjectURL(blobUrl)
          document.body.removeChild(a)
        }
        return { ok: true, data: undefined as TResult }
      }

      // ---------------------------------------------------
      type ResponseJsonType
        = { type: 'data', data: TResult } // 処理結果データ
        | { type: 'redirect', url: string } // 特定画面にリダイレクト
        | { type: 'toast', text?: string } // トーストでメッセージ表示

      const responseJson = await response.json() as ResponseJsonType

      // ---------------------------------------------------
      // データ返却
      if (responseJson.type === 'data') {
        return { ok: true, data: responseJson.data }
      }

      // ---------------------------------------------------
      // リダイレクト
      if (responseJson.type === 'redirect') {
        navigate(responseJson.url)
        return { ok: true, data: undefined as TResult }
      }

      // ---------------------------------------------------
      // 成功したが結果解釈不能の場合（単に成功である旨のみ返す）
      return { ok: true, data: undefined as TResult }
    }
  })

  return {
    get,
    post,
    /** HTTPレスポンスの内容次第で細かく制御を分けたい場合はこちらを使用 */
    post2,
    handleUnknownResponse,
    /** 廃止予定 */
    postWithHandler,
    /** POSTリクエストを送信し、レスポンスをデータでなくファイルとして解釈してダウンロードを開始します。 */
    postAndDownload,
    /**
     * サーバー側処理と組み合わせて複雑なリクエストやレスポンスを実現するPOST送信。
     * 詳細はwebapiプロジェクト側の同名のクラスを参照。
     */
    complexPost,
    httpDelete,
  }
}

/** 添付ファイルの場合は特殊なHTTPリクエストになるので、その判定用関数 */
export const isFileAttachment = (value: unknown): value is (FileAttachment & { file: FileList }) => {
  const attachment = value as FileAttachment | undefined
  return attachment?.file instanceof FileList
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

import React from "react"
import * as ReactHookForm from "react-hook-form"
import { useMsgContext } from "./useMsgContext"
import { useToastContext } from "./useToastContext"
import { UUID } from "uuidjs"

/**
 * バックエンドへのリクエストを行なう。
 * GETリクエストと、ComplexPost（ASP.NET Core 側のPresentationContextの仕組みと統合されたPOSTリクエスト）の2種類のリクエストをサポートする。
 */
export const useHttpRequest = () => {
  const msgContext = useMsgContext()
  const toastContext = useToastContext()

  /** GETリクエストを行なう。 */
  const get = React.useCallback(async <TReturnValue = unknown>(
    /** バックエンドのURL（ドメイン部分除く） */
    subDirectory: string,
    /** クエリパラメーター */
    searchParams: URLSearchParams
  ): Promise<TReturnValue | undefined> => {

    try {
      const url = getBackendUrl(subDirectory) + '?' + searchParams.toString()
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      })
      if (!response.ok) {
        msgContext.error(handleUnknownResponse(response))
        return undefined
      }
      const json = await response.json() as TReturnValue
      return json
    } catch (error) {
      msgContext.error(handleUnknownError(error))
    }
  }, [msgContext])

  /** POSTリクエストを行なう。 */
  const post = React.useCallback(async <TReturnValue = unknown, TRequestBody = unknown>(
    /** バックエンドのURL（ドメイン部分除く） */
    subDirectory: string,
    /** リクエストボディ */
    requestBody: TRequestBody,
  ): Promise<TReturnValue | undefined> => {

    try {
      const url = getBackendUrl(subDirectory)
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestBody),
      })
      if (!response.ok) {
        msgContext.error(handleUnknownResponse(response))
        return undefined
      }
      const json = await response.json() as TReturnValue
      return json
    } catch (error) {
      msgContext.error(handleUnknownError(error))
    }
  }, [msgContext])

  /** ComplexPost（ASP.NET Core 側のPresentationContextの仕組みと統合されたPOSTリクエスト） */
  const complexPost = React.useCallback(async <TReturnValue = unknown, TRequestBody = unknown>(
    /** バックエンドのURL（ドメイン部分除く） */
    subDirectory: string,
    /** リクエストボディ */
    requestBody: TRequestBody,
    /** オプション */
    options?: ComplexPostOptions,
  ): Promise<TReturnValue | undefined> => {
    try {
      // 添付ファイルを処理する。
      // リクエスト全体は Content-Type: multipart/form-data で送信する。
      // リクエスト本体は "complex-post-request-body" という名前のフィールドにJSONをまるごと格納する。
      // 添付ファイルは、ファイル1件ごとにUUIDを発番し、そのUUIDをキーとしてファイル本体のバイナリをそのフィールドに格納する。
      const formData = new FormData()
      const stringifiedRequestBody = JSON.stringify(requestBody, (key, value) => {
        // FileListならばこのタイミングでファイル1件ごとにUUIDを発番し、別フィールドにファイルの実体（Blob）を格納する
        if (value instanceof FileList) {
          const fileMetadataList: FileAttachmentMetadata[] = []
          for (const file of value) {
            const uuid = UUID.generate()
            formData.append(uuid, file)
            fileMetadataList.push({
              id: uuid,
              fileName: file.name,
              fileSize: file.size,
            })
          }
          return fileMetadataList
        }
        return value
      })
      formData.append('complex-post-request-body', stringifiedRequestBody)

      // -------------------------
      // 「～しますがよろしいですか？」の確認メッセージが表示される前の、エラーチェックのみを行なうHTTPリクエストでは、
      // クエリパラメータに `ignoreConfirm` を指定しない。
      // 確認メッセージが承諾された後の本処理を行なうHTTPリクエストでは、クエリパラメータに `ignoreConfirm=true` を指定する。
      // オプションで確認メッセージ無視が指定されている場合は1巡目からignoreConfirmを指定する。
      const searchParams = new URLSearchParams()
      if (options?.ignoreConfirm) {
        searchParams.append('ignore-confirm', 'true')
      }

      while (true) {
        const url = getBackendUrl(subDirectory) + '?' + searchParams.toString()
        const response = await fetch(url, {
          method: 'POST',
          body: formData,
        })

        // HTTPステータスコードが200-299でない場合はエラー。
        // ここで言うエラーは、必須入力漏れなどのエラーが発生したことを示すものではなく、
        // サーバーからの応答が無い、サーバー側で復旧不可のエラーが発生した、などといったものを示す。
        if (!response.ok) {
          msgContext.error(handleUnknownResponse(response))
          return undefined
        }

        // HTTPステータスコードが200-299の場合はレスポンスをJSONとしてパースする
        const json = await response.json() as PresentationContextResponse<TReturnValue>

        // トーストメッセージがある場合はそれを表示
        if (json.toastMessage) {
          toastContext.info(json.toastMessage)
        }

        // レスポンスのエラー詳細情報フィールドに値がある場合はそれを表示
        if (json.detail) {
          if (options?.handleDetailMessage) {
            options.handleDetailMessage(json.detail)
          } else {
            // `オブジェクトパス: メッセージ, メッセージ, ...` という形式に変換する
            msgContext.error(toFlattenStringList(json.detail).join('\n'))
          }
        }

        // 確認メッセージがある場合はそれを表示
        if (json.confirms.length > 0) {
          if (window.confirm(json.confirms.join('\n'))) {
            // 承諾された場合はignoreConfirmをtrueにしてHTTPリクエスト2巡目に進む
            searchParams.set(IGNORE_CONFIRM, 'true')
            continue
          } else {
            // 承諾されなかった場合はリクエストを中止
            return undefined
          }
        }

        return json.returnValue
      }

    } catch (error) {
      msgContext.error(handleUnknownError(error))
    }
  }, [msgContext, toastContext])

  return { get, post, complexPost }
}

// --------------------------------------------

/**
 * バックエンドのURLを取得する。
 */
const getBackendUrl = (subDirectory: string) => {
  // スラッシュ始まりでもそうでなくても受け入れられるようにする
  const subDirectoryWithoutFirstSlash = subDirectory.startsWith('/')
    ? subDirectory.slice(1)
    : subDirectory

  if (import.meta.env.DEV) {
    // ポートは ASP.NET Core の launchSettings.json で指定しているポート
    const backendApi = import.meta.env.VITE_BACKEND_API
    const backendApiWithLastSlash = backendApi.endsWith('/') ? backendApi : (backendApi + '/')
    return `${backendApiWithLastSlash}${subDirectoryWithoutFirstSlash}`
  } else {
    // 本番環境ではReactは1個のjsファイルにバンドルされてASP.NET Core と同じオリジンから配信されるので相対パスでよい。
    return `/${subDirectoryWithoutFirstSlash}`
  }
}

/**
 * 未知のエラーを、画面上に表示できる文字列に変換する。
 */
const handleUnknownError = (error: unknown): string => {
  if (error instanceof Error) {
    return error.message
  } else if (typeof error === 'string') {
    return error
  } else {
    return JSON.stringify(error)
  }
}

/**
 * 未知のレスポンスを、画面上に表示できる文字列に変換する。
 */
const handleUnknownResponse = (response: Response): string => {
  if (response.ok) {
    return '不明なエラーが発生しました'
  } else {
    return response.statusText
  }
}

/**
 * ComplexPost（ASP.NET Core 側のPresentationContextの仕組みと統合されたPOSTリクエスト）で使われるクエリパラメータの名前。
 * 「～しますがよろしいですか？」の確認メッセージが表示される前の、エラーチェックのみを行なうHTTPリクエストでは付されず、
 * 確認メッセージが承諾された後の本処理を行なうHTTPリクエストではtrueが指定される。
 */
const IGNORE_CONFIRM = "ignore-confirm"

export type ComplexPostOptions = {
  /** 確認メッセージを表示しない */
  ignoreConfirm?: true
  /**
   * 詳細メッセージのハンドリングを行なう。
   * 基本的には react-hook-form の `setError` を呼び出す。
   */
  handleDetailMessage?: (detail: DetailMessagesContainer) => void
}

/**
 * ComplexPost（ASP.NET Core 側のPresentationContextの仕組みと統合されたPOSTリクエスト）の戻り値。
 * C#側の `PresentationContextInWebApi` クラスの型と合わせる必要がある。
 */
type PresentationContextResponse<T> = {
  /** 詳細メッセージ。パラメータの型と同じ構造をもち、フィールドごとにそのフィールドに対するメッセージが格納される。 */
  detail: DetailMessagesContainer
  /** 確認メッセージ */
  confirms: string[]
  /** トーストメッセージ */
  toastMessage?: string
  /** バックエンドからの戻り値 */
  returnValue: T
}

/**
 * 詳細メッセージの型。
 * パラメータの型と同じ構造をもち、フィールドごとにそのフィールドに対するメッセージが格納される。
 */
export type DetailMessagesContainer = {
  [key: string]: DetailMessagesContainer | DetailMessage
}

/**
 * 詳細メッセージの型（フィールド1件分）
 */
export type DetailMessage = {
  error?: string[]
  warn?: string[]
  info?: string[]
}

/**
 * オブジェクトを展開してstringの配列にする。
 * 例えば * `{ aaa: { '1': { bbb: { error: ['エラー1'] } } } }` というオブジェクトを
 * `['aaa.1.bbb.error: エラー1']` に変換する。
 */
const toFlattenStringList = (detail: DetailMessagesContainer): string[] => {
  const result: string[] = []
  const collectMessagesRecursively = (path: string[], messages: DetailMessagesContainer | DetailMessage) => {
    for (const [key, value] of Object.entries(messages)) {
      if (isDetailMessage(value)) {
        // error, warn, info を全部まとめて表示
        const allMessages = [...(value.error ?? []), ...(value.warn ?? []), ...(value.info ?? [])]
        result.push(`${path.join('.')}.${key}: ${allMessages.join(', ')}`)
      } else {
        collectMessagesRecursively([...path, key], value)
      }
    }
  }
  collectMessagesRecursively([], detail)
  return result
}

/** 詳細メッセージの型かどうかを判定する。 */
export const isDetailMessage = (value: DetailMessagesContainer | DetailMessage): value is DetailMessage => {
  // error, warn, info のいずれかのフィールドが存在し、かつそのフィールドの値が配列であるかどうかで判定する
  if (typeof value !== 'object' || value === null) return false
  if ('error' in value && Array.isArray(value.error)) return true
  if ('warn' in value && Array.isArray(value.warn)) return true
  if ('info' in value && Array.isArray(value.info)) return true
  return false
}

/**
 * 詳細メッセージを react-hook-form の `setError` によってエラー情報に変換する。
 * FieldError は `types` という1つのフィールドに複数のメッセージを格納するパターンをサポートしているので、
 * この関数は `types` というフィールドに複数のメッセージを格納する。
 */
export const setErrorDetailMessage = (setError: ReactHookForm.UseFormSetError<any>, detail: DetailMessagesContainer) => {
  const setErrorsRecursively = (path: string[], messages: DetailMessagesContainer | DetailMessage) => {
    for (const [key, value] of Object.entries(messages)) {
      if (isDetailMessage(value)) {
        setError(path.join('.'), { types: value })
      } else {
        setErrorsRecursively([...path, key], value)
      }
    }
  }
  setErrorsRecursively([], detail)
}

/**
 * 添付ファイルのメタデータ。
 * ファイル1件ごとに発番されるID、ファイル名、ファイルサイズを保持する。
 */
type FileAttachmentMetadata = {
  id: string
  fileName: string
  fileSize: number
}

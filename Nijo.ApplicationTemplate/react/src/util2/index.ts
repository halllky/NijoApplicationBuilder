import * as ReactHookForm from "react-hook-form"

// UIを全部カスタマイズする場合、これらの処理の実装は自前で行う必要がある

/**
 * エラーメッセージを画面上に表示するフック。
 * ここではダミーなので実装内容は空。
 */
export const useMsgContext = () => {
  return [
    null,
    (fn: (msg: {
      info: (...args: any[]) => {},
      warn: (...args: any[]) => {},
      error: (...args: any[]) => {},
    }) => void) => { }
  ] as const
}

/**
 * サーバーにHTTPリクエストを送るフック。
 * ここではダミーなので実装内容は空。
 */
export const useHttpRequest = () => {

  const get = <T = object>(url: string, param?: {
    [key: string]: unknown;
  }): Promise<HttpSendResult<T>> => {
    throw new Error('未実装')
  }

  const post = <T>(url: string, data?: object): Promise<HttpSendResult<T>> => {
    throw new Error('未実装')
  }

  const complexPost = <
    TResult = unknown,
    TParam extends object = object
  >(
    url: string,
    data: TParam,
    options?: ComplexPostOptions<TParam>,
  ): { ok: true, data: TResult } | { ok: false } => {
    throw new Error('未実装')
  }

  return {
    get,
    post,
    complexPost,
  }
}
type ComplexPostOptions<TParam extends object> = {
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
  handled: false
  /** complexPost処理自体が正常終了したか異常終了したかを表します。 */
  ok?: never
} | {
  handled: true
  ok: boolean
}
type HttpSendResult<T>
  = { ok: true, data: T }
  | { ok: false, errors: unknown }

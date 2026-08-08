/**
 * サーバー側のURL。
 *
 * 開発環境(DEV)では Vite のホットリロードを活用するため Vite のサーバーと
 * ASP.NET Core のサーバーが別々に起動されている。
 * そのため、ASP.NET Core のデバッグ用のオリジンを明示的に指定する必要がある。
 *
 * 本番環境ではクライアント側のソースコードは1個の JavaScript ファイルにバンドルされ
 * ASP.NET Core が静的ファイルとしてそれを返す。
 * そのため JavaScript と C# が同じ環境で動作しているため、単に自身（/）を指定する。
 *
 * 共有デモサイトでは WebService のリバースプロキシ経由で /demo-api/ 配下にAPIが
 * 公開されるため、VITE_API_BASE_URL が設定されている場合はそちらを優先する。
 */
const ASP_NET_CORE_BASE_URL = import.meta.env.VITE_API_BASE_URL
  ?? (import.meta.env.DEV ? 'http://localhost:5290/' : '/')

/**
 * サーバー側エンドポイントを呼び出す。
 * サーバー側ベースURLの解決や、認証用のCookieの付加を責務とする。
 * エラーハンドリングは特に行なっていないため呼び出し側で行うこと。
 *
 * @param endpoint URLのオリジン部分を除くパスおよびクエリパラメータ。URIエンコードは呼び出し側で行う。
 * @param init リクエストの初期化情報。HTTPメソッドやボディなど。
 * @returns レスポンス
 */
export async function callAspNetCoreApiAsync(endpoint: string, init: RequestInit): Promise<Response> {
  const url = endpoint.startsWith('/')
    ? `${ASP_NET_CORE_BASE_URL}${endpoint.slice(1)}`
    : `${ASP_NET_CORE_BASE_URL}${endpoint}`
  return await fetch(url, {
    // 認証用のCookieをHTTPヘッダに付加する
    credentials: 'include',

    // 開発環境では CORS を許可する。ASP.NET Core のサーバーを呼ぶため。
    // より安全にする場合はクライアント側でこのオプションを指定するのをやめ、
    // ASP.NET Core 側で Vite からのリクエストを許可するようにする。
    mode: import.meta.env.DEV ? 'cors' : undefined,

    ...init,
  })
}

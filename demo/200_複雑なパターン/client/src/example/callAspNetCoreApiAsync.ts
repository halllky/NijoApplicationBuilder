/**
 * サーバー側エンドポイントを呼び出す。
 *
 * Vite の開発サーバーが /api への配下のリクエストを ASP.NET Core サーバーへプロキシするため、
 * デバッグ時・本番時とも常に自分自身のオリジンに対してルート相対パスでリクエストすればよい。
 * サーバー側の URL を環境ごとに出し分ける必要はない。
 *
 * このほか、認証用のCookieの付加を責務とする。
 * エラーハンドリングは特に行なっていないため呼び出し側で行うこと。
 *
 * @param endpoint URLのオリジン部分を除くパスおよびクエリパラメータ。URIエンコードは呼び出し側で行う。
 * @param init リクエストの初期化情報。HTTPメソッドやボディなど。
 * @returns レスポンス
 */
export async function callAspNetCoreApiAsync(endpoint: string, init: RequestInit): Promise<Response> {
  const url = endpoint.startsWith('/') ? endpoint : `/${endpoint}`
  return await fetch(url, {
    // 認証用のCookieをHTTPヘッダに付加する
    credentials: 'same-origin',

    ...init,
  })
}

import React from "react"
import * as ReactRouter from "react-router-dom"
import { useNavigate } from "react-router-dom"
import { PageBase } from "../app/PageBase"
import デバッグメニュー from "../debug-rooms/デバッグメニュー"

export const URL = "/"

/**
 * P000_トップページ へ遷移するためのフック
 */
export function useNavigateToP000トップページ() {
  const navigate = useNavigate()
  return React.useCallback(() => {
    navigate(URL)
  }, [navigate])
}

/**
 * ルーティング定義
 */
export default {
  path: URL,
  element: <P000_トップページ />,
  loader: undefined, // 画面初期表示時の読み込み処理がある場合はここで定義
} satisfies ReactRouter.RouteObject

/**
 * P000 トップページ
 */
function P000_トップページ() {
  return (
    <PageBase
      browserTitle="販売管理システム"
      contents={(
        <div className="py-1">
          {import.meta.env.DEV && (
            <デバッグメニュー />
          )}
        </div>
      )}
      className="bg-gray-100"
    />
  )
}

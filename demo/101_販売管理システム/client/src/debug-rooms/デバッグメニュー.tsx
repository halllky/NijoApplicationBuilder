import React from "react"
import { Link } from "react-router-dom"
import * as UIコンポーネントカタログ from "./UIコンポーネントカタログ"
import * as ER図 from "./ER図"
import { callAspNetCoreApiAsync } from "../example/callAspNetCoreApiAsync"
import { Button } from "../ui/Button"
import { useLoginLogout } from "../app/useLoginLogout"

export default function デバッグメニュー() {
  // 開発環境でのみ表示
  if (!import.meta.env.DEV) return null

  const [processing, setProcessing] = React.useState(false)
  const { logoutAsync } = useLoginLogout()
  const handleRecreateDatabase = async () => {
    if (processing) return
    setProcessing(true)
    if (!confirm("データベースを削除して再作成しますか？\n※この操作は取り消せません。")) return
    try {
      // ログイン情報も消えるので一旦ログアウト
      await logoutAsync()

      const res = await callAspNetCoreApiAsync('/example/destroy-and-recreate-database', { method: 'POST' })
      if (res.ok) {
        alert("データベースを再作成しました。")
      } else {
        const detail = await res.text()
        alert(`エラーが発生しました: ${detail}`)
      }
    } catch (error) {
      alert("通信エラーが発生しました。")
      console.error(error)
    } finally {
      setProcessing(false)
    }
  }

  return (
    <div className="flex flex-col gap-2 p-2 bg-white border border-gray-300 rounded">
      <h2 className="text-lg font-bold">デバッグメニュー</h2>
      <span className="text-sm text-gray-500">
        開発環境でのみ表示されるデバッグ用メニューです。
      </span>
      <hr className="border-gray-300" />
      <div className="space-y-2 flex flex-col items-start">
        <Link to={UIコンポーネントカタログ.URL} className="text-blue-600 underline">
          UIコンポーネントカタログへ移動
        </Link>
        <Link to={ER図.URL} className="text-blue-600 underline">
          ER図へ移動
        </Link>
        <Button fill loading={processing} onClick={handleRecreateDatabase}>
          データベース再作成
        </Button>
      </div>
    </div>
  )
}

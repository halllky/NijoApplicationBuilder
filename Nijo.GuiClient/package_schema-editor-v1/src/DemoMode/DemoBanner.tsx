import * as React from "react"
import { useDemoMode } from "./DemoModeProvider"

/**
 * 共有デモサイトの状態を表示する上部バナー。
 * ロック中の表示、デモ101を開くリンク、リセットボタンを提供する。
 * isDemoMode=false のときは何も表示しない。
 */
export const DemoBanner: React.FC = () => {
  const { isDemoMode, lock, isLockedByOther, demoUrl, demoAppStatus, reloadReason, resetDemo } = useDemoMode()
  const [resetting, setResetting] = React.useState(false)
  const [resetError, setResetError] = React.useState<string>()

  if (!isDemoMode) return null

  const handleReset = async () => {
    if (!window.confirm("環境を初期状態にリセットします。現在の編集内容は失われます。よろしいですか?")) return
    setResetting(true)
    setResetError(undefined)
    const result = await resetDemo()
    if (!result.ok) setResetError(result.error)
    setResetting(false)
  }

  return (
    <div className="flex items-center gap-3 bg-amber-100 text-amber-900 text-sm px-3 py-1.5 border-b border-amber-300">
      <span className="font-semibold">共有デモサイト</span>

      {isLockedByOther && lock && (
        <span className="flex items-center gap-1">
          🔒 {lock.reason}
        </span>
      )}

      <span className="text-amber-700">
        デモ101: {demoAppStatus === "running" ? "稼働中" : demoAppStatus === "starting" ? "起動中" : demoAppStatus === "error" ? "エラー" : "停止中"}
      </span>

      <a
        href={demoUrl}
        target="_blank"
        rel="noreferrer"
        className="underline hover:no-underline ml-auto"
      >
        デモ101を開く
      </a>

      <button
        type="button"
        onClick={handleReset}
        disabled={resetting}
        className="px-2 py-0.5 rounded border border-amber-400 hover:bg-amber-200 disabled:opacity-50"
      >
        {resetting ? "リセット中..." : "環境をリセット"}
      </button>

      {resetError && <span className="text-red-700">{resetError}</span>}

      {reloadReason && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded shadow-lg px-6 py-4 text-gray-900">
            <p>{reloadReason}</p>
            <p className="text-sm text-gray-500 mt-1">最新の状態を読み込みます...</p>
          </div>
        </div>
      )}
    </div>
  )
}

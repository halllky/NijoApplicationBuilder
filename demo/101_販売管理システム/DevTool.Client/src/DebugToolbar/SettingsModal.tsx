import React from "react"
import { XMarkIcon } from "@heroicons/react/24/outline"
import { PreviewProcessState } from "./usePreviewState"

/**
 * デバッグ実行プロセス群の稼働状態とログを表示するモーダル。
 * 表示専用であり、編集可能な設定項目は持たない。
 * 閉じる操作（シェードクリック・閉じるボタン）は onClose を呼ぶだけで、
 * 実際に閉じるかどうかの判断はこのコンポーネントを呼ぶ側が行う。
 */
export const SettingsModal = ({ open, onClose, processes, logs }: {
  open: boolean
  onClose: () => void
  processes: PreviewProcessState[]
  logs: Record<string, { stdout: string, stderr: string }>
}) => {
  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">

      {/* シェード */}
      <div className="absolute inset-0 bg-black/25" onClick={onClose} />

      {/* パネル */}
      <div className="relative bg-white rounded shadow-lg w-[720px] max-w-[90vw] max-h-[85vh] flex flex-col">

        {/* ヘッダ */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-gray-200">
          <h2 className="text-base font-bold">デバッグ実行プロセスの状態</h2>
          <button type="button" onClick={onClose} className="p-1 text-gray-500 hover:text-gray-800">
            <XMarkIcon className="w-5 h-5" />
          </button>
        </div>

        {/* プロセス一覧・ログ */}
        <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-4">
          {processes.length === 0 && (
            <p className="text-sm text-gray-500">稼働中のプロセスはありません。</p>
          )}
          {processes.map(p => (
            <div key={p.name} className="flex flex-col gap-1">
              {/* プロセス名と稼働状態 */}
              <div className="flex items-center gap-2 text-sm">
                <span className="font-bold">{p.name}</span>
                <span className={p.isRunning ? 'text-emerald-600' : 'text-gray-500'}>
                  {p.isRunning ? `稼働中 (PID=${p.processId})` : `停止 (ExitCode=${p.exitCode ?? '?'})`}
                </span>
              </div>
              {/* 標準出力・標準エラー出力 */}
              <LogPane text={logs[p.name]?.stdout ?? ''} />
              <LogPane text={logs[p.name]?.stderr ?? ''} isError />
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

/** ログ1本分の表示欄。追記のたびに末尾へ自動スクロールする */
const LogPane: React.FC<{ text: string, isError?: boolean }> = ({ text, isError }) => {
  const ref = React.useRef<HTMLPreElement>(null)

  // DOM要素の末尾へのスクロール位置は描画結果に依存するため、DOM操作としてeffectで行う
  React.useEffect(() => {
    const el = ref.current
    if (el) el.scrollTop = el.scrollHeight
  }, [text])

  return (
    <pre
      ref={ref}
      className={`h-[120px] overflow-auto bg-gray-900 text-xs p-2 whitespace-pre-wrap rounded ${isError ? 'text-rose-400' : 'text-gray-100'}`}
    >
      {text}
    </pre>
  )
}

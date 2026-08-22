import { ArrowTopRightOnSquareIcon, CheckCircleIcon, MinusCircleIcon, XCircleIcon } from "@heroicons/react/24/outline"
import React from "react"
import { PREVIEW_PROCESS_NAMES, PREVIEW_TARGET_ORIGIN } from "../../shared/devtool-api"
import type { useDevToolState } from "../Preview"
import { LogPane } from "../ui"

/** デバッグ実行プロセス（vite・dotnet）の一覧。未起動のプロセスも行として表示し、開始できるようにする */
export const ProcessList: React.FC<{ preview: ReturnType<typeof useDevToolState> }> = ({ preview }) => {
  const { processes, logs, start, stop, restart, isBusy } = preview
  const processByName = React.useMemo(() => new Map(processes.map(p => [p.name, p])), [processes])

  return (
    <div className="flex flex-col gap-4">
      {PREVIEW_PROCESS_NAMES.map(name => {
        const state = processByName.get(name)
        const running = state?.isRunning ?? false
        return (
          <div key={name} className="flex flex-col gap-1">
            <div className="flex items-center gap-2 text-sm">
              {/* プロセス名 */}
              <span className="font-bold">{name}</span>

              {/* 稼働状態 */}
              <span className={running ? 'text-emerald-600' : 'text-gray-500'}>
                {!state ? '未起動' : running ? `稼働中 (PID=${state.processId})` : `停止 (ExitCode=${state.exitCode ?? '?'})`}
              </span>

              {/* viteへの別タブリンク */}
              {name === "vite" && (
                <div className="flex items-center gap-2">
                  <a
                    href={PREVIEW_TARGET_ORIGIN}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center gap-1 p-1 text-sm text-sky-700 underline hover:bg-gray-100 rounded"
                    title="別タブで開く"
                  >
                    {PREVIEW_TARGET_ORIGIN}
                    <ArrowTopRightOnSquareIcon className="w-5 h-5" />
                  </a>
                  <TargetStatusIndicator status={preview.targetStatus} />
                </div>
              )}

              {/* 操作ボタン */}
              <div className="ml-auto flex gap-1">
                <button
                  type="button"
                  onClick={() => (running ? stop(name) : start(name))}
                  disabled={isBusy}
                  className="px-2 py-0.5 text-xs bg-gray-800 text-white rounded disabled:opacity-50"
                >
                  {running ? '停止' : '開始'}
                </button>
                <button
                  type="button"
                  onClick={() => restart(name)}
                  disabled={isBusy}
                  className="px-2 py-0.5 text-xs text-gray-600 hover:bg-gray-100 rounded disabled:opacity-50"
                >
                  再起動
                </button>
              </div>
            </div>
            {/* 標準出力・標準エラー出力 */}
            <LogPane text={logs[name]?.stdout ?? ''} />
            <LogPane text={logs[name]?.stderr ?? ''} isError />
          </div>
        )
      })}
    </div>
  )
}

/** デバッグ対象アプリへの到達状況の表示 */
const TargetStatusIndicator: React.FC<{ status: number | null }> = ({ status }) => {
  if (status === 200) return (
    <CheckCircleIcon title="応答あり" className="h-4 w-4 text-emerald-600" />
  )
  if (status === null) return (
    <XCircleIcon title="応答なし（プロセスが起動していないか、まだ準備中です）" className="h-4 w-4 text-gray-600" />
  )
  return (
    <MinusCircleIcon title={`ステータス ${status} が返っています`} className="h-4 w-4 text-amber-600" />
  )
}

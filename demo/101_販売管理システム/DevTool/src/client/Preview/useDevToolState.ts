import React from "react"
import type { DevToolStateResponse, PreviewProcessState } from "../../shared/devtool-api"

export type { PreviewProcessState }

/** ログ表示欄に保持する行数の上限。これを超えたら古い行から捨てる */
const MAX_LOG_LINES = 2000

/** プロセスごとの標準出力・標準エラー出力のログ本文 */
type PreviewLogText = { stdout: string, stderr: string }

/**
 * デバッグ実行プロセス群の起動・停止・再起動と、
 * 稼働状態・ログ・デバッグ対象アプリへの到達状況・DevToolサーバー自身のログの1秒間隔ポーリングを行う。
 * processName を省略すると全プロセスが対象になる。
 * デバッグ対象アプリが未応答から応答可能（HTTP 200）に変わった瞬間に onTargetReachable を呼ぶ。
 */
export function useDevToolState({ onTargetReachable }: { onTargetReachable: () => void }) {
  const [processes, setProcesses] = React.useState<PreviewProcessState[]>([])
  const [logs, setLogs] = React.useState<Record<string, PreviewLogText>>({})
  const [serverLog, setServerLog] = React.useState('')
  const [targetStatus, setTargetStatus] = React.useState<number | null>(null)
  const [isBusy, setIsBusy] = React.useState(false)
  const [error, setError] = React.useState<string>()

  // 次回ポーリング時に送る「既読オフセット」。再レンダリングに影響しないためrefで保持する
  const offsetsRef = React.useRef<Record<string, { stdout: number, stderr: number }>>({})
  const logsRef = React.useRef<Record<string, PreviewLogText>>({})
  const serverLogOffsetRef = React.useRef(0)
  const serverLogRef = React.useRef('')
  // 直前のポーリングで観測した到達ステータス。200への変化を検知する比較にのみ使うためrefで保持する
  const previousTargetStatusRef = React.useRef<number | null>(null)
  // ポーリングのeffectを張り直さずに、常に最新のコールバックを呼べるようにする
  const onTargetReachableRef = React.useRef(onTargetReachable)
  onTargetReachableRef.current = onTargetReachable

  // サーバー側で稼働しているプロセス群の状態・ログ・到達状況・サーバー自身のログと1秒間隔で同期する
  React.useEffect(() => {
    let cancelled = false

    const poll = async () => {
      try {
        const res = await fetch('/devtool-api/state', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ offsets: offsetsRef.current, serverLogOffset: serverLogOffsetRef.current }),
        })
        if (!res.ok || cancelled) return

        const data: DevToolStateResponse = await res.json()
        if (cancelled) return

        const nextOffsets: typeof offsetsRef.current = {}
        const nextLogs: typeof logsRef.current = { ...logsRef.current }
        for (const p of data.processes) {
          nextOffsets[p.name] = { stdout: p.stdout.offset, stderr: p.stderr.offset }
          nextLogs[p.name] = {
            stdout: trimLog((nextLogs[p.name]?.stdout ?? '') + p.stdout.text),
            stderr: trimLog((nextLogs[p.name]?.stderr ?? '') + p.stderr.text),
          }
        }
        const nextServerLog = trimLog(serverLogRef.current + data.serverLog.text)

        offsetsRef.current = nextOffsets
        logsRef.current = nextLogs
        serverLogOffsetRef.current = data.serverLog.offset
        serverLogRef.current = nextServerLog
        setProcesses(data.processes)
        setLogs(nextLogs)
        setServerLog(nextServerLog)
        setTargetStatus(data.targetStatus)

        // 未到達（またはエラー応答）から200に変わった瞬間だけ、iframeの再読み込みを促す
        if (data.targetStatus === 200 && previousTargetStatusRef.current !== 200) {
          onTargetReachableRef.current()
        }
        previousTargetStatusRef.current = data.targetStatus
      } catch {
        // ポーリング1回分の失敗は無視し、次回のポーリングに委ねる
      }
    }

    poll()
    const timer = window.setInterval(poll, 1000)
    return () => {
      cancelled = true
      window.clearInterval(timer)
    }
  }, [])

  const callAction = React.useCallback(async (action: 'start' | 'stop' | 'restart', processName?: string) => {
    setIsBusy(true)
    setError(undefined)
    try {
      const query = processName ? `?process=${encodeURIComponent(processName)}` : ''
      const res = await fetch(`/devtool-api/preview/${action}${query}`, { method: 'POST' })
      if (!res.ok) setError(await res.text())
      // 停止・再起動後はプレビュープロセスのログバッファがクリアされる場合があるため、そのオフセットだけ先頭に戻す。
      // DevToolサーバー自身のログはこの操作と無関係に続いているため、serverLogOffsetRef はここでは触らない
      // （触ると停止・再起動のたびにサーバーログ全量が再送されてしまう）。
      if (action !== 'start') offsetsRef.current = {}
    } catch (e) {
      setError(e instanceof Error ? e.message : `不明なエラー(${e})`)
    } finally {
      setIsBusy(false)
    }
  }, [])

  const start = React.useCallback((processName?: string) => callAction('start', processName), [callAction])
  const stop = React.useCallback((processName?: string) => callAction('stop', processName), [callAction])
  const restart = React.useCallback((processName?: string) => callAction('restart', processName), [callAction])

  return { processes, logs, serverLog, targetStatus, start, stop, restart, isBusy, error }
}

function trimLog(text: string): string {
  const lines = text.split('\n')
  return lines.length > MAX_LOG_LINES ? lines.slice(lines.length - MAX_LOG_LINES).join('\n') : text
}

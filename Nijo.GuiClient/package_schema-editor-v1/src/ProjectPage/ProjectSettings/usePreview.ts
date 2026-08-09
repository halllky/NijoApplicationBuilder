import React from "react"
import useEvent from "react-use-event-hook"
import { SERVER_DOMAIN } from "../../main"
import { NIJOUI_CLIENT_ROUTE_PARAMS } from "../../routing"
import { PreviewProcessState } from "../../backend"

/** ログ表示欄に保持する行数の上限。これを超えたら古い行から捨てる */
const MAX_LOG_LINES = 2000

/** プロセスごとの標準出力・標準エラー出力のログ本文 */
type PreviewLogText = { stdout: string, stderr: string }

/**
 * プレビュー（生成後アプリのデバッグプロセス）の起動・停止と、
 * 稼働状態・ログの1秒間隔ポーリングを行う。
 */
export function usePreview(projectDir: string | null) {
  const [processes, setProcesses] = React.useState<PreviewProcessState[]>([])
  const [logs, setLogs] = React.useState<Record<string, PreviewLogText>>({})
  const [isBusy, setIsBusy] = React.useState(false)
  const [error, setError] = React.useState<string>()

  // 次回ポーリング時に送る「既読オフセット」。再レンダリングに影響しないためrefで保持する
  const offsetsRef = React.useRef<Record<string, { stdout: number, stderr: number }>>({})
  const logsRef = React.useRef<Record<string, PreviewLogText>>({})

  const query = `${NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR}=${encodeURIComponent(projectDir ?? '')}`

  // サーバー側で稼働しているプレビュープロセス群の状態・ログと1秒間隔で同期する
  React.useEffect(() => {
    let cancelled = false

    const poll = async () => {
      try {
        const res = await fetch(`${SERVER_DOMAIN}/nijo-api/preview/state?${query}`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ offsets: offsetsRef.current }),
        })
        if (!res.ok || cancelled) return

        const data: { processes: PreviewProcessState[] } = await res.json()
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

        offsetsRef.current = nextOffsets
        logsRef.current = nextLogs
        setProcesses(data.processes)
        setLogs(nextLogs)
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
  }, [query])

  const start = useEvent(async () => {
    setIsBusy(true)
    setError(undefined)
    try {
      const res = await fetch(`${SERVER_DOMAIN}/nijo-api/preview/start?${query}`, { method: 'POST' })
      if (!res.ok) setError(await res.text())
    } catch (e) {
      setError(e instanceof Error ? e.message : `不明なエラー(${e})`)
    } finally {
      setIsBusy(false)
    }
  })

  const stop = useEvent(async () => {
    setIsBusy(true)
    setError(undefined)
    try {
      const res = await fetch(`${SERVER_DOMAIN}/nijo-api/preview/stop?${query}`, { method: 'POST' })
      if (!res.ok) setError(await res.text())
      // 停止直後は次回起動時にログファイルがクリアされるため、既読オフセットも先頭に戻す
      offsetsRef.current = {}
    } catch (e) {
      setError(e instanceof Error ? e.message : `不明なエラー(${e})`)
    } finally {
      setIsBusy(false)
    }
  })

  return { processes, logs, start, stop, isBusy, error }
}

function trimLog(text: string): string {
  const lines = text.split('\n')
  return lines.length > MAX_LOG_LINES ? lines.slice(lines.length - MAX_LOG_LINES).join('\n') : text
}

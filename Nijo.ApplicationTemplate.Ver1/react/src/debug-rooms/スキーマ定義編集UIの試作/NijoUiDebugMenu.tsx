import { useEffect, useState } from "react"
import * as Icon from "@heroicons/react/24/outline"
import * as Input from "../../input"
import * as Layout from "../../layout"
import { DebugProcessState } from "./types"
import { SERVER_DOMAIN } from "./NijoUi"

export const NijoUiDebugMenu = () => {
  const [debugState, setDebugState] = useState<DebugProcessState>()
  const [loading, setLoading] = useState<boolean>(false)
  const [error, setError] = useState<string>()

  const fetchDebugState = async () => {
    if (loading) return
    setLoading(true)
    setError(undefined)
    setDebugState(undefined)
    try {
      const response = await fetch(`${SERVER_DOMAIN}/debug-state`)
      if (!response.ok) {
        let errorText = `HTTP error! status: ${response.status}`
        try {
          const errorData = await response.json()
          if (errorData && typeof errorData.message === 'string') {
            errorText = errorData.message
          } else if (typeof errorData === 'string') {
            errorText = errorData
          }
        } catch (e) {
          // JSONパースに失敗した場合は元のエラーテキストを使用
        }
        throw new Error(errorText)
      }
      const data: DebugProcessState = await response.json()
      setDebugState(data)
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('An unknown error occurred')
      }
      setDebugState(undefined)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchDebugState()
  }, [])

  const debugProcessIsRunning
    = debugState?.estimatedPidOfNodeJs !== undefined
    && debugState?.estimatedPidOfAspNetCore !== undefined
    && !isNaN(debugState.estimatedPidOfNodeJs)
    && !isNaN(debugState.estimatedPidOfAspNetCore)

  let state: string
  if (loading) {
    state = ''
  } else if (error) {
    state = 'エラーが発生しました'
  } else if (!debugProcessIsRunning) {
    state = 'デバッグプロセスは開始されていません。'
  } else {
    state = 'デバッグプロセスは実行中です。'
  }

  return (
    <div className="p-2">
      <h2 className="text-xl font-semibold mb-4">デバッグメニュー</h2>

      <hr className="border-gray-300 my-2" />

      <h3 className="flex items-center gap-2">
        <span>
          {state}
        </span>
        <Input.IconButton icon={Icon.ArrowPathIcon} onClick={fetchDebugState} loading={loading} outline mini>
          再読み込み
        </Input.IconButton>
      </h3>

      {error && (
        <div className="text-red-500 mt-3 p-3 bg-red-100 border border-red-400 rounded">
          <p className="font-semibold">エラーが発生しました:</p>
          <pre className="whitespace-pre-wrap">{error}</pre>
        </div>
      )}

      {!debugState && !error && (
        <Layout.NowLoading />
      )}

      {debugState && !error && (
        <div className="flex flex-col gap-2 text-sm mt-2">

          <table className="table-fixed self-start border-collapse border border-gray-300">
            <thead>
              <tr className="border-b border-gray-300 bg-gray-200">
                <th className="w-40 text-left"></th>
                <th className="w-56 text-left">URL設定</th>
                <th className="w-72 text-left">状態</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className=" py-1">Node.js プロセス</td>
                <td className="">
                  <LinkText url={debugState.nodeJsDebugUrl}>
                    {debugState.nodeJsDebugUrl}
                  </LinkText>
                </td>
                <td className="">
                  <PidText pid={debugState.estimatedPidOfNodeJs} />
                </td>
              </tr>
              <tr>
                <td className="py-1">ASP.NET Core プロセス</td>
                <td className="">
                  <LinkText url={debugState.aspNetCoreDebugUrl}>
                    {debugState.aspNetCoreDebugUrl}
                  </LinkText>
                </td>
                <td className="">
                  <PidText pid={debugState.estimatedPidOfAspNetCore} />
                </td>
              </tr>
            </tbody>
          </table>

          <details className="text-xs">
            <summary className="cursor-pointer text-gray-600">プロセスID検索時コンソール出力</summary>
            <pre className="bg-gray-800 text-white p-2 resize-y h-64 overflow-y-auto text-xs">
              {debugState.consoleOut || '(出力なし)'}
            </pre>
          </details>
        </div>
      )}
    </div>
  )
}

/** リンクを表示する。 */
const LinkText = ({ url, children }: { url: string | undefined, children: React.ReactNode }) => {
  if (!url) {
    return <span>{children}</span>
  }
  return (
    <a href={url} target="_blank" rel="noopener noreferrer" className="text-sky-600 hover:underline underline">
      {children}
    </a>
  )
}

/** プロセスIDを表示する。 */
const PidText = ({ pid }: { pid: number | undefined }) => {
  if (pid === undefined) {
    return (
      <span className="text-gray-400 text-xs">
        このポートで実行されているプロセスはありません。
      </span>
    )
  }
  return (
    <span className="">
      実行中（推定PID: {pid}）
    </span>
  )
}

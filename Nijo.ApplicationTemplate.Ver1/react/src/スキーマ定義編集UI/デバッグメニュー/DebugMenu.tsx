import { useEffect, useState } from "react"
import * as ReactRouter from "react-router-dom"
import * as Icon from "@heroicons/react/24/outline"
import * as Input from "../../input"
import * as Layout from "../../layout"
import { DebugProcessState, SchemaDefinitionOutletContextType } from "../スキーマ定義編集/types"
import { SERVER_DOMAIN } from "../../routes"
import useEvent from "react-use-event-hook"

export const NijoUiDebugMenu = () => {
  const { formMethods, validationContext: { trigger } } = ReactRouter.useOutletContext<SchemaDefinitionOutletContextType>()
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

  const [startNpmDebuggingProcessing, setStartNpmDebuggingProcessing] = useState(false)
  const startNpmDebugging = useEvent(async () => {
    if (anyCommandProcessing) return
    setStartNpmDebuggingProcessing(true)
    const response = await fetch(`${SERVER_DOMAIN}/start-npm-debugging`, {
      method: 'POST',
    })
    if (!response.ok) {
      throw new Error('Failed to start debugging')
    }
    const data: DebugProcessState = await response.json()
    setDebugState(data)
    setStartNpmDebuggingProcessing(false)
  })

  const [startDotnetDebuggingProcessing, setStartDotnetDebuggingProcessing] = useState(false)
  const startDotnetDebugging = useEvent(async () => {
    if (anyCommandProcessing) return
    setStartDotnetDebuggingProcessing(true)
    const response = await fetch(`${SERVER_DOMAIN}/start-dotnet-debugging`, {
      method: 'POST',
    })
    if (!response.ok) {
      throw new Error('Failed to start debugging')
    }
    const data: DebugProcessState = await response.json()
    setDebugState(data)
    setStartDotnetDebuggingProcessing(false)
  })

  const [stopNpmDebuggingProcessing, setStopNpmDebuggingProcessing] = useState(false)
  const stopNpmDebugging = useEvent(async () => {
    if (anyCommandProcessing) return
    setStopNpmDebuggingProcessing(true)
    const response = await fetch(`${SERVER_DOMAIN}/stop-npm-debugging`, {
      method: 'POST',
    })
    if (!response.ok) {
      throw new Error('Failed to stop debugging')
    }
    const data: DebugProcessState = await response.json()
    setDebugState(data)
    setStopNpmDebuggingProcessing(false)
  })

  const [stopDotnetDebuggingProcessing, setStopDotnetDebuggingProcessing] = useState(false)
  const stopDotnetDebugging = useEvent(async () => {
    if (anyCommandProcessing) return
    setStopDotnetDebuggingProcessing(true)
    const response = await fetch(`${SERVER_DOMAIN}/stop-dotnet-debugging`, {
      method: 'POST',
    })
    if (!response.ok) {
      throw new Error('Failed to stop debugging')
    }
    const data: DebugProcessState = await response.json()
    setDebugState(data)
    setStopDotnetDebuggingProcessing(false)
  })

  const [regenerateProcessing, setRegenerateProcessing] = useState(false)
  const regenerateCode = useEvent(async () => {
    if (anyCommandProcessing) return
    setRegenerateProcessing(true)
    setError(undefined)
    const applicationState = formMethods.getValues()

    try {
      const response = await fetch(`${SERVER_DOMAIN}/generate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(applicationState),
      })
      if (!response.ok) {
        let errorText = `HTTP error! status: ${response.status}`
        try {
          const errorData = await response.json()
          if (errorData && typeof errorData.message === 'string') {
            errorText = errorData.message
          } else if (Array.isArray(errorData) && errorData.length > 0 && typeof errorData[0] === 'string') {
            errorText = errorData[0]
          } else if (typeof errorData === 'string') {
            errorText = errorData
          }
        } catch (e) {
          // JSONパースに失敗した場合は元のエラーテキストを使用
        }
        throw new Error(errorText)
      }
      // 204なら入力検証エラー
      if (response.status === 204) {
        trigger()
        return
      }
      // 204以外の成功は再読み込み
      await fetchDebugState()
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('An unknown error occurred during regeneration.')
      }
    } finally {
      setRegenerateProcessing(false)
    }
  })

  const anyCommandProcessing = loading
    || startNpmDebuggingProcessing
    || stopNpmDebuggingProcessing
    || startDotnetDebuggingProcessing
    || stopDotnetDebuggingProcessing
    || regenerateProcessing

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
    <div className="p-2 h-full overflow-y-auto">
      <h2 className="flex items-center gap-2">
        <span className="font-semibold">
          デバッグメニュー
        </span>
        <div className="basis-4" />
        <Input.IconButton icon={Icon.ArrowPathIcon} onClick={fetchDebugState} loading={anyCommandProcessing} outline mini>
          再読み込み
        </Input.IconButton>
        <Input.IconButton icon={Icon.ArrowPathIcon} onClick={regenerateCode} loading={anyCommandProcessing} fill mini>
          再生成
        </Input.IconButton>
      </h2>

      <hr className="border-gray-300 my-2" />

      {debugState?.errorSummary && (
        <div className="text-red-500 mt-3 p-1 bg-red-100 border border-rose-500">
          {debugState.errorSummary}
        </div>
      )}

      <span>
        {state}
      </span>

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

          <table className="table-fixed border-collapse border border-gray-300">
            <thead>
              <tr className="border-b border-gray-300 bg-gray-200">
                <th className="w-40 text-left"></th>
                <th className="w-36 text-left">操作</th>
                <th className="w-56 text-left">URL</th>
                <th className="text-left">状態</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className=" py-1">Node.js プロセス</td>
                <td>
                  <div className="flex gap-1">
                    <Input.IconButton icon={Icon.StopIcon} onClick={stopNpmDebugging} loading={anyCommandProcessing} outline mini>
                      停止
                    </Input.IconButton>
                    <Input.IconButton icon={Icon.PlayIcon} onClick={startNpmDebugging} loading={anyCommandProcessing} outline mini>
                      開始
                    </Input.IconButton>
                  </div>
                </td>
                <td className="">
                  <LinkText url={debugState.nodeJsDebugUrl}>
                    {debugState.nodeJsDebugUrl}
                  </LinkText>
                </td>
                <td className="">
                  <ProcessInfoText
                    pid={debugState.estimatedPidOfNodeJs}
                    processName={debugState.nodeJsProcessName}
                  />
                </td>
              </tr>
              <tr>
                <td className="py-1">ASP.NET Core プロセス</td>
                <td>
                  <div className="flex gap-1">
                    <Input.IconButton icon={Icon.StopIcon} onClick={stopDotnetDebugging} loading={anyCommandProcessing} outline mini>
                      停止
                    </Input.IconButton>
                    <Input.IconButton icon={Icon.PlayIcon} onClick={startDotnetDebugging} loading={anyCommandProcessing} outline mini>
                      開始
                    </Input.IconButton>
                  </div>
                </td>
                <td className="">
                  <LinkText url={debugState.aspNetCoreDebugUrl}>
                    {debugState.aspNetCoreDebugUrl}
                  </LinkText>
                </td>
                <td className="">
                  <ProcessInfoText
                    pid={debugState.estimatedPidOfAspNetCore}
                    processName={debugState.aspNetCoreProcessName}
                  />
                </td>
              </tr>
            </tbody>
          </table>

          <details className="text-xs">
            <summary className="cursor-pointer text-gray-600">処理詳細</summary>
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

/** プロセス情報を表示する。 */
const ProcessInfoText = ({ pid, processName }: { pid: number | undefined, processName: string | undefined }) => {
  if (pid === undefined) {
    return (
      <span className="text-gray-400 text-xs">
        このポートで実行されているプロセスはありません。
      </span>
    )
  }
  return (
    <span className="">
      実行中（PID: {pid}, プロセス名: {processName}）
    </span>
  )
}

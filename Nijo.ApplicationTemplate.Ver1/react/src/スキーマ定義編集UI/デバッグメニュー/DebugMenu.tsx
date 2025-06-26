import React, { useEffect, useState } from "react"
import * as ReactRouter from "react-router-dom"
import * as Icon from "@heroicons/react/24/outline"
import * as Input from "../../input"
import * as Layout from "../../layout"
import { DebugProcessState, SchemaDefinitionOutletContextType } from "../スキーマ定義編集/types"
import { SERVER_DOMAIN } from "../../routes"
import useEvent from "react-use-event-hook"
import { ToTopPageButton } from "../ToTopPageButton"
import useQueryEditorServerApi from "../../データプレビュー/useQueryEditorServerApi"
import { BACKEND_URL } from "../../データプレビュー/IndexAsNijoUiPage"

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
    setDebugState(state => ({ ...state, consoleOut: '' }))
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
    setDebugState(state => ({ ...state, consoleOut: '' }))
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
    setDebugState(state => ({ ...state, consoleOut: '' }))
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
    setDebugState(state => ({ ...state, consoleOut: '' }))
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
    setDebugState(state => ({ ...state, consoleOut: '' }))
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

  // ログの値が変わったときは末尾にスクロール
  const logRef = React.useRef<HTMLPreElement>(null)
  React.useEffect(() => {
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight
    }
  }, [debugState?.consoleOut])

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

  return (
    <div className="p-2 h-full overflow-y-auto flex flex-col">
      <h2 className="flex items-center gap-2">
        <ToTopPageButton />
        <Icon.ChevronRightIcon className="w-4 h-4" />
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
        <div className="text-rose-500 text-sm mt-3 p-1">
          {debugState.errorSummary}
        </div>
      )}

      {error && (
        <pre className="text-rose-500 mt-3 p-3 text-sm whitespace-pre-wrap">
          エラーが発生しました: {error}
        </pre>
      )}

      {!debugState && !error && (
        <Layout.NowLoading />
      )}

      {debugState && !error && (
        <div className="flex-1 overflow-hidden flex flex-col gap-2 text-sm mt-2">

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

          <span className="text-xs cursor-pointer text-gray-600">
            ログ
          </span>

          <pre ref={logRef} className="flex-1 overflow-y-auto text-xs bg-gray-800 text-white p-2">
            {debugState.consoleOut}
          </pre>

          <ResetDatabase anyCommandProcessing={anyCommandProcessing} />
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


/**
 * データベース初期化欄
 */
const ResetDatabase = ({ anyCommandProcessing }: { anyCommandProcessing: boolean }) => {

  // データベース初期化関連のstate
  const { getDummyDataGenerateOptions, destroyAndResetDatabase } = useQueryEditorServerApi(BACKEND_URL)
  const [dummyDataGenerateOptions, setDummyDataGenerateOptions] = useState<{ [key: string]: boolean }>({})
  const [dbResetMessage, setDbResetMessage] = useState<string | null>(null)
  const [dbResetError, setDbResetError] = useState<string | null>(null)
  const [dbResetProcessing, setDbResetProcessing] = useState(false)

  React.useEffect(() => {
    (async () => {
      const result = await getDummyDataGenerateOptions()
      if (result.ok) {
        setDummyDataGenerateOptions(result.data)
        setDbResetError(null)
      } else {
        setDbResetError(result.error)
        setDummyDataGenerateOptions({})
      }
    })()
  }, [])

  // データベース初期化関連の関数
  const selectAll = Object.values(dummyDataGenerateOptions).every(value => value)
  const handleSelectAll = useEvent((e: React.ChangeEvent<HTMLInputElement>) => {
    setDummyDataGenerateOptions(state => {
      const newState = { ...state }
      Object.keys(newState).forEach(key => {
        newState[key as keyof typeof newState] = e.target.checked
      })
      return newState
    })
  })

  const handleResetDatabase = useEvent(async () => {
    if (anyCommandProcessing || dbResetProcessing) return
    if (!window.confirm('データベースを初期化 (データ消去＆再作成) しますか？')) {
      return
    }
    setDbResetMessage(null)
    setDbResetError(null)
    setDbResetProcessing(true)
    try {
      const result = await destroyAndResetDatabase(dummyDataGenerateOptions)
      if (result.ok) {
        setDbResetMessage('データベースが正常に初期化されました。')
      } else {
        setDbResetError(result.error)
        setDbResetMessage(null)
      }
    } catch (e: unknown) {
      setDbResetError(`予期せぬエラーが発生しました: ${e instanceof Error ? e.message : '不明なエラー'}`)
      console.error('Unhandled error during database reset:', e)
    } finally {
      setDbResetProcessing(false)
    }
  })

  return (
    <div className="mt-8 flex flex-col items-start gap-1 p-2 border border-gray-300 mt-2">
      <div className="flex items-center gap-2">
        <h2 className="font-semibold">
          データベース初期化 (データ消去＆再作成)
        </h2>
        <span className="text-xs text-gray-500">
          ※ASP.NET Core プロセスが開始されている必要があります。
        </span>
      </div>

      <hr className="self-stretch border-gray-300 my-2" />

      <div className="flex gap-4 items-start">
        <Input.IconButton onClick={handleResetDatabase} fill loading={dbResetProcessing || anyCommandProcessing}>
          実行
        </Input.IconButton>
        <div className="flex flex-col gap-1">
          <p className="text-xs">
            ダミーデータ投入テーブル選択
          </p>
          <div className="flex flex-wrap gap-x-2 gap-y-1">
            <label>
              <input type="checkbox" checked={selectAll} onChange={handleSelectAll} />
              すべて選択
            </label>
            {Object.entries(dummyDataGenerateOptions).map(([key, value]) => (
              <label key={key}>
                <input type="checkbox" checked={value} onChange={e => {
                  setDummyDataGenerateOptions(state => {
                    const newState = { ...state }
                    newState[key as keyof typeof newState] = e.target.checked
                    return newState
                  })
                }} />
                {key}
              </label>
            ))}
          </div>
        </div>
      </div>
      {dbResetMessage && <p style={{ color: 'green', marginTop: '10px' }}>{dbResetMessage}</p>}
      {dbResetError && <p style={{ color: 'red', marginTop: '10px' }}>{dbResetError}</p>}
    </div>
  )
}

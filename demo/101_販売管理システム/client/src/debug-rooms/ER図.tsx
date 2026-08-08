import React from "react"
import * as ReactRouter from "react-router-dom"
import { Link } from "react-router-dom"
import { GraphView2 } from "@nijo/ui-components"
import { callAspNetCoreApiAsync } from "../example/callAspNetCoreApiAsync"
import { Button } from "../ui/Button"
import { NowLoading } from "../ui/NowLoading"
import { PageBase } from "../app/PageBase"
import { PageTitle } from "../ui/PageTitle"

export const URL = "/debug/er-diagram"

type DataSetKey = "logicalName" | "physicalName"
type Position = { x: number, y: number }
type DataSet = {
  nodes: GraphView2.Node[]
  edges: GraphView2.Edge[]
}
type SavedLayout = {
  nodePositions?: Record<string, Position>
  defaultPan?: Position
  defaultZoom?: number
}
type SavedDataSet = DataSet & SavedLayout
type LoadResponse = {
  logicalNameDataSet: DataSet
  physicalNameDataSet: DataSet
  savedState: {
    logicalName?: SavedDataSet
    physicalName?: SavedDataSet
    selectedDataSetKey?: DataSetKey
  } | null
}

export default {
  path: URL,
  element: <ER図画面 />,
} satisfies ReactRouter.RouteObject

function ER図画面() {
  const graphViewRef = React.useRef<GraphView2.GraphViewRef | null>(null)
  const saveTimerRef = React.useRef<number | null>(null)
  const latestRef = React.useRef<{
    dataSets: Record<DataSetKey, DataSet> | null
    savedLayout: SavedLayout
    selectedDataSetKey: DataSetKey
  }>({
    dataSets: null,
    savedLayout: {},
    selectedDataSetKey: "logicalName",
  })

  const [loading, setLoading] = React.useState(true)
  const [saving, setSaving] = React.useState(false)
  const [error, setError] = React.useState<string | null>(null)
  const [saveMessage, setSaveMessage] = React.useState<string>("")
  const [graphKey, setGraphKey] = React.useState(0)
  const [dataSets, setDataSets] = React.useState<Record<DataSetKey, DataSet> | null>(null)
  const [savedLayout, setSavedLayout] = React.useState<SavedLayout>({})
  const [selectedDataSetKey, setSelectedDataSetKey] = React.useState<DataSetKey>("logicalName")

  const normalizeDataSetKey = React.useCallback((value: string | undefined | null): DataSetKey => {
    return value === "physicalName" ? "physicalName" : "logicalName"
  }, [])

  React.useEffect(() => {
    latestRef.current = {
      dataSets,
      savedLayout,
      selectedDataSetKey,
    }
  }, [dataSets, savedLayout, selectedDataSetKey])

  const normalizeSavedLayout = React.useCallback((savedState: LoadResponse["savedState"]): SavedLayout => {
    const savedDataSet = savedState?.logicalName ?? savedState?.physicalName
    return {
      nodePositions: savedDataSet?.nodePositions,
      defaultPan: savedDataSet?.defaultPan,
      defaultZoom: savedDataSet?.defaultZoom,
    }
  }, [])

  const loadDiagramAsync = React.useCallback(async () => {
    if (saveTimerRef.current !== null) {
      window.clearTimeout(saveTimerRef.current)
      saveTimerRef.current = null
    }
    setLoading(true)
    setError(null)

    try {
      const response = await callAspNetCoreApiAsync("/debug/er-diagram", { method: "GET" })
      if (!response.ok) {
        const detail = await response.text()
        throw new Error(detail || "ER図の取得に失敗しました。")
      }

      const body: LoadResponse = await response.json()
      const nextDataSets = {
        logicalName: body.logicalNameDataSet,
        physicalName: body.physicalNameDataSet,
      } satisfies Record<DataSetKey, DataSet>

      setDataSets(nextDataSets)
      setSavedLayout(normalizeSavedLayout(body.savedState))
      setSelectedDataSetKey(normalizeDataSetKey(body.savedState?.selectedDataSetKey))
      setGraphKey(prev => prev + 1)
      setSaveMessage(body.savedState ? "保存済みレイアウトを読み込みました。" : "")
    } catch (loadError) {
      console.error(loadError)
      setError(loadError instanceof Error ? loadError.message : `不明なエラー(${loadError})`)
    } finally {
      setLoading(false)
    }
  }, [normalizeDataSetKey, normalizeSavedLayout])

  React.useEffect(() => {
    void loadDiagramAsync()
  }, [loadDiagramAsync])

  const persistAsync = React.useCallback(async () => {
    const snapshot = latestRef.current
    if (!snapshot.dataSets) return

    setSaving(true)
    setSaveMessage("保存中...")

    try {
      const requestBody = {
        logicalName: {
          nodes: snapshot.dataSets.logicalName.nodes,
          edges: snapshot.dataSets.logicalName.edges,
          nodePositions: snapshot.savedLayout.nodePositions ?? {},
          defaultPan: snapshot.savedLayout.defaultPan,
          defaultZoom: snapshot.savedLayout.defaultZoom,
        },
        physicalName: {
          nodes: snapshot.dataSets.physicalName.nodes,
          edges: snapshot.dataSets.physicalName.edges,
          nodePositions: snapshot.savedLayout.nodePositions ?? {},
          defaultPan: snapshot.savedLayout.defaultPan,
          defaultZoom: snapshot.savedLayout.defaultZoom,
        },
        selectedDataSetKey: snapshot.selectedDataSetKey,
      }

      const response = await callAspNetCoreApiAsync("/debug/er-diagram/layout", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(requestBody),
      })
      if (!response.ok) {
        const detail = await response.text()
        throw new Error(detail || "レイアウトの保存に失敗しました。")
      }

      setSaveMessage("保存しました。")
    } catch (saveError) {
      console.error(saveError)
      setSaveMessage("保存に失敗しました。")
      setError(saveError instanceof Error ? saveError.message : `不明なエラー(${saveError})`)
    } finally {
      setSaving(false)
    }
  }, [])

  const schedulePersist = React.useCallback(() => {
    if (saveTimerRef.current !== null) {
      window.clearTimeout(saveTimerRef.current)
    }
    setSaveMessage("保存待機中...")
    saveTimerRef.current = window.setTimeout(() => {
      saveTimerRef.current = null
      void persistAsync()
    }, 800)
  }, [persistAsync])

  React.useEffect(() => {
    return () => {
      if (saveTimerRef.current !== null) {
        window.clearTimeout(saveTimerRef.current)
      }
    }
  }, [])

  const mergeViewState = React.useCallback((viewState: ReturnType<GraphView2.GraphViewRef["getViewState"]>) => {
    setSavedLayout({
      nodePositions: viewState.nodePositions,
      defaultPan: viewState.defaultPan,
      defaultZoom: viewState.defaultZoom,
    })
  }, [])

  const captureCurrentViewState = React.useCallback(() => {
    const graphView = graphViewRef.current
    if (!graphView) return
    mergeViewState(graphView.getViewState())
  }, [mergeViewState])

  const handleNodePositionChanged = React.useCallback((nodes: { id: string, position: Position }[]) => {
    setSavedLayout(prev => {
      const nextNodePositions = { ...(prev.nodePositions ?? {}) }
      for (const node of nodes) {
        nextNodePositions[node.id] = node.position
      }
      return {
        ...prev,
        nodePositions: nextNodePositions,
      }
    })
    schedulePersist()
  }, [schedulePersist])

  const handlePanZoomChanged = React.useCallback(({ pan, zoom }: { pan: Position, zoom: number }) => {
    setSavedLayout(prev => {
      return {
        ...prev,
        defaultPan: pan,
        defaultZoom: zoom,
      }
    })
    schedulePersist()
  }, [schedulePersist])

  const handleChangeDataSet = React.useCallback((next: DataSetKey) => {
    if (latestRef.current.selectedDataSetKey === next) return
    captureCurrentViewState()
    setSelectedDataSetKey(next)
    setGraphKey(prev => prev + 1)
    schedulePersist()
  }, [captureCurrentViewState, schedulePersist])

  const activeDataSet = dataSets?.[selectedDataSetKey]

  return (
    <PageBase
      browserTitle="ER図"
      header={(
        <>
          <PageTitle>ER図</PageTitle>
          <Link to="/" className="text-sm text-teal-700 underline">
            デバッグメニューへ戻る
          </Link>
        </>
      )}
      contents={(
        <div className="flex min-h-0 flex-1 flex-col gap-3 py-2">
          <div className="flex flex-wrap items-center gap-2">
            <Button fill={selectedDataSetKey === "logicalName"} outline={selectedDataSetKey !== "logicalName"} onClick={() => handleChangeDataSet("logicalName")}>
              論理名
            </Button>
            <Button fill={selectedDataSetKey === "physicalName"} outline={selectedDataSetKey !== "physicalName"} onClick={() => handleChangeDataSet("physicalName")}>
              物理名
            </Button>
            <Button outline onClick={() => void loadDiagramAsync()} loading={loading}>
              再読み込み
            </Button>
            <span className="ml-auto text-sm text-gray-500">
              {saving ? "保存中..." : saveMessage}
            </span>
          </div>

          <div className="text-sm text-gray-500">
            テーブルやカラムは編集できません。ドラッグでノード配置を変更すると、開発環境のサーバーに送信され JSON として保存されます。
          </div>

          {error && (
            <div className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              {error}
            </div>
          )}

          <div className="relative min-h-[36rem] flex-1 overflow-hidden rounded border border-gray-300 bg-white">
            {activeDataSet && (
              <GraphView2.GraphView2
                key={`${selectedDataSetKey}-${graphKey}`}
                ref={graphViewRef}
                nodes={activeDataSet.nodes}
                edges={activeDataSet.edges}
                defaultNodePositions={savedLayout.nodePositions}
                defaultPan={savedLayout.defaultPan}
                defaultZoom={savedLayout.defaultZoom}
                onNodePositionChanged={handleNodePositionChanged}
                onPanZoomChanged={handlePanZoomChanged}
                showGrid
                className="h-full w-full"
              />
            )}
            {loading && <NowLoading opacity={0.2} />}
          </div>
        </div>
      )}
      className="bg-gray-100"
    />
  )
}

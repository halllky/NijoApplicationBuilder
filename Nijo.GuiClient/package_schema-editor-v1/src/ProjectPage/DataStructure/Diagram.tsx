import React from "react";
import useEvent from "react-use-event-hook";
import * as ReactHookForm from "react-hook-form";
import { GraphView2 } from "@nijo/ui-components";
import { EditingProject, EditingSchemaGraphViewState } from "../../backend";
import { NodeMetadata, useDiagramDataSet } from "./useDiagramDataSet";
import { useDiagramPanZoomSaving } from "./useDiagramPanZoomSaving";

export type DiagramRef = {
  graphViewRef: React.RefObject<GraphView2.GraphViewRef | null>
  getGraphDataSet: () => EditingSchemaGraphViewState
}

/**
 * スキーマ定義ダイアグラム
 */
export function Diagram(props: {
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
  onSelectedRootAggregateChanged: (aggregateId: string | null) => void
  diagramRef: React.RefObject<DiagramRef | null>
  className?: string
  /** 左肩部分にオーバーレイで表示される */
  children?: React.ReactNode
}) {

  const graphViewRef = React.useRef<GraphView2.GraphViewRef | null>(null)
  const { nodes, edges } = useDiagramDataSet(props.formMethods)
  const graphViewState = ReactHookForm.useWatch({ name: 'graphViewState', control: props.formMethods.control });

  // パン、ズームの localStorage への保存
  const { defaultPan, defaultZoom, handlePanZoomChanged } = useDiagramPanZoomSaving()

  // 選択範囲変更時
  const handleSelectionChange = useEvent(() => {
    const selectedNodes = graphViewRef.current?.getSelectedNodes()
    if (!selectedNodes || selectedNodes.length === 0) {
      props.onSelectedRootAggregateChanged(null)
      return;
    }

    // 選択されたノードがルート集約の場合、そのIDを通知
    const meta = selectedNodes[0].meta as NodeMetadata
    props.onSelectedRootAggregateChanged(meta.rootAggregateUniqueId)
  })

  // ref
  React.useImperativeHandle(props.diagramRef, () => ({
    graphViewRef,

    // 画面全体のデータ保存時のレイアウト保存
    getGraphDataSet: () => {
      if (!graphViewRef.current) throw new Error("GraphViewRef is not available")
      return {
        schemaDefinition: {
          nodes: nodes.reduce((acc, node) => {
            // メタ情報は内部制御にのみ使用しているため永続化対象外
            const { meta, ...rest } = node
            acc[node.id] = rest
            return acc
          }, {} as { [id: string]: GraphView2.Node }),
          edges,
          nodePositions: (graphViewRef.current?.getViewState() ?? { nodePositions: {}, defaultZoom: 1, defaultPan: { x: 0, y: 0 } }).nodePositions,
        },
      }
    },
  }))

  return (
    <div className={`relative ${props.className ?? ''}`}>
      <GraphView2.GraphView2
        ref={graphViewRef}
        nodes={nodes}
        edges={edges}
        defaultNodePositions={graphViewState?.schemaDefinition?.nodePositions}
        defaultPan={defaultPan}
        defaultZoom={defaultZoom}
        onSelectionChange={handleSelectionChange}
        onPanZoomChanged={handlePanZoomChanged}
        showGrid
        className="h-full w-full"
      />

      <div className="absolute top-1 left-1 select-none">
        {props.children}
      </div>
    </div>
  )
}


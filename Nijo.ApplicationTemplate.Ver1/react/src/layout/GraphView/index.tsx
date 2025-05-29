import React, { useImperativeHandle, forwardRef, useEffect, useState } from 'react';
import Navigator from './Cy.Navigator';
import { useCytoscape, CytoscapeHookType, LayoutSelectorComponentType, ViewState, CytoscapeDataSet } from './Cy';
import cytoscape from 'cytoscape';
import { LayoutLogicName } from './Cy.AutoLayout';
import { Node as CyNode, Edge as CyEdge } from './DataSource';

export * from "./DataSource"

export interface GraphViewRef extends Omit<CytoscapeHookType, 'cy' | 'containerRef' | 'applyToCytoscape' | 'hasNoElements' | 'expandAll' | 'collapseAll' | 'nodesLocked'> {
  getCy: () => cytoscape.Core | undefined;
  getNodesLocked: () => boolean;
  LayoutSelector: LayoutSelectorComponentType;
  resetLayout: () => void;
  applyViewState: (viewState: Partial<ViewState>) => void;
}

export interface GraphViewProps {
  handleKeyDown?: React.KeyboardEventHandler<HTMLDivElement>;
  nowLoading?: boolean;
  nodes?: CyNode[];
  edges?: CyEdge[];
  parentMap?: { [nodeId: string]: string };
  onReady?: () => void;
  /** ノードの自動整列に用いられるロジックのうちどれを使用するか。既定はklay */
  layoutLogic?: LayoutLogicName;
  onNodeDoubleClick?: (event: cytoscape.EventObject) => void;
  /** ノードのレイアウトが変更された瞬間に呼ばれる */
  onLayoutChange?: (event: cytoscape.EventObject) => void;
  /** ナビゲーターを表示するかどうか */
  showNavigator?: boolean;
}

/** 有向グラフを表示するコンポーネント。 */
export const GraphView = forwardRef<GraphViewRef, GraphViewProps>((props, ref) => {
  const {
    cy,
    containerRef,
    applyToCytoscape,
    reset,
    expandSelections,
    collapseSelections,
    toggleExpandCollapse,
    LayoutSelector,
    nodesLocked,
    toggleNodesLocked,
    hasNoElements,
    collectViewState,
    selectAll,
    resetLayout,
    applyViewState,
  } = useCytoscape(props);
  const [isReadyCalled, setIsReadyCalled] = useState(false);

  const graphViewRefObject = React.useMemo((): GraphViewRef => ({
    reset,
    expandSelections,
    collapseSelections,
    toggleExpandCollapse,
    LayoutSelector,
    getNodesLocked: () => nodesLocked,
    toggleNodesLocked,
    collectViewState,
    selectAll,
    getCy: () => cy,
    resetLayout: () => resetLayout(props.layoutLogic ?? 'klay'),
    applyViewState,
  }), [
    reset, expandSelections, collapseSelections, toggleExpandCollapse, LayoutSelector,
    nodesLocked, toggleNodesLocked, collectViewState, selectAll, cy, resetLayout, props.layoutLogic, applyViewState
  ]);

  useImperativeHandle(ref, () => graphViewRefObject);

  useEffect(() => {
    if (!cy) return;

    const newNodes = props.nodes ?? [];
    const newEdges = props.edges ?? [];
    const newParentMap = props.parentMap ?? {};

    cy.startBatch();
    try {
      const existingCyNodesMap = new Map(cy.nodes().map(n => [n.id(), n]));
      const newNodesMap = new Map(newNodes.map(n => [n.id, n]));

      // 不要になったノードを削除
      for (const [id, cyNode] of existingCyNodesMap) {
        if (!newNodesMap.has(id)) {
          cy.remove(cyNode);
        }
      }

      // ノードを追加または更新
      for (const newNode of newNodes) {
        const nodeDataForCy = {
          ...newNode,
          parent: newParentMap[newNode.id],
        };
        const existingNode = existingCyNodesMap.get(newNode.id);
        if (existingNode) {
          // 既存ノードのデータを更新
          const oldParent = existingNode.parent();
          const oldParentId = oldParent.length > 0 ? oldParent[0].id() : undefined;
          existingNode.data(nodeDataForCy);

          // 親が実際に変更された場合、move API を使って明示的に移動
          if (oldParentId !== nodeDataForCy.parent) {
            existingNode.move({ parent: nodeDataForCy.parent === undefined ? null : nodeDataForCy.parent });
          }
        } else {
          // 新しいノードを追加
          cy.add({ data: nodeDataForCy, group: 'nodes' });
        }
      }

      // エッジの差分更新
      const existingEdgesMap = new Map();
      cy.edges().forEach(edge => {
        const key = `${edge.source().id()}-${edge.target().id()}-${edge.data('label') ?? ''}`;
        existingEdgesMap.set(key, edge);
      });

      const newEdgesSet = new Set();
      for (const newEdge of newEdges) {
        const key = `${newEdge.source}-${newEdge.target}-${newEdge.label ?? ''}`;
        newEdgesSet.add(key);

        if (!existingEdgesMap.has(key)) {
          // 新しいエッジを追加
          // sourceとtargetノードが実際に存在する場合のみエッジを追加
          if (cy.getElementById(newEdge.source).length > 0 && cy.getElementById(newEdge.target).length > 0) {
            cy.add({ data: newEdge, group: 'edges' });
          }
        } else {
          // 既存エッジのデータを更新 (もしエッジのデータにlabel以外の変更がありうるなら)
          const existingEdge = existingEdgesMap.get(key);
          if (existingEdge.data('label') !== (newEdge.label ?? '')) { // 簡単のためlabelのみ比較
            existingEdge.data('label', newEdge.label ?? '');
          }
        }
      }

      // 不要になったエッジを削除
      existingEdgesMap.forEach((edge, key) => {
        if (!newEdgesSet.has(key)) {
          cy.remove(edge);
        }
      });

    } finally {
      cy.endBatch();
    }

    // レイアウトの再実行
    // if (cy.elements().length > 0) {
    //   // ViewStateが適用された場合は自動レイアウトを実行しない
    //   const viewStateApplied = cy.data('viewStateApplied');

    //   if (!viewStateApplied) {
    //     resetLayout(props.layoutLogic ?? 'klay');
    //   } else {
    //     // フラグをリセット（次回の更新のため）
    //     cy.removeData('viewStateApplied');
    //   }
    // }

    if (!isReadyCalled && cy.elements().length > 0) {
      props.onReady?.();
      setIsReadyCalled(true);
    }

  }, [cy, props.nodes, props.edges, props.parentMap, props.onReady, props.layoutLogic, resetLayout, isReadyCalled]);

  return (
    <>
      <div
        ref={containerRef}
        className="overflow-hidden [&>div>canvas]:left-0 h-full w-full outline-none"
        tabIndex={0}
        onKeyDown={props.handleKeyDown}
      ></div>
      {props.showNavigator && (
        <Navigator.Component
          hasNoElements={hasNoElements}
          className="absolute w-[20vw] h-[20vh] right-2 bottom-2 z-[200]"
        />
      )}
      {props.nowLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-500 bg-opacity-75 z-[300]">
          <p className="text-white text-2xl">読み込み中...</p>
        </div>
      )}
    </>
  );
});

import React, { useImperativeHandle, forwardRef, useEffect, useState } from 'react';
import Navigator from './Cy.Navigator';
import { useCytoscape, CytoscapeHookType, LayoutSelectorComponentType, ViewState, CytoscapeDataSet } from './Cy';
import cytoscape from 'cytoscape';
import { LayoutLogicName } from './Cy.AutoLayout';
import { Node as CyNode, Edge as CyEdge } from './DataSource';

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

      for (const [id, cyNode] of existingCyNodesMap) {
        if (!newNodesMap.has(id)) {
          cy.remove(cyNode);
        }
      }

      for (const newNode of newNodes) {
        const nodeDataForCy = {
          ...newNode,
          parent: newParentMap[newNode.id],
        };
        const existingNode = existingCyNodesMap.get(newNode.id);
        if (existingNode) {
          existingNode.data(nodeDataForCy);
        } else {
          cy.add({ data: nodeDataForCy, group: 'nodes' });
        }
      }

      cy.edges().remove();
      newEdges.forEach(edge => {
        if (cy.getElementById(edge.source).length > 0 && cy.getElementById(edge.target).length > 0) {
          cy.add({ data: edge, group: 'edges' });
        }
      });

    } finally {
      cy.endBatch();
    }

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

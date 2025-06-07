import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';

import * as Input from '../input';
import * as Layout from '../layout';
import { PerspectiveNode, PerspectivePageData } from './types';
import cytoscape from 'cytoscape'; // cytoscapeの型情報をインポート
import { ViewState } from '../layout/GraphView/Cy';
import ExpandCollapseFunctions from '../layout/GraphView/Cy.ExpandCollapse';
import { MentionUtil } from './MentionTextarea';

export const PerspectivePageGraph = ({
  formMethods,
  onNodeDoubleClick,
  className,
}: {
  formMethods: ReactHookForm.UseFormReturn<PerspectivePageData>
  onNodeDoubleClick: (nodeId: string) => void
  className: string
}) => {

  const watchedNodes = ReactHookForm.useWatch({ name: 'perspective.nodes', control: formMethods.control })
  const graphViewRef = React.useRef<Layout.GraphViewRef>(null)

  const graphNodes: Layout.Node[] | undefined = React.useMemo(() => {
    return watchedNodes.map((node: PerspectiveNode) => ({
      id: node.entityId,
      label: MentionUtil.toPlainText(node.entityName),
    } satisfies Layout.Node));
  }, [watchedNodes]);

  const parentMap: { [nodeId: string]: string } | undefined = React.useMemo(() => {
    const map: { [nodeId: string]: string } = {};
    watchedNodes.forEach((node, index, allNodes) => {
      // Find the closest preceding node with a smaller indent level
      let parentNodeId: string | undefined = undefined;
      for (let i = index - 1; i >= 0; i--) {
        if (allNodes[i].indent < node.indent) {
          parentNodeId = allNodes[i].entityId;
          break;
        }
      }
      if (parentNodeId) {
        map[node.entityId] = parentNodeId;
      }
    });
    return map;
  }, [watchedNodes]);

  const handleNodeClick = useEvent((event: cytoscape.EventObject) => {
    const nodeId = event.target.id();
    onNodeDoubleClick(nodeId);
  });

  const handleLayoutChange = useEvent((event: cytoscape.EventObject) => {
    // GraphViewRefを使用してViewStateを収集する
    const viewState = graphViewRef.current?.collectViewState();

    // 収集されたViewStateがnullまたは空のnodePositionsを持つ場合のみ、
    // 代替手段としてイベントのcyインスタンスから直接収集
    if (!viewState || Object.keys(viewState.nodePositions).length === 0) {
      const cy = event.cy;
      const nodePositions: ViewState['nodePositions'] = {};

      cy.nodes().forEach(node => {
        const pos = node.position();
        nodePositions[node.id()] = {
          x: Math.trunc(pos.x * 10000) / 10000,
          y: Math.trunc(pos.y * 10000) / 10000,
        };
      });

      const fallbackViewState: ViewState = {
        zoom: cy.zoom(),
        scrollPosition: cy.pan(),
        nodePositions,
        collapsedNodes: [],
      };

      // 折りたたみ状態の収集を試みる
      if (graphViewRef.current?.getCy()) {
        const cy = graphViewRef.current.getCy();
        if (cy) {
          fallbackViewState.collapsedNodes = ExpandCollapseFunctions(cy).toViewState();
        }
      }

      formMethods.setValue('perspective.viewState', fallbackViewState);
    } else {
      formMethods.setValue('perspective.viewState', viewState);
    }
  });

  const handleReadyGraph = useEvent(() => {
    const savedViewState = formMethods.getValues("perspective.viewState");

    if (savedViewState) {
      // レイアウト適用フラグを設定（あとでGraphView側でチェックする用）
      graphViewRef.current?.getCy()?.data('viewStateApplied', true);

      graphViewRef.current?.applyViewState(savedViewState);
    }
  });

  return (
    <div className={className}>
      <Layout.GraphView
        ref={graphViewRef}
        nodes={graphNodes}
        parentMap={parentMap}
        edges={undefined} // エッジ編集の仕組みがないので保留
        onNodeDoubleClick={handleNodeClick}
        onLayoutChange={handleLayoutChange}
        onReady={handleReadyGraph}
      />
    </div>
  );
}

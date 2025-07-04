import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { applyFormatCondition, PerspectiveNode, PerspectivePageData } from './types';
import cytoscape from 'cytoscape'; // cytoscapeの型情報をインポート
import { ViewState } from '../../layout/GraphView/Cy';
import ExpandCollapseFunctions from '../../layout/GraphView/Cy.ExpandCollapse';
import { MentionUtil } from '../UI';

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

  // ノード、エッジ
  const [graphNodes, graphEdges] = React.useMemo((): [
    graphNodes: Layout.Node[],
    graphEdges: Layout.Edge[],
  ] => {
    const attrDefs = formMethods.getValues('perspective.attributes')
    const formatConditions = formMethods.getValues('perspective.formatConditions')

    // ノード
    const nodes: Map<string, Layout.Node> = new Map()
    for (const node of watchedNodes) {
      // 条件付き書式の適用
      const formatCondition = applyFormatCondition(node, formatConditions);
      if (formatCondition.invisibleInGraph) continue;

      nodes.set(node.entityId, {
        id: node.entityId,
        label: MentionUtil.toPlainText(node.entityName),
        color: formatCondition.graphNodeColor,
        'background-color': formatCondition.graphNodeColor,
        'border-color': formatCondition.graphNodeColor,
      } satisfies Layout.Node)
    }

    // エッジ
    const edges: Layout.Edge[] = []
    const mentions = MentionUtil.collectMentionIds(watchedNodes, attrDefs)

    for (const [sourceId, targetMap] of Object.entries(mentions)) {
      for (const [targetId, mention] of Object.entries(targetMap)) {
        edges.push({
          source: sourceId,
          target: targetId,
          label: Array.from(mention.relations).join(','),
        } satisfies Layout.Edge)

        // 参照先が他のページのエンティティの場合、
        // ここでグラフに参照先のノードを加える必要がある
        if (!mention.targetIsInThisPerspective && !nodes.has(targetId)) {
          nodes.set(targetId, {
            id: targetId,
            label: Array.from(mention.mentionTexts).join(','),
            "border-color": "white",
            "background-color": "white",
          } satisfies Layout.Node)
        }
      }
    }
    return [Array.from(nodes.values()), edges]
  }, [watchedNodes]);

  // 親子関係
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
        edges={graphEdges}
        onNodeDoubleClick={handleNodeClick}
        onLayoutChange={handleLayoutChange}
        onReady={handleReadyGraph}
      />
    </div>
  );
}

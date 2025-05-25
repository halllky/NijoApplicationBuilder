import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { NIJOUI_CLIENT_ROUTE_PARAMS } from '../routing';
import { Perspective, PerspectiveNode, PerspectivePageData } from './types';
import { NijoUiOutletContextType } from '../types';
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels';
import cytoscape from 'cytoscape'; // cytoscapeの型情報をインポート
import { ViewState } from '../../layout/GraphView/Cy';

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
      id: node.nodeId,
      label: node.label ?? '',
    } satisfies Layout.Node));
  }, [watchedNodes]);

  const parentMap: { [nodeId: string]: string } | undefined = React.useMemo(() => {
    const map: { [nodeId: string]: string } = {};
    watchedNodes.forEach(node => {
      if (node.parentId) {
        map[node.nodeId] = node.parentId;
      }
    });
    return map;
  }, [watchedNodes]);

  const handleNodeClick = useEvent((event: cytoscape.EventObject) => {
    const nodeId = event.target.id();
    onNodeDoubleClick(nodeId);
  });

  const handleLayoutChange = useEvent((event: cytoscape.EventObject) => {
    const cy = event.cy
    const nodePositions: ViewState['nodePositions'] = {}
    cy.nodes().forEach(node => {
      const pos = node.position()
      nodePositions[node.id()] = { x: pos.x, y: pos.y }
    })
    const viewState: ViewState = {
      zoom: cy.zoom(),
      scrollPosition: cy.pan(),
      nodePositions,
      collapsedNodes: [],
    }
    formMethods.setValue('perspective.viewState', viewState)
  })

  const handleReadyGraph = useEvent(() => {
    const savedViewState = formMethods.getValues("perspective.viewState")
    if (savedViewState) {
      graphViewRef.current?.applyViewState(savedViewState);
    }
  })

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

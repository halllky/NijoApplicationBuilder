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

  const graphNodes: Layout.Node[] | undefined = React.useMemo(() => {
    return watchedNodes.map((node: PerspectiveNode) => ({
      id: node.nodeId,
      label: node.label ?? '',
    }));
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

  return (
    <div className={className}>
      <Layout.GraphView
        nodes={graphNodes}
        parentMap={parentMap}
        edges={undefined} // エッジ編集の仕組みがないので保留
        onNodeDoubleClick={handleNodeClick}
      />
    </div>
  );
}

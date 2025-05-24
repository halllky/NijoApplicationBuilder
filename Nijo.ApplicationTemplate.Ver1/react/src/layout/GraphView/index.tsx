import React, { useImperativeHandle, forwardRef } from 'react';
import Navigator from './Cy.Navigator';
import { useCytoscape, CytoscapeHookType, LayoutSelectorComponentType, ViewState, CytoscapeDataSet } from './Cy';
import cytoscape from 'cytoscape';

export interface GraphViewRef extends Omit<CytoscapeHookType, 'cy' | 'containerRef' | 'applyToCytoscape' | 'hasNoElements' | 'expandAll' | 'collapseAll' | 'nodesLocked'> {
  applyToCytoscape: (dataSet: CytoscapeDataSet, viewState?: ViewState) => void;
  getCy: () => cytoscape.Core | undefined;
  getNodesLocked: () => boolean;
  LayoutSelector: LayoutSelectorComponentType;
  applyLayout: (layoutName: string) => void;
}

interface GraphViewProps {
  handleKeyDown?: React.KeyboardEventHandler<HTMLDivElement>;
  nowLoading?: boolean;
  initialDataSet?: CytoscapeDataSet;
  initialViewState?: ViewState;
}

const GraphView = forwardRef<GraphViewRef, GraphViewProps>((
  { handleKeyDown, nowLoading, initialDataSet, initialViewState },
  ref
) => {
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
    applyLayout,
  } = useCytoscape();

  useImperativeHandle(ref, () => ({
    reset,
    expandSelections,
    collapseSelections,
    toggleExpandCollapse,
    LayoutSelector,
    getNodesLocked: () => nodesLocked,
    toggleNodesLocked,
    collectViewState,
    selectAll,
    applyToCytoscape: (dataSet: CytoscapeDataSet, viewState) => applyToCytoscape(dataSet, viewState),
    getCy: () => cy,
    applyLayout: (layoutName: string) => applyLayout(layoutName),
  }));

  React.useEffect(() => {
    if (initialDataSet) {
      applyToCytoscape(initialDataSet, initialViewState);
    }
  }, [initialDataSet, initialViewState, applyToCytoscape]);

  return (
    <>
      <div
        ref={containerRef}
        className="overflow-hidden [&>div>canvas]:left-0 h-full w-full outline-none"
        tabIndex={0}
        onKeyDown={handleKeyDown}
      ></div>
      <Navigator.Component
        hasNoElements={hasNoElements}
        className="absolute w-[20vw] h-[20vh] right-2 bottom-2 z-[200]"
      />
      {nowLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-500 bg-opacity-75 z-[300]">
          <p className="text-white text-2xl">読み込み中...</p>
        </div>
      )}
    </>
  );
});

export default GraphView;

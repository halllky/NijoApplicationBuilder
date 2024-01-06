import { useCallback } from 'react'
import cytoscape from 'cytoscape'
// @ts-ignore
import cytospaceExpandCollapse from 'cytoscape-expand-collapse'
import { Query } from './GraphView.Query'

const configure = (cy: typeof cytoscape) => {
  cytospaceExpandCollapse(cy)
}

const setupCyInstance = (cy: cytoscape.Core) => {
  (cy as any).expandCollapse({
    layoutBy: null, // to rearrange after expand/collapse. It's just layout options or whole layout function. Choose your side!
    // recommended usage: use cose-bilkent layout with randomize: false to preserve mental map upon expand/collapse
    fisheye: false, // whether to perform fisheye view after expand/collapse you can specify a function too
    animate: false, // whether to animate on drawing changes you can specify a function too
    animationDuration: 1000, // when animate is true, the duration in milliseconds of the animation
    ready: function () { },  // callback when expand/collapse initialized
    undoable: false, // and if undoRedoExtension exists,

    cueEnabled: true, // Whether cues are enabled
    expandCollapseCuePosition: 'top-left', // default cue position is top left you can specify a function per node too
    expandCollapseCueSize: 12, // size of expand-collapse cue
    expandCollapseCueLineSize: 8, // size of lines used for drawing plus-minus icons
    expandCueImage: undefined, // image of expand icon if undefined draw regular expand cue
    collapseCueImage: undefined, // image of collapse icon if undefined draw regular collapse cue
    expandCollapseCueSensitivity: 1, // sensitivity of expand-collapse cues
    edgeTypeInfo: "edgeType", // the name of the field that has the edge type, retrieved from edge.data(), can be a function, if reading the field returns undefined the collapsed edge type will be "unknown"
    groupEdgesOfSameTypeOnCollapse: false, // if true, the edges to be collapsed will be grouped according to their types, and the created collapsed edges will have same type as their group. if false the collapased edge will have "unknown" type.
    allowNestedEdgeCollapse: true, // when you want to collapse a compound edge (edge which contains other edges) and normal edge, should it collapse without expanding the compound first
    zIndex: 100,// z-index value of the canvas in which cue ımages are drawn
  })
}

const useExpandCollapse = (cy: cytoscape.Core | undefined) => {
  const expandAll = useCallback(() => {
    const api = (cy as any)?.expandCollapse('get')
    api.expandAll()
    api.expandAllEdges()
  }, [cy])
  const collapseAll = useCallback(() => {
    const api = (cy as any)?.expandCollapse('get')
    api.collapseAll()
    api.collapseAllEdges()
  }, [cy])

  return {
    expandAll,
    collapseAll,
  }
}

const getViewState = (beforeState: Query, cy: cytoscape.Core): Query => {
  const api = (cy as any)?.expandCollapse('get')
  const collapsedNodes = api
    .getAllCollapsedChildrenRecursively()
    .map((node: cytoscape.NodeSingular) => node.id())
  return { ...beforeState, collapsedNodes }
}
const restoreViewState = (viewState: Query, cy: cytoscape.Core) => {
  // const api = (cy as any)?.expandCollapse('get')
  // for (const nodeId of viewState.collapsedNodes) {
  //   const node = cy.getElementById(nodeId)
  //   if (node) api.collapse(node[0])
  // }
}

export default {
  configure,
  setupCyInstance,
  useExpandCollapse,
  getViewState,
  restoreViewState,
}

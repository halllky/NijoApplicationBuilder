import { useReducer } from 'react'
import { ReactHookUtil } from './util'

export type ViewState = {
  name: string
  nodePositions: { [nodeId: string]: cytoscape.Position }
  collapsedNodes: string[]
}

export const createNewQuery = (): ViewState => ({
  name: '',
  nodePositions: {},
  collapsedNodes: [],
})

const viewStateReducer = ReactHookUtil.defineReducer((state: ViewState) => ({
  rename: (name: string) => ({ ...state, name }),
  clear: () => ({ ...state, nodePositions: {}, collapsedNodes: [] }),
}))
export type ViewStateDispatcher = ReactHookUtil.DispatcherOf<typeof viewStateReducer>

export const useViewState = () => {
  const [viewState, dispatchViewState] = useReducer(viewStateReducer, createNewQuery())
  return [viewState, dispatchViewState] as const
}

const getViewState = (beforeState: ViewState, cy: cytoscape.Core): ViewState => {
  return {
    ...beforeState,
    nodePositions: cy.nodes().reduce((map, node) => {
      const pos = node.position()
      map[node.id()] = {
        x: Math.trunc(pos.x * 10000) / 10000,
        y: Math.trunc(pos.y * 10000) / 10000,
      }
      return map
    }, { ...beforeState.nodePositions }),
  }
}
const restoreViewState = (viewState: ViewState, cy: cytoscape.Core) => {
  for (const node of cy.nodes()) {

    // 子要素をもつノードの位置は子要素の位置が決まると自動的に決まるのであえて設定しない
    if (node.isParent()) continue

    const pos = viewState.nodePositions[node.id()]
    if (pos) node.position(pos)
  }
}

export default {
  getViewState,
  restoreViewState,
  useViewState,
}

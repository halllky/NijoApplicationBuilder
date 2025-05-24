import { useCallback } from 'react'
import cytoscape from 'cytoscape'
import ExpandCollapseFunctions from './Cy.ExpandCollapse'

export type ViewState = {
  nodePositions: { [nodeId: string]: cytoscape.Position }
  collapsedNodes: string[]
}

export const getEmptyViewState = (): ViewState => ({
  nodePositions: {},
  collapsedNodes: [],
})

export const useViewState = (cy: cytoscape.Core | undefined) => {

  const collectViewState = useCallback((): ViewState => {
    const viewState = getEmptyViewState()
    if (!cy) return viewState
    for (const node of cy.nodes()) {
      const pos = node.position()
      viewState.nodePositions[node.id()] = {
        x: Math.trunc(pos.x * 10000) / 10000,
        y: Math.trunc(pos.y * 10000) / 10000,
      }
    }

    // 折りたたみ状態の保存
    viewState.collapsedNodes = ExpandCollapseFunctions(cy).toViewState()

    return viewState
  }, [cy])

  const applyViewState = useCallback((viewState: ViewState) => {
    if (!cy) return
    for (const node of cy.nodes()) {
      // 子要素をもつノードの位置は子要素の位置が決まると自動的に決まるのであえて設定しない
      if (node.isParent()) continue

      const pos = viewState.nodePositions[node.id()]
      if (pos) node.position(pos)
    }

    // 折りたたみ状態の復元
    ExpandCollapseFunctions(cy).applyViewState(viewState)

  }, [cy])

  return {
    collectViewState,
    applyViewState,
  }
}

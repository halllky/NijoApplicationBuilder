import { useCallback } from 'react'
import cytoscape from 'cytoscape'
import ExpandCollapseFunctions from './Cy.ExpandCollapse'

export type ViewState = {
  nodePositions: { [nodeId: string]: cytoscape.Position }
  collapsedNodes: string[]
  zoom: number
  scrollPosition: cytoscape.Position
}

export const getEmptyViewState = (): ViewState => ({
  nodePositions: {},
  collapsedNodes: [],
  zoom: 1,
  scrollPosition: { x: 0, y: 0 },
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

  const applyViewState = useCallback((viewState: Partial<ViewState>) => {
    if (!cy) return

    // ノード位置の復元
    for (const node of cy.nodes()) {
      // 子要素をもつノードの位置は子要素の位置が決まると自動的に決まるのであえて設定しない
      if (node.isParent()) continue

      const pos = viewState.nodePositions?.[node.id()]
      if (pos) node.position(pos)
    }

    // 折りたたみ状態の復元
    if (viewState.collapsedNodes) {
      ExpandCollapseFunctions(cy).applyViewState(viewState.collapsedNodes)
    }

    // 拡大率の復元
    if (viewState.zoom) cy.zoom(viewState.zoom)

    // スクロール位置の復元
    if (viewState.scrollPosition) cy.pan(viewState.scrollPosition)

  }, [cy])

  return {
    collectViewState,
    applyViewState,
  }
}

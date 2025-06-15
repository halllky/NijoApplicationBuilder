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

    // ズームとパン情報を追加
    viewState.zoom = cy.zoom()
    viewState.scrollPosition = cy.pan()

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

    // 折りたたみ状態の復元
    if (viewState.collapsedNodes) {
      ExpandCollapseFunctions(cy).applyViewState(viewState.collapsedNodes)
    }

    // ノード位置の復元をpresetレイアウトで行う
    // presetレイアウトはノード位置を指定するとその位置に固定されるので、
    // ノード位置を指定している場合はpresetレイアウトを使用する
    if (viewState.nodePositions && Object.keys(viewState.nodePositions).length > 0) {
      const layoutOptions = {
        name: 'preset',
        positions: viewState.nodePositions,
        fit: false,
        animate: false,
      } satisfies cytoscape.PresetLayoutOptions

      cy.layout(layoutOptions).run()
    }

    // 拡大率の復元
    if (viewState.zoom) cy.zoom(viewState.zoom)

    // スクロール位置の復元
    if (viewState.scrollPosition) cy.pan(viewState.scrollPosition)

    // ViewStateが適用されたことを示すフラグを設定
    cy.data('viewStateApplied', true)

  }, [cy])

  return {
    collectViewState,
    applyViewState,
  }
}

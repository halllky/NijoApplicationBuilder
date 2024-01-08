import { useCallback, useState } from 'react'
import cytoscape from 'cytoscape'
import * as UUID from 'uuid'
import Navigator from './Cy.Navigator'
import AutoLayout from './Cy.AutoLayout'
import ExpandCollapse from './Cy.ExpandCollapse'
import VS, { ViewState, ViewStateDispatcher } from './Graph.ViewState'
import { DataSet } from './Graph.DataSource'
import { Messaging, ReactHookUtil } from './util'

AutoLayout.configure(cytoscape)
Navigator.configure(cytoscape)
ExpandCollapse.configure(cytoscape)

export const useCytoscape = (
  reloadDataSet: () => Promise<DataSet>,
  viewState: ViewState,
  dispatchViewState: ViewStateDispatcher
) => {
  const [, dispatchMsg] = Messaging.useMsgContext()

  const [cy, setCy] = useState<cytoscape.Core>()
  const [navInstance, setNavInstance] = useState<{ destroy: () => void }>()

  const containerRef = useCallback((divElement: HTMLElement | null) => {
    if (!cy && divElement) {
      // 初期化
      const cyInstance = cytoscape({
        container: divElement,
        elements: [],
        style: STYLESHEET,
        layout: AutoLayout.DEFAULT,
      })
      setNavInstance(Navigator.setupCyInstance(cyInstance))
      ExpandCollapse.setupCyInstance(cyInstance)
      setCy(cyInstance)

    } else if (cy && !divElement) {
      // 破棄
      navInstance?.destroy()
    }
  }, [cy, navInstance])

  const { autoLayout, LayoutSelector } = AutoLayout.useAutoLayout(cy)
  const { expandAll, collapseAll } = ExpandCollapse.useExpandCollapse(cy)

  // ノード位置固定
  const [nodesLocked, changeNodesLocked] = ReactHookUtil.useToggle()
  const toggleNodesLocked = useCallback(() => {
    if (!cy) { changeNodesLocked(x => x.setValue(false)); return }
    changeNodesLocked(x => x.toggle())
    cy.autolock(!nodesLocked)
  }, [cy, nodesLocked])

  const reset = useCallback(() => {
    autoLayout()
    dispatchViewState(state => state.clear())
  }, [autoLayout])

  const [nowLoading, setNowLoading] = useState(false)
  const reload = useCallback(async () => {
    if (!cy) return
    try {
      setNowLoading(true)

      const dataSet = await reloadDataSet()

      cy.startBatch()

      // データ洗い替え前のノード位置などを退避させておく
      const viewStateBeforeQuery1 = VS.getViewState(viewState, cy)
      const viewStateBeforeQuery = ExpandCollapse.getViewState(viewStateBeforeQuery1, cy)

      cy.elements().remove()

      const nodeIds = new Set(Object.keys(dataSet.nodes))

      // 結果セット中に存在しないノードは仮ノードを作成して表示する
      const ensureNodeExists = (id: string) => {
        if (nodeIds.has(id)) return
        nodeIds.add(id)
        const label = id
        cy.add({ data: { id, label } })
      }

      // ノード
      for (const [id, node] of Object.entries(dataSet.nodes)) {
        if (node.parent) ensureNodeExists(node.parent)

        const label = node.label
        const parent = node.parent
        cy.add({ data: { id, label, parent } })
      }

      // エッジ
      for (const { source, target, label } of dataSet.edges) {
        ensureNodeExists(source)
        ensureNodeExists(target)

        const id = UUID.v4()
        cy.add({ data: { id, source, target, label } })
      }

      // ノード位置などViewStateの復元
      VS.restoreViewState(viewStateBeforeQuery, cy)
      ExpandCollapse.restoreViewState(viewStateBeforeQuery, cy)

      cy.endBatch()

    } catch (error) {
      dispatchMsg(msg => msg.push('error', error))

    } finally {
      setNowLoading(false)
    }
  }, [cy, reloadDataSet])

  return {
    containerRef,
    reload,
    reset,
    expandAll,
    collapseAll,
    LayoutSelector,
    nodesLocked,
    toggleNodesLocked,
    nowProcessing: nowLoading,
  }
}

const STYLESHEET: cytoscape.CytoscapeOptions['style'] = [{
  selector: 'node',
  css: {
    'shape': 'round-rectangle',
    'width': (node: any) => node.data('label')?.length * 10,
    'text-valign': 'center',
    'text-halign': 'center',
    'border-width': '1px',
    'border-color': '#909090',
    'background-color': '#666666',
    'background-opacity': .1,
    'label': 'data(label)',
  },
}, {
  selector: 'node:parent', // 子要素をもつノードに適用される
  css: {
    'text-valign': 'top',
    'color': '#707070',
  },
}, {
  selector: 'edge',
  style: {
    'target-arrow-shape': 'triangle',
    'curve-style': 'bezier',
  },
}, {
  selector: 'edge:selected',
  style: {
    'label': 'data(label)',
    'color': 'blue',
  },
}]

import React, { useCallback, useMemo, useState } from 'react'
import cytoscape from 'cytoscape'
import { UUID } from 'uuidjs'
import Navigator from './Cy.Navigator'
import AutoLayout from './Cy.AutoLayout'
import ExpandCollapseFunctions from './Cy.ExpandCollapse'
import { ViewState, useViewState } from './Cy.SaveLoad'
import * as DS from './DataSource'
import { USER_SETTING } from './UserSetting'

AutoLayout.configure(cytoscape)
Navigator.configure(cytoscape)

// DS.DataSet をエクスポート
export type { DataSet as CytoscapeDataSet } from './DataSource'

// ViewState を再エクスポート
export type { ViewState } from './Cy.SaveLoad'

// LayoutSelector の型を定義してエクスポート
export type LayoutSelectorComponentType = React.FC; // AutoLayout.useAutoLayout の戻り値から推測

// useCytoscape の戻り値の型を定義してエクスポート
export interface CytoscapeHookType {
  cy: cytoscape.Core | undefined;
  containerRef: (divElement: HTMLElement | null) => void;
  applyToCytoscape: (dataSet: DS.DataSet, viewState?: ViewState) => Promise<void>;
  selectAll: () => void;
  reset: () => void;
  expandSelections: () => void;
  collapseSelections: () => void;
  toggleExpandCollapse: () => void;
  LayoutSelector: LayoutSelectorComponentType;
  nodesLocked: boolean;
  toggleNodesLocked: () => void;
  hasNoElements: boolean;
  collectViewState: () => ViewState;
  applyLayout: (layoutName: string) => void;
}

export const useCytoscape = (): CytoscapeHookType => {
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
        wheelSensitivity: USER_SETTING.wheelSensitivity.value,
      })
      setNavInstance(Navigator.setupCyInstance(cyInstance))

      // Cytoscapeは標準でcanvas要素のmousedownでフォーカスを外す処理をしており
      // これによりcanvasを操作しているときにキーボードショートカットが使えないため
      // 無理やり再フォーカスさせてkeydownがトリガーされるようにする。
      cyInstance.on('mousedown', () => {
        divElement.focus()
      })

      setCy(cyInstance)

    } else if (cy && !divElement) {
      // 破棄
      navInstance?.destroy()
    }
  }, [cy, navInstance])

  const { autoLayout, LayoutSelector } = AutoLayout.useAutoLayout(cy)
  const { actions: expandCollapseActions } = useMemo(() => {
    return ExpandCollapseFunctions(cy)
  }, [cy])

  const selectAll = useCallback(() => {
    cy?.nodes().select()
  }, [cy])

  // ノード位置固定
  const [nodesLocked, changeNodesLocked] = React.useState(false)
  const toggleNodesLocked = useCallback(() => {
    if (!cy) { changeNodesLocked(false); return }
    changeNodesLocked(checked => !checked)
    cy.autolock(!nodesLocked)
  }, [cy, nodesLocked])

  const reset = useCallback(() => {
    autoLayout()
  }, [autoLayout])

  const { collectViewState, applyViewState } = useViewState(cy)

  const applyToCytoscape = useCallback(async (dataSet: DS.DataSet, viewState?: ViewState) => {
    if (!cy) return
    try {
      cy.startBatch()
      const viewStateBeforeQuery = collectViewState()
      cy.elements().remove()
      const nodeIds = new Set(Object.keys(dataSet.nodes))
      const ensureNodeExists = (id: string) => {
        if (nodeIds.has(id)) return
        nodeIds.add(id)
        const label = id
        cy.add({ data: { id, label } })
      }
      const nodesWithDepth = Object.entries(dataSet.nodes).reduce((arr, [id, node]) => {
        let depth = 0
        let parentId = node.parent
        while (parentId) {
          const parent = dataSet.nodes[parentId]
          if (!parent) break
          depth++
          parentId = parent.parent
        }
        arr.push({ id, node, depth })
        return arr
      }, [] as { id: string, node: DS.Node, depth: number }[])
      nodesWithDepth.sort((a, b) => {
        if (a.depth < b.depth) return -1
        if (a.depth > b.depth) return 1
        return 0
      })
      for (const { id, node } of nodesWithDepth) {
        if (node.parent) ensureNodeExists(node.parent)
        cy.add({ data: { id, ...node } })
      }
      for (const edge of dataSet.edges) {
        ensureNodeExists(edge.source)
        ensureNodeExists(edge.target)
        const id = UUID.generate()
        cy.add({ data: { id, ...edge } })
      }
      // ノード位置などViewStateの復元
      if (viewState) {
        applyViewState(viewState)
      }
      applyViewState(viewStateBeforeQuery)
    } finally {
      cy.endBatch()
    }
  }, [cy, collectViewState, applyViewState])

  const applyLayout = useCallback((layoutName: string) => {
    if (!cy) return;
    const layoutOption = AutoLayout.OPTION_LIST[layoutName];
    if (layoutOption) {
      cy.layout(layoutOption).run();
    }
  }, [cy]);

  return {
    cy,
    containerRef,
    applyToCytoscape,
    selectAll,
    reset,
    ...expandCollapseActions,
    LayoutSelector,
    nodesLocked,
    toggleNodesLocked,
    hasNoElements: (cy?.elements().length ?? 0) === 0,
    collectViewState,
    applyLayout,
  }
}

const STYLESHEET: cytoscape.CytoscapeOptions['style'] = [{
  selector: 'node',
  css: {
    'shape': 'rectangle',
    'width': (node: any) => node.data('label')?.length * 20,
    'text-valign': 'center',
    'text-halign': 'center',
    'color': (node: any) => node.data('color') ?? '#000000',
    'border-width': '1px',
    'border-color': node => node.data('border-color') ?? '#909090',
    'background-color': node => node.data('background-color') ?? '#666666',
    'background-opacity': .1,
    'label': 'data(label)',
  },
}, {
  selector: 'node:selected',
  style: {
    'border-style': 'dashed',
    'border-width': '1px',
    'border-color': node => node.data('border-color:selected') ?? '#FF4F02',
  },
}, {
  selector: 'node:parent', // 子要素をもつノードに適用される
  css: {
    'text-valign': 'top',
    'color': (node: any) => node.data('color:container') ?? '#707070',
  },
}, {
  selector: 'edge',
  style: {
    'label': 'data(label)',
    'color': '#707070',
    'line-color': edge => edge.data('line-color') ?? '#707070',
    'target-arrow-color': edge => edge.data('line-color') ?? '#707070',
    'line-opacity': .5,
    'font-size': '10px',
    'target-arrow-shape': 'triangle',
    'curve-style': 'bezier',
    'width': '1px',
  },
}, {
  selector: 'edge:selected',
  style: {
    'label': 'data(label)',
    'color': '#FF4F02',
    'line-color': '#FF4F02',
    'line-style': 'dashed',
    'source-arrow-color': '#FF4F02',
    'target-arrow-color': '#FF4F02',
    'width': '2px',
  },
}]

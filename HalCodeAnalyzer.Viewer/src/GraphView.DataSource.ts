import { useEffect } from 'react'
import * as UUID from 'uuid'
import ViewState, { Query } from './GraphView.Query'
import ExpandCollapse from './GraphView.ExpandCollapse'

namespace GraphDataSource {

  export type DataSet = {
    nodes: { [id: string]: Node }
    edges: Edge[]
  }
  export type Node = {
    label: string
    parent?: string
  }
  export type Edge = {
    source: string
    target: string
    label?: string
  }

  export const createEmptyDataSet = (): DataSet => ({
    nodes: {},
    edges: [],
  })

  export const useDataSource = (
    cy: cytoscape.Core | undefined,
    dataSet: DataSet,
    viewState: Query
  ) => {

    useEffect(() => {
      if (!cy) return
      cy.startBatch()

      const viewStateBeforeQuery1 = ViewState.getViewState(viewState, cy)
      const viewStateBeforeQuery = ExpandCollapse.getViewState(viewStateBeforeQuery1, cy)

      cy.elements().remove()

      // ノード
      const nodeIds = new Set(Object.keys(dataSet.nodes))
      for (const [id, node] of Object.entries(dataSet.nodes)) {
        const label = node.label
        const parent = node.parent && nodeIds.has(node.parent)
          ? node.parent
          : undefined // 親が結果セット中に存在しないノードは親なしとして表示

        const element: cytoscape.ElementDefinition = { data: { id, label, parent } }
        cy.add(element)
      }

      // エッジ
      for (const { source, target, label } of dataSet.edges) {
        const id = UUID.v4()

        // 両端のうちいずれかが存在しないエッジはスキップ
        if (!nodeIds.has(source) || !nodeIds.has(target)) continue

        const element: cytoscape.ElementDefinition = { data: { id, source, target, label } }
        cy.add(element)
      }

      // ノード位置などViewStateの復元
      ViewState.restoreViewState(viewStateBeforeQuery, cy)
      ExpandCollapse.restoreViewState(viewStateBeforeQuery, cy)

      cy.endBatch()
    }, [cy, dataSet])
  }
}

export default GraphDataSource

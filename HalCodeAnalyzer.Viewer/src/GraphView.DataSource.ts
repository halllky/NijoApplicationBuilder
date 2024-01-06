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

      // データ洗い替え前のノード位置などを退避させておく
      const viewStateBeforeQuery1 = ViewState.getViewState(viewState, cy)
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
      ViewState.restoreViewState(viewStateBeforeQuery, cy)
      ExpandCollapse.restoreViewState(viewStateBeforeQuery, cy)

      cy.endBatch()
    }, [cy, dataSet])
  }
}

export default GraphDataSource

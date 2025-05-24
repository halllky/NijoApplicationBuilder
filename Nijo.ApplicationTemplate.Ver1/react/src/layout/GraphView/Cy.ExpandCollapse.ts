import cytoscape from 'cytoscape'
import { UUID } from 'uuidjs'
import { ViewState } from './Cy.SaveLoad'


const collapse = (cy: cytoscape.Core | undefined, nodes: cytoscape.NodeCollection) => {
  if (!cy) return
  cy.batch(() => {
    // 子ノードをもたないものはたためないのでスキップ
    const nodesToBeCollapsed = nodes.filter(n => n.isParent())

    // グラフ中に存在しないサマリーエッジをaddする
    nodesToBeCollapsed.data(IS_COLLAPSED, true)
    const virtualEdges = nodesToBeCollapsed
      .descendants()
      .connectedEdges()
      .map(edge => getVirtualEdge(edge, cy))
    for (const edge of virtualEdges) {
      if (!edge) continue
      if (cy.hasElementWithId(edge.id())) continue
      cy.add(edge)
    }

    // 各collapasedNodesの子孫を非表示にする。
    // CytosccontainerNodesape.jsでは両端のうちどちらかのノードにdisplay:noneが付与されているエッジは非表示になる。
    nodesToBeCollapsed.descendants().style('display', 'none')
  })
}


const expand = (cy: cytoscape.Core | undefined, nodes: cytoscape.NodeCollection) => {
  if (!cy) return
  cy.batch(() => {
    const descendantsToBeShown: cytoscape.NodeSingular[] = []
    const descendantsStillHidden: cytoscape.NodeSingular[] = []
    const summaryEdgesToBeRemoved: cytoscape.EdgeSingular[] = []

    for (const node of nodes) {
      // ひらく必要が無いノードはスキップ
      if (node.data(IS_COLLAPSED) == undefined) continue

      // ひらかれるノードの操作
      node.removeData(IS_COLLAPSED)
      summaryEdgesToBeRemoved.push(...node.connectedEdges(`[${SUMMARISE}]`))

      // ひらかれるノードの子孫ノードの操作
      for (const descendant of node.descendants()) {
        if (descendant.ancestors(`[${IS_COLLAPSED}]`).length === 0) {
          descendantsToBeShown.push(descendant)
        } else {
          descendantsStillHidden.push(descendant)
        }
      }
    }

    // 新たに表示されるノードに接続するサマリーエッジ
    const virtualEdges = cy
      .collection(descendantsToBeShown)
      .union(descendantsStillHidden)
      .connectedEdges()
      .map(edge => getVirtualEdge(edge, cy))

    // 更新
    cy.remove(cy.collection(summaryEdgesToBeRemoved))
    cy.collection(descendantsToBeShown).style('display', '')
    for (const edge of virtualEdges) {
      if (!edge) continue
      if (cy.hasElementWithId(edge.id())) continue
      cy.add(edge)
    }
  })
}

/**
 * 実際に表示に使用されるエッジを返す。
 * 折りたたまれた結果sourceもtargetも同じノードになる場合はundefined
 */
const getVirtualEdge = (edge: cytoscape.EdgeSingular, cy: cytoscape.Core): cytoscape.EdgeSingular | undefined => {

  // 実際に表示されるノードがどれかを調べる
  const virtualSource = getVisibleAncestor(edge.source()).id()
  const virtualTarget = getVisibleAncestor(edge.target()).id()

  // 両端がどちらも可視であれば引数のエッジがそのまま表示される
  if (virtualSource === edge.source().id()
    && virtualTarget === edge.target().id()) {
    return edge
  }

  const virtualEdge = cy.edges(`[source = "${virtualSource}"][target = "${virtualTarget}"]`)

  if (virtualEdge.length === 0) {
    // 折りたたまれた結果sourceもtargetも同じノードになる場合
    if (virtualSource === virtualTarget) return undefined

    // サマリーエッジの作成
    const data: cytoscape.EdgeDataDefinition = {
      id: UUID.generate(),
      source: virtualSource,
      target: virtualTarget,
      [SUMMARISE]: [edge.id()],
    }
    const newVirtualEdge = cy.add({ data })
    return newVirtualEdge

  } else if (virtualEdge.length === 1) {
    // 既存のサマリーエッジにこのエッジを追加
    const summarisedEdgeIdList = new Set<string>([edge.id()])
    virtualEdge[0].data(SUMMARISE, summarisedEdgeIdList)
    return virtualEdge[0]

  } else {
    throw new Error('virtual edges should be unique.')
  }
}


/** 引数のノードの祖先のうち可視のものを返す */
const getVisibleAncestor = (node: cytoscape.NodeSingular) => {
  // 祖先をルートから順に辿って最初にCOLLAPSED属性がついているのが表示されるノード
  const ancestors: cytoscape.NodeSingular[] = []
  let parent: cytoscape.NodeSingular | undefined = node.parent()[0]
  while (parent !== undefined) {
    ancestors.push(parent)
    parent = parent.parent()[0]
  }
  ancestors.reverse()
  for (const ancestor of ancestors) {
    if (ancestor.data(IS_COLLAPSED)) return ancestor
  }
  // 祖先がいずれも折りたたみ状態でなければ引数のノード自身が表示される
  return node
}


/**
 * 折りたたまれて見えなくなったノードではなく
 * それらの見えなくなったノードの親のノードにつく属性
 */
const IS_COLLAPSED = 'COLLAPSED'

/**
 * サマリーエッジ。
 * 折り畳みにより非表示となった子孫ノードに接続するエッジをまとめたエッジ。
 * dataの値にはまとめたエッジのidを格納している
 */
const SUMMARISE = 'SUMMARISE'


export default (cy: cytoscape.Core | undefined) => ({
  // -------------------- syntax sugar ------------------------
  actions: {
    expandSelections: () => {
      if (cy) expand(cy, cy.nodes(':selected'))
    },

    collapseSelections: () => {
      if (cy) collapse(cy, cy.nodes(':selected'))
    },

    toggleExpandCollapse: () => {
      if (!cy) return
      const selected = cy.nodes(':selected')
      if (selected.length === 0) return
      if (selected[0].data(IS_COLLAPSED) === undefined) {
        collapse(cy, selected)
      } else {
        expand(cy, selected)
      }
    },
  },

  // ------------ 保存・復元 ----------------
  toViewState: (): ViewState['collapsedNodes'] => {
    if (!cy) return []
    return cy
      .nodes(`[${IS_COLLAPSED}]`)
      .map(n => n.id())
      .sort()
  },

  applyViewState: (viewState: ViewState): void => {
    if (!cy) return
    for (const nodeId of viewState.collapsedNodes) {
      collapse(cy, cy.$id(nodeId))
    }
  },
})

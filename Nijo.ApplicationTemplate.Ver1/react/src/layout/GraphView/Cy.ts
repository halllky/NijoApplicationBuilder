import React, { useCallback, useMemo, useState } from 'react'
import cytoscape from 'cytoscape'
import { UUID } from 'uuidjs'
import Navigator from './Cy.Navigator'
import AutoLayout, { LayoutLogicName } from './Cy.AutoLayout'
import ExpandCollapseFunctions from './Cy.ExpandCollapse'
import { ViewState, useViewState } from './Cy.SaveLoad'
import * as DS from './DataSource'
import { USER_SETTING } from './UserSetting'
import { GraphViewProps } from '.'

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
  applyToCytoscape: (dataSet: DS.DataSet, viewState?: Partial<ViewState>) => Promise<void>;
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
  resetLayout: (layoutName: LayoutLogicName) => void;
  applyViewState: (viewState: Partial<ViewState>) => void;
}

export const useCytoscape = (props: GraphViewProps): CytoscapeHookType => {
  const [cy, setCy] = useState<cytoscape.Core>()
  const [navInstance, setNavInstance] = useState<{ destroy: () => void }>()

  // GraphViewのpropsのうちイベントは常に最新のインスタンスを呼ぶ必要があるのでrefで管理
  const propsRef = React.useRef(props)
  propsRef.current = props

  const containerRef = useCallback((divElement: HTMLElement | null) => {
    if (!cy && divElement) {
      // 初期化
      const cyInstance = cytoscape({
        container: divElement,
        elements: [],
        style: STYLESHEET,
        layout: AutoLayout.DEFAULT,
      })
      if (propsRef.current.showNavigator) {
        setNavInstance(Navigator.setupCyInstance(cyInstance))
      }

      // Cytoscapeは標準でcanvas要素のmousedownでフォーカスを外す処理をしており
      // これによりcanvasを操作しているときにキーボードショートカットが使えないため
      // 無理やり再フォーカスさせてkeydownがトリガーされるようにする。
      cyInstance.on('mousedown', () => {
        divElement.focus()
      })

      // GraphViewのpropsで指定されている各種イベント
      cyInstance.on('dblclick', 'node', event => {
        propsRef.current.onNodeDoubleClick?.(event)
      });
      cyInstance.on('drag', 'node', event => {
        updateTagPositions(cyInstance)
      });
      cyInstance.on('dragfree', 'node', event => {
        propsRef.current.onLayoutChange?.(event)
      });
      cyInstance.on('pan', event => {
        propsRef.current.onLayoutChange?.(event)
      });
      cyInstance.on('zoom', event => {
        propsRef.current.onLayoutChange?.(event)
      });
      cyInstance.on('select', event => {
        propsRef.current.onSelectionChange?.(event)
      });
      cyInstance.on('unselect', event => {
        propsRef.current.onSelectionChange?.(event)
      });

      // レイアウト完了後にタグの位置を更新
      cyInstance.on('layoutstop', () => {
        updateTagPositions(cyInstance)
      });

      setCy(cyInstance)

    } else if (cy && !divElement) {
      // 破棄
      navInstance?.destroy()
    }
  }, [cy, navInstance, propsRef])

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

  const applyToCytoscape = useCallback(async (dataSet: DS.DataSet, viewState?: Partial<ViewState>) => {
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
        cy.add({ data: node })

        // タグがある場合はタグ専用の子ノードを作成
        if (node.tags && node.tags.length > 0) {
          node.tags.forEach((tag, index) => {
            const tagId = `${id}_tag_${index}`
            const tagNodeData = {
              id: tagId,
              label: tag.label,
              isTag: true,
              tagIndex: index,
              parentNodeId: id,
              'color': tag['color'] || '#FFFFFF',
              'background-color': tag['background-color'] || '#FF6B35',
              'border-color': tag['background-color'] || '#FF6B35',
            }
            cy.add({ data: tagNodeData })
          })
        }
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
      } else {
        applyViewState(viewStateBeforeQuery)
      }

      // タグノードの位置を親ノードの右上に配置
      updateTagPositions(cy)

    } finally {
      cy.endBatch()
    }
  }, [cy, collectViewState, applyViewState])

  const resetLayout = useCallback((layoutName: LayoutLogicName) => {
    if (!cy) return;

    // 自動レイアウトを実行する前に、ViewStateが適用されたフラグをクリアする
    cy.removeData('viewStateApplied');

    const baseLayoutOption = AutoLayout.OPTION_LIST[layoutName];
    if (baseLayoutOption) {
      const layoutOptionsWithDefaults = {
        ...baseLayoutOption,
        fit: false,
        animate: false,
        // 他のレイアウトアルゴリズムに固有で、かつ fit や animate と同様の挙動を制御するオプションがあればここに追加
      };
      cy.layout(layoutOptionsWithDefaults).run();
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
    resetLayout,
    applyViewState,
  }
}

/** タグノードの位置を親ノードの右上に配置する */
export const updateTagPositions = (cyInstance: cytoscape.Core) => {
  // 保有者ID単位で処理するために、保有者IDとタグノードの配列をマップに格納
  const ownerIdAndTags = new Map<string, cytoscape.NodeSingular[]>()
  cyInstance.nodes('[isTag]').forEach((tagNode) => {
    const parentId = tagNode.data('parentNodeId')
    if (ownerIdAndTags.has(parentId)) {
      ownerIdAndTags.get(parentId)?.push(tagNode)
    } else {
      ownerIdAndTags.set(parentId, [tagNode])
    }
  })

  for (const [ownerId, tagNodes] of ownerIdAndTags) {
    const ownerNode = cyInstance.getElementById(ownerId)
    if (ownerNode.length === 0) continue

    const ownerBB = ownerNode[0].boundingBox({
      includeOverlays: false, // ドラッグ中のノードを囲うように表示されるオーバーレイのサイズを除外
    })

    // tagIndexが大きい順に右から左に向かって詰めて表示
    const tagNodesSortedByIndex = tagNodes.sort((a, b) => (b.data('tagIndex') ?? 0) - (a.data('tagIndex') ?? 0))
    let offsetXTotal = 0
    for (const tagNode of tagNodesSortedByIndex) {
      const width = tagNode.width()
      const offsetX = (ownerBB.w / 2) - (width / 2) - offsetXTotal - 1
      const offsetY = -(ownerBB.h / 2) - 4
      tagNode.position({
        x: ownerNode[0].position('x') + offsetX,
        y: ownerNode[0].position('y') + offsetY,
      })
      offsetXTotal += width + 2
    }
  }
}

/** スタイルシート */
const STYLESHEET: cytoscape.CytoscapeOptions['style'] = [{
  selector: 'node',
  css: {
    'shape': 'rectangle',
    'width': (node: cytoscape.NodeSingular) => Math.max(32, (node.data('label') as string)?.length * 20),
    'text-valign': 'center',
    'text-halign': 'center',
    'color': (node: cytoscape.NodeSingular) => (node.data('color') as string) ?? '#000000',
    'border-width': '1px',
    'border-color': node => (node.data('border-color') as string) ?? '#909090',
    'background-color': node => (node.data('background-color') as string) ?? '#666666',
    'background-opacity': .1,
    'label': 'data(label)',
  },
}, {
  selector: 'node[isTag]',
  css: {
    'shape': 'round-rectangle',
    'width': (node: cytoscape.NodeSingular) => Math.max(20, (node.data('label') as string)?.length * 8 + 10),
    'height': '18px',
    'text-valign': 'center',
    'text-halign': 'center',
    'font-size': '10px',
    'color': (node: cytoscape.NodeSingular) => (node.data('color') as string) ?? '#FFFFFF',
    'border-width': '1px',
    'border-color': node => (node.data('border-color') as string) ?? '#FF6B35',
    'background-color': node => (node.data('background-color') as string) ?? '#FF6B35',
    'background-opacity': 1,
    'label': 'data(label)',
    'text-outline-width': 0,
    'z-index': 10,
    'events': 'no',
  },
}, {
  selector: 'node[tags]',
  css: {
    'label': 'data(label)',
    'compound-sizing-wrt-labels': 'exclude',
  },
}, {
  selector: 'node:selected',
  style: {
    'border-style': 'dashed',
    'border-width': '1px',
    'border-color': node => (node.data('border-color:selected') as string) ?? '#FF4F02',
  },
}, {
  selector: 'node:parent', // 子要素をもつノードに適用される
  css: {
    'text-valign': 'top', // ラベルをノードの上部外側に配置
    'padding': '20px', // parentが複数重なるとラベルが重なるので、ノードの上部分に余白を持たせる
    'color': (node: cytoscape.NodeSingular) => (node.data('color:container') as string) ?? '#707070',
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

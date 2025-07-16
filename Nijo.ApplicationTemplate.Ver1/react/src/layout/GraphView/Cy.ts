import React, { useCallback, useMemo, useState } from 'react'
import cytoscape from 'cytoscape'
import { UUID } from 'uuidjs'
// @ts-ignore このライブラリは型定義を提供していない
import nodeHtmlLabel from "cytoscape-node-html-label"
import Navigator from './Cy.Navigator'
import AutoLayout, { LayoutLogicName } from './Cy.AutoLayout'
import ExpandCollapseFunctions from './Cy.ExpandCollapse'
import { ViewState, useViewState } from './Cy.SaveLoad'
import * as DS from './DataSource'
import { USER_SETTING } from './UserSetting'
import { GraphViewProps } from '.'

AutoLayout.configure(cytoscape)
Navigator.configure(cytoscape)

// cytoscape-node-html-label拡張機能を登録
nodeHtmlLabel(cytoscape)

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
        style: getStyleSheet(),
        layout: AutoLayout.DEFAULT,
      })

      // HTMLラベルテンプレートを設定
      setupHtmlLabels(cyInstance)

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
        updateMemberPositions(cyInstance)
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
        updateMemberPositions(cyInstance)
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

        // メンバーがある場合はメンバー専用の子ノードを作成
        if (node.members && node.members.length > 0) {
          node.members.forEach((member, index) => {
            const memberId = `${id}_member_${index}`
            const memberNodeData = {
              id: memberId,
              label: member,
              isMember: true,
              memberIndex: index,
              parentNodeId: id,
            }
            cy.add({ data: memberNodeData })
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
      // メンバーノードの位置を親ノードの直下に配置
      updateMemberPositions(cy)

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

        // 親ノードに付随して位置が定まるノードをレイアウト対象外にする
        eles: cy.nodes().filter(node => {
          // isMember, isTag, parent（親ノード）がある場合は除外
          if (node.data('isMember') !== undefined) return false;
          if (node.data('isTag') !== undefined) return false;
          if (node.data('parentNodeId') !== undefined) return false;
          return true;
        }),
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

/** メンバーノードの位置を親ノードの内部に配置する */
export const updateMemberPositions = (cyInstance: cytoscape.Core) => {
  // 保有者ID単位で処理するために、保有者IDとメンバーノードの配列をマップに格納
  const ownerIdAndMembers = new Map<string, cytoscape.NodeSingular[]>()
  cyInstance.nodes('[isMember]').forEach((memberNode) => {
    const parentId = memberNode.data('parentNodeId')
    if (ownerIdAndMembers.has(parentId)) {
      ownerIdAndMembers.get(parentId)?.push(memberNode)
    } else {
      ownerIdAndMembers.set(parentId, [memberNode])
    }
  })

  for (const [ownerId, memberNodes] of ownerIdAndMembers) {
    const ownerNode = cyInstance.getElementById(ownerId)
    if (ownerNode.length === 0) continue

    const ownerBB = ownerNode[0].boundingBox({
      includeOverlays: false, // ドラッグ中のノードを囲うように表示されるオーバーレイのサイズを除外
    })

    // memberIndexが小さい順に上から下に向かって配置
    const memberNodesSortedByIndex = memberNodes.sort((a, b) => (a.data('memberIndex') ?? 0) - (b.data('memberIndex') ?? 0))

    // 親ノードの上辺を基準として開始位置を調整
    const ownerCenter = ownerNode[0].position()
    const ownerTop = ownerCenter.y - (ownerBB.h / 2) - 8
    const startY = ownerTop + (MEMBER_HEIGHT / 2)

    let offsetYTotal = 0
    for (const memberNode of memberNodesSortedByIndex) {
      memberNode.position({
        x: ownerCenter.x,
        y: startY + offsetYTotal + MEMBER_HEIGHT / 2,
      })
      offsetYTotal += MEMBER_HEIGHT
    }
  }
}
/** members の各要素の高さ */
const MEMBER_HEIGHT = 20
/** 親ノードのラベルのパディング */
const PARENT_NODE_PADDING = 20

/** HTMLラベルテンプレートを設定する */
const setupHtmlLabels = (cyInstance: cytoscape.Core) => {
  (cyInstance as any).nodeHtmlLabel([
    {
      query: 'node', // 全てのノードに適用
      valign: 'center',
      halign: 'center',
      tpl: (data: any) => {
        const label = data.label || ''
        // 改行文字（\n または \r\n）を<br>タグに変換
        const htmlLabel = label.replace(/\r\n|\n/g, '<br>')
        return `<div style="pointer-events: none;">${htmlLabel}</div>`
      }
    },
    {
      query: 'node[members]', // メンバーを持つノードに適用
      valign: 'top',
      valignBox: 'top',
      halign: 'center',
      tpl: (data: any) => {
        const label = data.label || ''
        // 改行文字（\n または \r\n）を<br>タグに変換
        const htmlLabel = label.replace(/\r\n|\n/g, '<br>')
        return `<div style="pointer-events: none;">${htmlLabel}</div>`
      }
    },
    {
      query: 'node:parent', // 親ノードに適用
      valign: 'top',
      valignBox: 'top',
      halign: 'center',
      tpl: (data: any) => {
        const label = data.label || ''
        // 改行文字（\n または \r\n）を<br>タグに変換
        const htmlLabel = label.replace(/\r\n|\n/g, '<br>')
        return `<div style="transform: translateY(-${PARENT_NODE_PADDING}px); text-align: center; pointer-events: none;">${htmlLabel}</div>`
      }
    },
    {
      query: 'node[isMember]', // メンバーノード用
      valign: 'center',
      halign: 'center',
      tpl: (data: any) => {
        const label = data.label || ''
        return `<div style="pointer-events: none;">${label}</div>`
      }
    }
  ])
}

/** スタイルシート */
const getStyleSheet = (): cytoscape.CytoscapeOptions['style'] => {
  // テキストの幅を推定する関数
  const canvas = document.createElement('canvas')
  const canvasContext = canvas.getContext('2d')
  if (!canvasContext) return []
  const estimateTextWidth = (text: string) => {
    canvasContext.font = '16px Noto Sans JP'
    return canvasContext.measureText(text).width
  }

  return [{
    selector: 'node',
    css: {
      'shape': 'rectangle',
      'width': (node: cytoscape.NodeSingular) => {
        const members = node.data('members') as string[] | undefined
        const maxTextLength = members && members.length > 0
          ? Math.max(...members.map(m => estimateTextWidth(m)))
          : estimateTextWidth(node.data('label') as string)
        return Math.max(32, maxTextLength + 8)
      },
      'height': (node: cytoscape.NodeSingular) => {
        const members = node.data('members') as string[] | undefined
        if (members && members.length > 0) {
          // メンバーがある場合は高さを調整
          return members.length * MEMBER_HEIGHT
        } else {
          // ラベルの改行数に基づいて高さを計算
          const label = (node.data('label') as string) || ''
          const lines = label.split(/\r\n|\n/)
          const lineHeight = 16 * 1.2 // フォントサイズ16px × line-height 1.2
          return Math.max(32, lines.length * lineHeight + 8) // 最小32px、上下パディング8px
        }
      },
      'color': (node: cytoscape.NodeSingular) => (node.data('color') as string) ?? '#000000',
      'border-width': '1px',
      'border-color': node => (node.data('border-color') as string) ?? '#909090',
      'background-color': node => (node.data('background-color') as string) ?? '#666666',
      'background-opacity': .1,
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
    selector: 'node[isMember]',
    css: {
      'shape': 'rectangle',
      'width': (node: cytoscape.NodeSingular) => {
        const parentNodeId = node.data('parentNodeId')
        if (parentNodeId) {
          // 親ノードと同じ幅にする
          const parentNode = node.cy().getElementById(parentNodeId)
          if (parentNode.length > 0) {
            return parentNode.width()
          }
        }
        return Math.max(80, (node.data('label') as string)?.length * 8 + 20)
      },
      'height': MEMBER_HEIGHT,
      'font-size': '12px',
      'border-width': '1px',
      'border-color': node => (node.data('border-color') as string) ?? '#909090',
      'background-opacity': 0,
      'z-index': 5,
      'events': 'no', // メンバーはドラッグできないようにする
    },
  }, {
    selector: 'node[tags]',
    css: {
      'label': '', // HTMLラベルを使用するため標準ラベルを無効化
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
      'padding': `${PARENT_NODE_PADDING}px`, // parentが複数重なるとラベルが重なるので、ノードの上部分に余白を持たせる
      'color': (node: cytoscape.NodeSingular) => (node.data('color:container') as string) ?? '#707070',
    },
  }, {
    selector: 'edge',
    style: {
      'label': 'data(label)',
      'color': '#707070',
      'line-color': edge => edge.data('line-color') ?? '#707070',
      'line-style': edge => edge.data('line-style') ?? 'solid',
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
}
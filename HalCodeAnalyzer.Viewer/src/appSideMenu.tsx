import cytoscape from 'cytoscape'
import { useCallback, useMemo, useState } from 'react'
import GraphView from './GraphView'
import { Tree } from './util'

type CyElementTreeNode = Tree.TreeNode<cytoscape.ElementDefinition>
type TreeNodeState = {
  collapse?: boolean
  hideInGraph?: boolean
}
export const TreeExplorer = ({ className }: {
  className?: string
}) => {
  const [{ cy, elements }] = GraphView.useGraphContext()
  const flattenTree = useMemo(() => {
    const nodes = elements.filter(item => !item.data.source) // edgeをはじく
    const tree = Tree.toTree(nodes, x => x.data.id ?? '', x => x.data.parent)
    return Tree.flatten(tree)
  }, [elements])

  const [nodeState, setNodeState] = useState<Map<string, TreeNodeState>>(new Map())

  // 開閉
  const handleExpandCollapse = useCallback((node: CyElementTreeNode, collapse: boolean) => {
    const id = node.item.data.id ?? ''
    nodeState.set(id, { ...nodeState.get(id), collapse })
    setNodeState(new Map(nodeState))
  }, [nodeState])
  const isExpanded = useCallback((node: CyElementTreeNode): boolean => {
    return Tree.getAncestors(node).every(a => !nodeState.get(a.item.data.id ?? '')?.collapse)
  }, [nodeState])
  const collapseAll = useCallback(() => {
    for (const node of flattenTree) handleExpandCollapse(node, true)
  }, [flattenTree, handleExpandCollapse])

  // グラフ中での可視状態の切り替え
  const handleVisibility = useCallback((node: CyElementTreeNode, hideInGraph: boolean) => {
    const nodes = hideInGraph
      ? Tree.getDescendantsAndSelf(node)
      : [...Tree.getAncestors(node), ...Tree.getDescendantsAndSelf(node)]
    for (const n of nodes) {
      const id = n.item.data.id ?? ''
      nodeState.set(id, { ...nodeState.get(id), hideInGraph })
      setNodeState(new Map(nodeState))
      cy?.nodes(`[id='${id}']`).style('display', hideInGraph ? 'none' : '')
    }
  }, [nodeState, cy])
  // 全切り替え
  const allHidden = useMemo(() => {
    return flattenTree.every(node => nodeState.get(node.item.data.id ?? '')?.hideInGraph)
  }, [flattenTree, nodeState])
  const toggleVisibleAll = useCallback(() => {
    const newValue = !allHidden
    for (const node of flattenTree) {
      const id = node.item.data.id ?? ''
      nodeState.set(id, { ...nodeState.get(id), hideInGraph: newValue })
    }
    cy?.nodes().style('display', newValue ? 'none' : '')
    setNodeState(new Map(nodeState))
  }, [flattenTree, nodeState, allHidden])

  return (
    <div className={`flex flex-col ${className}`}>
      <div className="pl-1">
        <input type="checkbox" checked={!allHidden} onChange={toggleVisibleAll} />
        <button onClick={collapseAll}>ルートに折りたたむ</button>
      </div>
      <ul className="overflow-x-hidden select-none pl-1">
        {flattenTree.filter(node => isExpanded(node)).map(node => (
          <li key={node.item.data.id} className="flex items-center overflow-hidden">
            <input
              type="checkbox"
              checked={!nodeState.get(node.item.data.id ?? '')?.hideInGraph}
              onChange={e => handleVisibility(node, !e.target.checked)}
            />
            <div style={{ minWidth: node.depth * 20, backgroundColor: 'tomato' }}></div>
            <CollapseButton
              visible={node.children.length > 0}
              collapse={nodeState.get(node.item.data.id ?? '')?.collapse}
              onChange={v => handleExpandCollapse(node, v)}
            />
            <span className="flex-1">
              {node.item.data.label}
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}

// ------------------- UI部品 --------------------

const CollapseButton = ({ visible, collapse, onChange }: {
  visible?: boolean
  collapse?: boolean
  onChange?: (v: boolean) => void
}) => {
  return (
    <span
      className="flex justify-center min-w-6 text-slate-400"
      style={{ visibility: visible ? undefined : 'hidden' }}
      onClick={() => onChange?.(!collapse)}
    >
      {collapse ? '+' : '-'}
    </span>
  )
}

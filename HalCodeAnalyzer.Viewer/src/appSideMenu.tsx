import cytoscape from 'cytoscape'
import React, { useCallback, useMemo, useRef, useState } from 'react'
import GraphView from './GraphView'
import { Components, Tree } from './util'
import { Link, useLocation } from 'react-router-dom'

export type UsePagesHook = () => {
  menuItems: SideMenuSection[]
  Routes: () => React.ReactNode
}
export type SideMenuSection = SideMenuSectionItem & {
  order?: number
}
export type SideMenuSectionItem = {
  itemId: string
  label: string
  url?: string
  children?: SideMenuSectionItem[]
  onRename?: (name: string) => void
  actions?: { actionName: string, onClick: () => void }[]
}

// -------------- コンポーネント ------------------
export const Explorer = ({ sections }: {
  sections: SideMenuSection[]
}) => {
  // 展開/折りたたみ
  const [collapsedIds, setCollapsedIds] = useState<Set<string>>(() => new Set())
  const handleExpandCollapse = useCallback((node: Tree.TreeNode<SideMenuSectionItem>, collapse: boolean) => {
    if (collapse) {
      collapsedIds.add(node.item.itemId)
    } else {
      collapsedIds.delete(node.item.itemId)
    }
    setCollapsedIds(new Set(collapsedIds))
  }, [collapsedIds])
  const getExpandedDescendantsAndSelf = useCallback((node: Tree.TreeNode<SideMenuSectionItem>) => {
    const entireTree = Tree.getDescendantsAndSelf(node)
    const expandedItems = entireTree.filter(desc => {
      return Tree.getAncestors(desc).every(a => !collapsedIds.has(a.item.itemId))
    })
    return expandedItems
  }, [collapsedIds])

  // メニュー項目
  const treeNodes = useMemo(() => {
    const ordered = Array.from(sections)
    ordered.sort((a, b) => {
      const aOrder = a.order ?? Number.MAX_SAFE_INTEGER
      const bOrder = b.order ?? Number.MAX_SAFE_INTEGER
      if (aOrder < bOrder) return -1
      if (aOrder > bOrder) return 1
      return 0
    })
    const rootNodes = Tree.toTree(ordered as SideMenuSectionItem[], {
      getId: x => x.itemId,
      getChildren: x => x.children,
    })
    const entireTree = rootNodes.flatMap(root => {
      return Tree.getDescendantsAndSelf(root)
    })
    const expandedItems = entireTree.filter(desc => {
      return Tree.getAncestors(desc).every(a => !collapsedIds.has(a.item.itemId))
    })
    return expandedItems
  }, [sections])

  // 表示中の画面を強調する
  const { pathname } = useLocation()

  // 名前変更
  const [renamingItem, setRenamingItem] = useState<Tree.TreeNode<SideMenuSectionItem>>()
  const [renamingName, setRenamingName] = useState<string>('')
  const renameRef = useRef<HTMLInputElement>(null)
  const startRenaming = useCallback((node: Tree.TreeNode<SideMenuSectionItem>) => {
    setRenamingItem(node)
    setRenamingName(node.item.label)
    window.setTimeout(() => renameRef.current?.focus(), 10)
  }, [])
  const endRenaming = useCallback(() => {
    renamingItem?.item.onRename?.(renamingName)
    setRenamingItem(undefined)
    setRenamingName('')
  }, [renamingItem, renamingName])

  return (
    <div className="flex flex-col overflow-x-hidden select-none bg-stone-300">
      {treeNodes.map(node => (
        <div
          key={node.item.itemId}
          className={`flex items-center
            ${pathname === node.item.url
              ? 'hover:bg-stone-200 border-1 border-stone-400 border-y bg-stone-100'
              : 'hover:bg-stone-200 border-1 border-stone-400 border-r'}
            ${node.depth === 0 && 'font-bold'}`}
        >
          <div style={{ minWidth: node.depth * 20 }}></div>
          <CollapseButton
            visible={node.children.length > 0}
            collapsed={collapsedIds.has(node.item.itemId)}
            onChange={v => handleExpandCollapse(node, v)}
          />
          {renamingItem?.item.itemId === node.item.itemId && (
            <Components.Text
              ref={renameRef}
              className="flex-1"
              value={renamingName}
              onChange={e => setRenamingName(e.target.value)}
              onBlur={endRenaming}
            />
          )}
          {renamingItem?.item.itemId !== node.item.itemId && node.item.url && (
            <Link to={node.item.url} className="flex-1 text-nowrap">
              {node.item.label || '(名前なし)'}
            </Link>
          )}
          {renamingItem?.item.itemId !== node.item.itemId && !node.item.url && (
            <span className="flex-1 text-nowrap">
              {node.item.label || '(名前なし)'}
            </span>
          )}
          {renamingItem === undefined && pathname === node.item.url && node.item.onRename && (
            <Components.Button onClick={() => startRenaming(node)}>
              改名
            </Components.Button>
          )}
          {renamingItem === undefined && pathname === node.item.url && node.item.actions?.map((act, actIx) => (
            <Components.Button key={actIx} onClick={act.onClick}>
              {act.actionName}
            </Components.Button>
          ))}
        </div>
      ))}
      {/* 下のスペース埋め */}
      <div className="flex-1 border-r border-1 border-stone-400"></div>
    </div>
  )
}

// -------------- ふるい仕組み ------------------
type CyElementTreeNode = Tree.TreeNode<cytoscape.ElementDefinition>
type TreeNodeState = {
  collapse?: boolean
  hideInGraph?: boolean
}
const TreeExplorer = ({ className }: {
  className?: string
}) => {
  const [{ cy, elements }] = GraphView.useGraphContext()
  const flattenTree = useMemo(() => {
    const nodes = elements.filter(item => !item.data.source) // edgeをはじく
    const tree = Tree.toTree(nodes, {
      getId: x => x.data.id ?? '',
      getParent: x => x.data.parent,
    })
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
              collapsed={nodeState.get(node.item.data.id ?? '')?.collapse}
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
const CollapseButton = ({ visible, collapsed, onChange }: {
  visible?: boolean
  collapsed?: boolean
  onChange?: (v: boolean) => void
}) => {
  return (
    <span
      className="flex justify-center min-w-6 opacity-50"
      style={{ visibility: visible ? undefined : 'hidden' }}
      onClick={() => onChange?.(!collapsed)}
    >
      {collapsed ? '+' : '-'}
    </span>
  )
}

import cytoscape from 'cytoscape'
import React, { useCallback, useMemo, useState } from 'react'
import GraphView from './GraphView'
import { Components, ContextUtil, Tree } from './util'
import { Link } from 'react-router-dom'

const getDefaultSideMenuState = () => ({
  sections: [
    { itemId: 'APP::HOME', label: 'ホーム', url: '/', order: 999 },
    { itemId: 'APP::SETTINGS', label: '設定', url: '/settings', order: 999 },
  ] as SideMenuSection[]
})

// -------------- フック ------------------
export type SideMenuSection = SideMenuSectionItem & {
  order?: number
}
export type SideMenuSectionItem = {
  itemId: string
  label: string
  url?: string
  children?: SideMenuSectionItem[]
}
const SideMenuContext = ContextUtil.createContextEx(getDefaultSideMenuState())

const useSideMenu = () => {
  const [{ sections }, dispatch] = ContextUtil.useContextEx(SideMenuContext)
  const registerMenu = useCallback((section: SideMenuSection) => {
    const registered = sections.find(s => s.itemId === section.itemId)
    if (registered === undefined) {
      dispatch({ update: 'sections', value: [section, ...sections] })
    } else if (registered.label !== section.label
      || registered.children !== section.children) {
      const value = [section, ...sections.filter(s => s.itemId !== section.itemId)]
      dispatch({ update: 'sections', value })
    }
  }, [sections])
  return { registerMenu }
}

// -------------- コンポーネント ------------------
const ContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const contextValue = ContextUtil.useReducerEx(getDefaultSideMenuState())
  return (
    <SideMenuContext.Provider value={contextValue}>
      {children}
    </SideMenuContext.Provider>
  )
}

const Explorer = () => {
  // メニュー項目
  const [{ sections }] = ContextUtil.useContextEx(SideMenuContext)
  const treeRoots = useMemo(() => {
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
    return rootNodes
  }, [sections])

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
  const getVisibleDescendantsAndSelf = useCallback((node: Tree.TreeNode<SideMenuSectionItem>) => {
    return Tree
      .getDescendantsAndSelf(node)
      .filter(desc => Tree.getAncestors(desc).every(a => !collapsedIds.has(a.item.itemId)))
  }, [collapsedIds])

  return (
    <div className="flex flex-col overflow-x-hidden select-none">
      {treeRoots.map((root, ix) => (
        <React.Fragment key={root.item.itemId}>
          {ix !== 0 && <Components.Separator />}
          {getVisibleDescendantsAndSelf(root).map(node => (
            <div key={node.item.itemId} className="flex items-center hover:bg-blue-200">
              <div style={{ minWidth: node.depth * 20 }}></div>
              <CollapseButton
                visible={node.children.length > 0}
                collapsed={collapsedIds.has(node.item.itemId)}
                onChange={v => handleExpandCollapse(node, v)}
              />
              {node.item.url ? (
                <Link to={node.item.url} className="flex-1 text-nowrap">
                  {node.item.label}
                </Link>
              ) : (
                <span className="flex-1 text-nowrap">
                  {node.item.label}
                </span>
              )}
            </div>
          ))}
        </React.Fragment>
      ))}
    </div>
  )
}

export default {
  ContextProvider,
  Explorer,
  useSideMenu,
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
      className="flex justify-center min-w-6 text-slate-400"
      style={{ visibility: visible ? undefined : 'hidden' }}
      onClick={() => onChange?.(!collapsed)}
    >
      {collapsed ? '+' : '-'}
    </span>
  )
}

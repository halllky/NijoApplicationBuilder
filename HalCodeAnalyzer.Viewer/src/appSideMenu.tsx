// import React, { useCallback, useMemo, useRef, useState } from 'react'
// import { Link, useLocation } from 'react-router-dom'
// import * as Icon from '@ant-design/icons'
// import { Components, ReactHookUtil, Tree } from './util'

// // -------------- type ------------------
// export type UsePagesHook = () => {
//   menuItems: SideMenuSection[]
//   Routes: () => React.ReactNode
// }
// export type SideMenuSection = SideMenuSectionItem & {
//   order?: number
// }
// export type SideMenuSectionItem = {
//   itemId: string
//   label: string
//   url?: string
//   children?: SideMenuSectionItem[]
//   onRename?: (name: string) => void
//   actions?: { icon: React.ElementType, actionName: string, onClick: () => void }[]
// }

// // -------------- hook ------------------
// type AppState = { showSideMenu: boolean }
// const getDefaultAppState = (): AppState => ({ showSideMenu: true })

// export const [
//   SideMenuContextProvider,
//   useSideMenuContext,
// ] = ReactHookUtil.defineContext(
//   getDefaultAppState,
//   state => ({
//     toggleSideMenu: () => {
//       return { ...state, showSideMenu: !state.showSideMenu }
//     },
//   }),
//   () => ({
//     storageKey: 'HALDIAGRAM::APPVIEWSTATE',
//     defaultValue: getDefaultAppState,
//     serialize: obj => JSON.stringify(obj),
//     deserialize: str => ({ ok: true, obj: JSON.parse(str) }),
//     noMessageOnSave: true,
//   }),
// )

// // -------------- コンポーネント ------------------
// export const Explorer = ({ sections }: {
//   sections: SideMenuSection[]
// }) => {
//   // 展開/折りたたみ
//   const [collapsedIds, setCollapsedIds] = useState<Set<string>>(() => new Set())
//   const handleExpandCollapse = useCallback((node: Tree.TreeNode<SideMenuSectionItem>, collapse: boolean) => {
//     if (collapse) {
//       collapsedIds.add(node.item.itemId)
//     } else {
//       collapsedIds.delete(node.item.itemId)
//     }
//     setCollapsedIds(new Set(collapsedIds))
//   }, [collapsedIds])

//   // メニュー項目
//   const treeNodes = useMemo(() => {
//     const ordered = Array.from(sections)
//     ordered.sort((a, b) => {
//       const aOrder = a.order ?? Number.MAX_SAFE_INTEGER
//       const bOrder = b.order ?? Number.MAX_SAFE_INTEGER
//       if (aOrder < bOrder) return -1
//       if (aOrder > bOrder) return 1
//       return 0
//     })
//     const rootNodes = Tree.toTree(ordered as SideMenuSectionItem[], {
//       getId: x => x.itemId,
//       getChildren: x => x.children,
//     })
//     const entireTree = rootNodes.flatMap(root => {
//       return Tree.getDescendantsAndSelf(root)
//     })
//     const expandedItems = entireTree.filter(desc => {
//       return Tree.getAncestors(desc).every(a => !collapsedIds.has(a.item.itemId))
//     })
//     return expandedItems
//   }, [sections, collapsedIds])

//   // 表示中の画面を強調する
//   const { pathname } = useLocation()

//   // 名前変更
//   const [renamingItem, setRenamingItem] = useState<Tree.TreeNode<SideMenuSectionItem>>()
//   const [renamingName, setRenamingName] = useState<string>('')
//   const renameRef = useRef<HTMLInputElement>(null)
//   const startRenaming = useCallback((node: Tree.TreeNode<SideMenuSectionItem>) => {
//     setRenamingItem(node)
//     setRenamingName(node.item.label)
//     window.setTimeout(() => renameRef.current?.focus(), 10)
//   }, [])
//   const endRenaming = useCallback(() => {
//     renamingItem?.item.onRename?.(renamingName)
//     setRenamingItem(undefined)
//     setRenamingName('')
//   }, [renamingItem, renamingName])

//   return (
//     <div className="flex flex-col overflow-x-hidden select-none bg-stone-300">
//       {treeNodes.map(node => (
//         <div
//           key={node.item.itemId}
//           className={`flex items-center
//             ${pathname === node.item.url
//               ? 'hover:bg-stone-200 border-1 border-stone-400 border-y bg-stone-100'
//               : 'hover:bg-stone-200 border-1 border-r border-r-stone-400 border-y border-y-stone-300'}
//             ${node.depth === 0 && 'font-bold'}`}
//         >
//           <div style={{ minWidth: node.depth * 20 }}></div>
//           <CollapseButton
//             visible={node.children.length > 0}
//             collapsed={collapsedIds.has(node.item.itemId)}
//             onChange={v => handleExpandCollapse(node, v)}
//           />
//           {renamingItem?.item.itemId === node.item.itemId && (
//             <Components.Text
//               ref={renameRef}
//               className="flex-1"
//               value={renamingName}
//               onChange={e => setRenamingName(e.target.value)}
//               onBlur={endRenaming}
//             />
//           )}
//           {renamingItem?.item.itemId !== node.item.itemId && node.item.url && (
//             <Link to={node.item.url} className="flex-1 text-nowrap overflow-hidden text-ellipsis">
//               {node.item.label || '(名前なし)'}
//             </Link>
//           )}
//           {renamingItem?.item.itemId !== node.item.itemId && !node.item.url && (
//             <span className="flex-1 text-nowrap overflow-hidden text-ellipsis">
//               {node.item.label || '(名前なし)'}
//             </span>
//           )}
//           {renamingItem === undefined && pathname === node.item.url && node.item.onRename && (
//             <Components.Button onClick={() => startRenaming(node)} icon={Icon.EditOutlined}>
//               改名
//             </Components.Button>
//           )}
//           {renamingItem === undefined && pathname === node.item.url && node.item.actions?.map((act, actIx) => (
//             <Components.Button key={actIx} onClick={act.onClick} icon={act.icon}>
//               {act.actionName}
//             </Components.Button>
//           ))}
//         </div>
//       ))}
//       {/* 下のスペース埋め */}
//       <div className="flex-1 border-r border-1 border-stone-400"></div>
//     </div>
//   )
// }

// // ------------------- UI部品 --------------------
// const CollapseButton = ({ visible, collapsed, onChange }: {
//   visible?: boolean
//   collapsed?: boolean
//   onChange?: (v: boolean) => void
// }) => {
//   return (
//     <span
//       className="flex justify-center min-w-6 opacity-50"
//       style={{ visibility: visible ? undefined : 'hidden' }}
//       onClick={() => onChange?.(!collapsed)}
//     >
//       {collapsed ? '+' : '-'}
//     </span>
//   )
// }

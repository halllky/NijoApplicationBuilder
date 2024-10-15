import React from "react";
import useEvent from "react-use-event-hook";
import { AggregateOrMember, GridRow, PageState } from "./types";

export type UseFlattenArrayTreeReturns = ReturnType<typeof useFlattenArrayTree>
type GridRowId = Exclude<PageState['aggregates'], undefined>[0]['uniqueId']

/**
 * ツリー構造に関連する状態と操作を提供する。
 * 通常のツリー構造は parent と children で親子が互いに参照を持つが、
 * UI上でインデントがころころ変わる都合で、depthのみを正としつつ、祖先・子孫の関係は動的に計算するほうが使いやすい。
 */
export const useFlattenArrayTree = (allRows: PageState['aggregates'], listRef: React.MutableRefObject<PageState['aggregates']>) => {

  // ------------------------------------------
  // 祖先・子孫の取得

  const getParent = useEvent((gridRow: GridRow): GridRow | undefined => {
    if (listRef.current === undefined) return undefined
    let currentIndex = listRef.current.indexOf(gridRow)
    if (currentIndex === -1) throw new Error('index is out of range.')
    while (true) {
      currentIndex--
      if (currentIndex < 0) return undefined

      const maybeParent = listRef.current[currentIndex]
      if (maybeParent.depth < gridRow.depth) return maybeParent
    }
  })

  const getAncestors = useEvent((gridRow: GridRow): GridRow[] => {
    if (listRef.current === undefined) return []
    const ancestors: GridRow[] = []
    let currentDepth = gridRow.depth
    let currentIndex = listRef.current.indexOf(gridRow)
    if (currentIndex === -1) throw new Error('index is out of range.')
    while (true) {
      if (currentDepth === 0) break

      currentIndex--
      if (currentIndex < 0) break

      const maybeAncestor = listRef.current[currentIndex]
      if (maybeAncestor.depth < currentDepth) {
        ancestors.push(maybeAncestor)
        currentDepth = maybeAncestor.depth
      }
    }
    return ancestors.reverse()
  })

  const getChildren = useEvent((gridRow: GridRow): GridRow[] => {
    if (listRef.current === undefined) return []
    const children: AggregateOrMember[] = []
    let currentIndex = listRef.current.indexOf(gridRow)
    if (currentIndex === -1) throw new Error('index is out of range.')
    while (true) {
      currentIndex++
      if (currentIndex >= listRef.current.length) break

      const maybeChild = listRef.current[currentIndex]
      if (maybeChild.depth <= gridRow.depth) break

      if (getParent(maybeChild) == gridRow) children.push(maybeChild)
    }
    return children
  })

  const getDescendants = useEvent((gridRow: GridRow): GridRow[] => {
    if (listRef.current === undefined) return []
    const descendants: AggregateOrMember[] = []
    let currentIndex = listRef.current.indexOf(gridRow)
    if (currentIndex === -1) throw new Error('index is out of range.')
    while (true) {
      currentIndex++
      if (currentIndex >= listRef.current.length) break

      const maybeDescendant = listRef.current[currentIndex]
      if (maybeDescendant.depth <= gridRow.depth) break

      descendants.push(maybeDescendant)
    }
    return descendants
  })

  // ------------------------------------------
  // 折り畳み
  const [collapsedRowIds, setCollapsedRowIds] = React.useState(new Set<GridRowId>())
  const expandAll = useEvent(() => {
    setCollapsedRowIds(new Set())
  })
  const collapseAll = useEvent(() => {
    const rootRows = allRows?.filter(row => row.depth === 0)
    setCollapsedRowIds(new Set(rootRows?.map(row => row.uniqueId)))
  })
  const toggleCollapsing = useEvent((uniqueId: GridRowId) => {
    const set = new Set(collapsedRowIds)
    if (collapsedRowIds.has(uniqueId)) {
      set.delete(uniqueId)
    } else {
      set.add(uniqueId)
    }
    setCollapsedRowIds(set)
  })

  const expandableRows = React.useMemo(() => {
    const result = new Set<GridRowId>()
    if (allRows) {
      for (let i = 0; i < allRows.length; i++) {
        const row = allRows[i]
        const nextRow = allRows[i + 1]
        if (nextRow === undefined) continue
        if (nextRow.depth <= row.depth) continue
        result.add(row.uniqueId)
      }
    }
    return result
  }, [allRows])

  const expandedRows = React.useMemo(() => {
    if (!allRows) return []
    const expanded: GridRow[] = []
    for (let i = 0; i < allRows.length; i++) {
      const row = allRows[i]
      expanded.push(row)

      // その行が折りたたまれているとき、その行の子孫はexpandedの配列に加えない
      if (collapsedRowIds.has(row.uniqueId)) {
        i += getDescendants(row).length
      }
    }
    return expanded
  }, [allRows, collapsedRowIds, getDescendants])

  // ------------------------------------------

  return {
    /** 直近の親を取得 */
    getParent,
    /** 祖先を列挙。より階層が浅い方が先。 */
    getAncestors,
    /** 直近の子を列挙 */
    getChildren,
    /** 子孫を列挙 */
    getDescendants,

    /** 折りたたまれた行のID */
    collapsedRowIds,
    /** 折りたたみ可能な行 */
    expandableRows,
    /** 折りたたまれていない行 */
    expandedRows,
    /** 特定の行の折りたたみ状態を切り替えます。 */
    toggleCollapsing,
    /** すべての行を折りたたみます。 */
    collapseAll,
    /** すべての行の折りたたみ状態を解除します。 */
    expandAll,
  }
}

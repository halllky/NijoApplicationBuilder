
import useEvent from "react-use-event-hook";
import { AggregateOrMember, GridRow, PageState } from "./types";

export type UseFlattenArrayTreeReturns = ReturnType<typeof useFlattenArrayTree>

/**
 * 平たい配列ツリーへの操作を提供する。
 * 通常のツリー構造は parent と children で親子が互いに参照を持つが、
 * UI上でインデントがころころ変わる都合で、depthのみを正としつつ、祖先・子孫の関係は動的に計算するほうが使いやすい。
 */
export const useFlattenArrayTree = (listRef: React.MutableRefObject<PageState['aggregates']>) => {

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

  return {
    /** 直近の親を取得 */
    getParent,
    /** 祖先を列挙。より階層が浅い方が先。 */
    getAncestors,
    /** 直近の子を列挙 */
    getChildren,
    /** 子孫を列挙 */
    getDescendants,
  }
}

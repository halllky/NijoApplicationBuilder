
export type TreeNode<T> = {
  item: T
  children: TreeNode<T>[]
  parent?: TreeNode<T>
  depth: number
}

type ToTreeArgs<T>
  = { getId: (item: T) => string, getParent: (item: T) => string | null | undefined, getChildren?: undefined }
  | { getId: (item: T) => string, getParent?: undefined, getChildren: (item: T) => T[] | null | undefined }
export const toTree = <T,>(items: T[], fn: ToTreeArgs<T>): TreeNode<T>[] => {
  const treeNodes = new Map<string, TreeNode<T>>(items
    .map(item => [
      fn.getId(item),
      { item, children: [], depth: -1 }
    ]))
  // 親子マッピング
  if (fn.getParent) {
    for (const node of treeNodes) {
      const parentId = fn.getParent(node[1].item)
      if (parentId == null) continue
      const parent = treeNodes.get(parentId)
      node[1].parent = parent
      parent?.children.push(node[1])
    }
    for (const node of treeNodes) {
      node[1].depth = getDepth(node[1])
    }
  } else {
    const createChildrenRecursively = (parent: TreeNode<T>): void => {
      const childrenItems = fn.getChildren(parent.item) ?? []
      for (const childItem of childrenItems) {
        const childNode: TreeNode<T> = {
          item: childItem,
          depth: parent.depth + 1,
          parent,
          children: [],
        }
        parent.children.push(childNode)
        createChildrenRecursively(childNode)
      }
    }
    for (const node of treeNodes) {
      node[1].depth = 0
      createChildrenRecursively(node[1])
    }
  }
  // ルートのみ返す
  return Array
    .from(treeNodes.values())
    .filter(node => node.depth === 0)
}

export const getAncestors = <T,>(node: TreeNode<T>): TreeNode<T>[] => {
  const arr: TreeNode<T>[] = []
  let parent = node.parent
  while (parent) {
    arr.push(parent)
    parent = parent.parent
  }
  return arr.reverse()
}
export const flatten = <T,>(nodes: TreeNode<T>[]): TreeNode<T>[] => {
  return nodes.flatMap(node => getDescendantsAndSelf(node))
}
export const getDescendantsAndSelf = <T,>(node: TreeNode<T>): TreeNode<T>[] => {
  return [node, ...getDescendants(node)]
}
export const getDescendants = <T,>(node: TreeNode<T>): TreeNode<T>[] => {
  const arr: TreeNode<T>[] = []
  const pushRecursively = (n: TreeNode<T>): void => {
    for (const child of n.children) {
      arr.push(child)
      pushRecursively(child)
    }
  }
  pushRecursively(node)
  return arr
}
export const getDepth = <T,>(node: TreeNode<T>): number => {
  return getAncestors(node).length
}

/**
 * ツリー構造をもつオブジェクトに対して再帰的に処理を行います。
 */
export const visitObject = (obj: object, callback: (obj: object) => void): void => {
  if (obj == null) return
  if (typeof obj !== 'object') return

  if (Array.isArray(obj)) {
    for (const item of obj) {
      callback(item)
      visitObject(item, callback)
    }
    return
  }

  callback(obj)

  for (const key in obj) {
    if (!Object.prototype.hasOwnProperty.call(obj, key)) continue
    const prop = obj[key as keyof typeof obj]
    visitObject(prop as object, callback)
  }
}

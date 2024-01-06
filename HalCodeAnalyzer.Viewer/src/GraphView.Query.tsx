import cytoscape from 'cytoscape'
import * as UUID from 'uuid'
import { StorageUtil } from './util'

export type Query = {
  queryId: string
  name: string
  queryString: string
  nodePositions: { [nodeId: string]: cytoscape.Position }
  collapsedNodes: string[]
}
export const createNewQuery = (): Query => ({
  queryId: UUID.v4(),
  name: '',
  queryString: '',
  nodePositions: {},
  collapsedNodes: [],
})

export const useQueryRepository = () => {
  const { data, save } = StorageUtil.useLocalStorage<Query[]>(() => ({
    storageKey: 'HALDIAGRAM::QUERIES',
    defaultValue: () => [],
    serialize: obj => {
      return JSON.stringify(obj)
    },
    deserialize: str => {
      try {
        const parsed: Partial<Query>[] = JSON.parse(str)
        if (!Array.isArray(parsed)) return { ok: false }
        const obj = parsed.map<Query>(item => ({
          queryId: item.queryId ?? '',
          name: item.name ?? '',
          queryString: item.queryString ?? '',
          nodePositions: item.nodePositions ?? {},
          collapsedNodes: item.collapsedNodes ?? [],
        }))
        return { ok: true, obj }
      } catch (error) {
        console.error(`Failure to load application settings.`, error)
        return { ok: false }
      }
    },
  }))
  return { storedQueries: data, saveQueries: save }
}

const getViewState = (beforeState: Query, cy: cytoscape.Core): Query => {
  return {
    ...beforeState,
    nodePositions: cy.nodes().reduce((map, node) => {
      const pos = node.position()
      map[node.id()] = {
        x: Math.trunc(pos.x * 10000) / 10000,
        y: Math.trunc(pos.y * 10000) / 10000,
      }
      return map
    }, { ...beforeState.nodePositions }),
  }
}
const restoreViewState = (viewState: Query, cy: cytoscape.Core) => {
  for (const node of cy.nodes()) {

    // 子要素をもつノードの位置は子要素の位置が決まると自動的に決まるのであえて設定しない
    if (node.isParent()) continue

    const pos = viewState.nodePositions[node.id()]
    if (pos) node.position(pos)
  }
}

export default {
  getViewState,
  restoreViewState,
}

import { EditingProject } from "../backend"

/**
 * データ構造タブが扱うルート集約（dataStructures・commands のいずれか）の位置。
 * ルート集約はモデル種別ごとに別配列に分かれているため、単純な数値インデックスだけでは位置を特定できない。
 */
export type RootAggregateLocation = {
  list: 'dataStructures' | 'commands'
  index: number
}

/**
 * ルート集約自身、またはその直属メンバーのUniqueIdから、そのルート集約の位置を検索する。
 * dataStructures → commands の順に探索する。
 */
export const findRootAggregateLocation = (project: EditingProject, uniqueId: string): RootAggregateLocation | undefined => {
  for (const list of ['dataStructures', 'commands'] as const) {
    const index = project[list].findIndex(root =>
      root.uniqueId === uniqueId || root.members.some(member => member.uniqueId === uniqueId))
    if (index !== -1) return { list, index }
  }
  return undefined
}

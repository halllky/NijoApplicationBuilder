import { EditingMember, EditingRootAggregate } from "../backend";
import { asTree } from "../asTree";

/**
 * ref-to種別のメンバーが参照している先の集約・メンバーを検索する。
 * @param refFrom 参照元メンバー
 * @param dataStructures 検索対象のデータ構造のルート集約一覧（ref-toはデータ構造のみを参照できる）
 * @returns 見つかった場合はその参照先と、参照先が属するルート集約
 */
export const findRefToTarget = (
  refFrom: EditingMember,
  dataStructures: EditingRootAggregate[]
): { refTo: EditingRootAggregate | EditingMember, refToRoot: EditingRootAggregate } | undefined => {
  if (refFrom.type.kind !== 'ref-to') return undefined

  const [rootAggregateName, ...descendantNames] = refFrom.type.refToPath

  // ルート集約を探す
  const rootAggregate = dataStructures.find(root => root.physicalName === rootAggregateName)
  if (!rootAggregate) return undefined

  // 子孫を探す
  const tree = asTree(rootAggregate.members, m => m.uniqueId)
  const findRecursively = (remaining: string[], candidatesOwner: EditingRootAggregate | EditingMember): EditingRootAggregate | EditingMember | undefined => {
    if (remaining.length === 0) return candidatesOwner

    const [currentSegment, ...rest] = remaining
    const candidates = 'indent' in candidatesOwner ? tree.getChildren(candidatesOwner) : rootAggregate.members.filter(m => m.indent === 1)
    const found = candidates.find(candidate => candidate.physicalName === currentSegment)
    if (!found) return undefined
    return findRecursively(rest, found)
  }

  const refTo = findRecursively(descendantNames, rootAggregate)
  return refTo ? { refTo, refToRoot: rootAggregate } : undefined
};

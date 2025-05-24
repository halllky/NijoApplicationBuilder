import { XmlElementItem, SchemaDefinitionGlobalState, asTree, ATTR_TYPE } from "./types";

/**
 * 'ref-to' 属性の値を解析し、ターゲットとなる XmlElementItem を検索する。
 * @param refToValue 'ref-to:PathSegment1/PathSegment2/...' 形式の文字列。
 * @param allElements 検索対象のツリーの配列。
 * @returns 見つかった場合は XmlElementItem、見つからない場合は undefined。
 */
export const findRefToTarget = (
  refFrom: XmlElementItem,
  allElements: SchemaDefinitionGlobalState['xmlElementTrees']
): { refTo: XmlElementItem, refToRoot: XmlElementItem } | undefined => {
  const refToValue = refFrom.attributes[ATTR_TYPE];
  if (!refToValue || !refToValue.startsWith('ref-to:')) return undefined

  const pathString = refToValue.substring('ref-to:'.length);

  const pathSegments = pathString.split('/');
  const [rootAggregateName, ...descendantNames] = pathSegments

  // ルート集約を探す
  const rootAggregateGroup = allElements.find(element => element.xmlElements[0]?.localName === rootAggregateName)
  if (!rootAggregateGroup) return undefined

  // 子孫を探す
  const tree = asTree(rootAggregateGroup.xmlElements)
  const findRecursively = (remaining: string[], candidatesOwner: XmlElementItem) => {
    if (remaining.length === 0) return candidatesOwner

    const [currentSegment, ...rest] = remaining
    const candidates = tree.getChildren(candidatesOwner)
    const found = candidates.find(candidate => candidate.localName === currentSegment)
    if (!found) return undefined
    return findRecursively(rest, found)
  }

  const rootAggregate = rootAggregateGroup.xmlElements[0]
  const refTo = findRecursively(descendantNames, rootAggregate)
  return refTo ? { refTo, refToRoot: rootAggregate } : undefined
};

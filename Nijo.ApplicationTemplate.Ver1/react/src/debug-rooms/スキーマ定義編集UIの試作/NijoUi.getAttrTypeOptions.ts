import { ATTR_TYPE, TYPE_VALUE_OBJECT_MODEL, XmlElementItem, TYPE_STATIC_ENUM_MODEL, TYPE_COMMAND_MODEL, TYPE_CHILD, TYPE_CHILDREN, TYPE_DATA_MODEL, TYPE_QUERY_MODEL, XmlElementSelectAttributeGetOptionFunction, XmlElementSelectAttributeOption, asTree } from "./types"

// Typeの選択肢
export const getAttrTypeOptions: XmlElementSelectAttributeGetOptionFunction = currentState => {

  // 基本構造体
  const aggregates: XmlElementSelectAttributeOption[] = [
    { id: TYPE_CHILD, displayName: 'Child' },
    { id: TYPE_CHILDREN, displayName: 'Children' },
  ]

  // モデルの種類
  const modelTypes: XmlElementSelectAttributeOption[] = [
    { id: TYPE_DATA_MODEL, displayName: 'データモデル' },
    { id: TYPE_QUERY_MODEL, displayName: 'クエリモデル' },
    { id: TYPE_COMMAND_MODEL, displayName: 'コマンドモデル' },
    { id: TYPE_STATIC_ENUM_MODEL, displayName: '静的列挙型' },
    { id: TYPE_VALUE_OBJECT_MODEL, displayName: '値オブジェクト' },
  ]

  // 値メンバーの種類
  const valueMemberTypes: XmlElementSelectAttributeOption[] = [
    ...currentState.valueMemberTypes.map(v => ({ id: v.schemaTypeName, displayName: v.typeDisplayName })),
  ]

  // 列挙体 or 値オブジェクト
  // ※ enum, value-object が定義されているXML要素を列挙体として扱う
  // ※ 物理名が指定されていない場合は選択肢として表示しない
  const enumOrValueObjectTypes: XmlElementSelectAttributeOption[] = currentState
    .xmlElementTrees
    .flatMap(m => m.xmlElements)
    .filter(el => {
      if (!el.localName) return false
      const type = el.attributes?.get(ATTR_TYPE)
      if (type === TYPE_STATIC_ENUM_MODEL) return true
      if (type === TYPE_VALUE_OBJECT_MODEL) return true
      return false
    })
    .map(el => ({
      id: el.attributes?.get(ATTR_TYPE) === TYPE_STATIC_ENUM_MODEL
        ? `enum:${el.localName}`
        : `value-object:${el.localName}`,
      displayName: el.localName!,
    }))

  // 外部参照（ref-to）
  const refToTypes: XmlElementSelectAttributeOption[] = []
  for (const flattenTree of currentState.xmlElementTrees) {

    // DataModel, QueryModel のツリー内の要素のみが外部参照可能とざっくりみなす。
    // ※ 厳密にその集約を参照可能か否かは保存時にサーバー側で判定する
    const rootAggregate = flattenTree.xmlElements[0]
    if (rootAggregate.attributes?.get(ATTR_TYPE) !== TYPE_DATA_MODEL
      && rootAggregate.attributes?.get(ATTR_TYPE) !== TYPE_QUERY_MODEL) {
      continue
    }

    // ルート集約、child, children が参照可能
    // ※ 物理名が指定されていない場合は選択肢として表示しない
    const candidateTypes: XmlElementItem[] = []
    if (rootAggregate.localName) candidateTypes.push(rootAggregate)

    for (const element of flattenTree.xmlElements.slice(1)) {
      const type = element.attributes?.get(ATTR_TYPE)
      if (type !== TYPE_CHILD && type !== TYPE_CHILDREN) continue
      if (!element.localName) continue
      candidateTypes.push(element)
    }

    // ref-to:... に変換する。
    // 参照先が子孫集約の場合は "ref-to:aaa/bbb/ccc" のようにルート集約からのパスを含める。
    const tree = asTree(flattenTree.xmlElements)
    for (const candidate of candidateTypes) {
      refToTypes.push({
        id: `ref-to:${tree.getAncestors(candidate).map(x => x.localName).join('/')}`,
        displayName: candidate.localName!,
      })
    }
  }

  return [
    ...aggregates,
    ...modelTypes,
    ...valueMemberTypes,
    ...enumOrValueObjectTypes,
    ...refToTypes,
  ]
}

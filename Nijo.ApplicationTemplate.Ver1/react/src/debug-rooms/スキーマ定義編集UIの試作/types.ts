
export type ApplicationState = {
  /** XML要素をルート集約ごとの塊に分類したもの。 */
  xmlElementTrees: ModelPageForm[]
  /** XML要素の属性定義。 */
  attributeDefs: XmlElementAttribute[]
  /** 値メンバーの種類定義。 */
  valueMemberTypes: ValueMemberType[]
}


/** Model定義画面のデータ型定義 */
export type ModelPageForm = {
  /**
   * そのXMLツリーの要素の一覧。
   * 以下は保証されているものとする。
   *
   * - 配列の要素数が1以上であること
   * - 先頭の要素がインデント0、以降の要素がインデント1以上であること
   * - インデントが0の要素のTypeは、モデルを表す種類であること
   * - インデントが1以上の要素のTypeは、モデルを表す種類以外の種類であること
   */
  xmlElements: XmlElementItem[]
}

/** Nijo.csproj の SchemaParseRule の ValueMemberType に同じ。 */
export type ValueMemberType = {
  /** XMLスキーマ定義でこの型を指定するときの型名 */
  schemaTypeName: string
  /** この種類の画面表示上名称 */
  typeDisplayName: string
}

// ---------------------------------

/** XML要素1個分と対応するデータ型 */
export type XmlElementItem = {
  /** XML要素とは関係ないUIの都合上のID。画面表示時に発番する。 */
  id: string
  /** インデントレベル。XML要素の親子関係は保存時にインデントをもとに再構築する。 */
  indent: number
  /** XML要素のローカル名 */
  localName?: string
  /** XML要素の値 */
  value?: string
  /** XML要素の属性 */
  attributes?: Map<XmlElementAttributeName, string>
}

/** XML要素の属性の識別子 */
export type XmlElementAttributeName = string & { _brand: 'XmlElementAttributeName' }

/** XML要素の属性の種類定義 */
export type XmlElementAttribute = {
  /** この属性の識別子。XML要素の属性名になる。 */
  attributeName: XmlElementAttributeName
} & (XmlElementStringAttribute | XmlElementBoolAttribute | XmlElementSelectAttribute)

/** XML要素の属性の種類定義（文字列属性） */
export type XmlElementStringAttribute = {
  type: 'string'
}
/** XML要素の属性の種類定義（ブール属性） */
export type XmlElementBoolAttribute = {
  type: 'bool'
}
/** XML要素の属性の種類定義（選択属性）  */
export type XmlElementSelectAttribute = {
  type: 'select'
  /** 選択肢を取得する関数。 */
  getOptions: XmlElementSelectAttributeGetOptionFunction
}

export type XmlElementSelectAttributeGetOptionFunction = (state: ApplicationState) => XmlElementSelectAttributeOption[]
export type XmlElementSelectAttributeOption = { id: string, displayName: string }

export const ATTR_TYPE = 'Type' as XmlElementAttributeName
export const TYPE_DATA_MODEL = 'data-model'
export const TYPE_QUERY_MODEL = 'query-model'
export const TYPE_COMMAND_MODEL = 'command-model'
export const TYPE_STATIC_ENUM_MODEL = 'enum'
export const TYPE_VALUE_OBJECT_MODEL = 'value-object'
export const TYPE_CHILD = 'child'
export const TYPE_CHILDREN = 'children'



/**
 * 型つきドキュメント。
 * 基本的には単なるアウトライナーで、横軸方向に任意の属性項目を追加できるもの。
 * 属性項目の定義とデータの内容の両方を持つ。
 */
export type TypedOutliner = {
  /** アウトライナーの型ID */
  typeId: string
  /** アウトライナーの型名 */
  typeName: string
  /** この種類のデータそれぞれに指定できる属性の定義 */
  attributes: OutlinerAttribute[]
  /** データ */
  items: OutlinerItem[]
}

/** アウトライナーのデータ1件につけることができる属性の定義 */
export type OutlinerAttribute = {
  /** 属性の型ID */
  attributeId: string
  /** 属性の型名 */
  attributeName: string
}

/** アウトライナーのデータ1件 */
export type OutlinerItem = {
  /** アイテムの型ID */
  itemId: string
  /** アイテムの型名 */
  itemName: string
  /** アイテムのインデント */
  indent: number
  /** アイテムの属性 */
  attributes: OutlinerItemAttributeValues
}

/** アウトライナーのデータ1件の属性の値 */
export type OutlinerItemAttributeValues = {
  [attributeId: string]: string
}

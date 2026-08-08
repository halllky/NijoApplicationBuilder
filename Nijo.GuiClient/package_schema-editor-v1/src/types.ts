import { GraphViewProps, Node, Edge } from "@nijo/ui-components/layout/GraphView2"
import {
  ModelPageForm,
  XmlElementAttribute,
  ValueMemberType,
  ProjectOptions,
  ProjectOptionPropertyInfo,
  NijoXmlCustomAttribute,
  GenericLookupTableCategoriesData,
  PreviewSetting,
} from "./types.nijoXml"

export * from "./types.nijoXml"

/**
 * ER図とスキーマ定義グラフのデータセット。
 * 自動生成後のアプリケーションのデバッグメニューで永続化された状態が参照される。
 */
export type AppSchemaDefinitionGraphDataSet = {
  schemaDefinition: {
    nodes: { [id: string]: Node }
    edges: Edge[]
    nodePositions: GraphViewProps['defaultNodePositions']
  }
} & {
  /** 未使用項目 */
  erDiagram?: never
  /** 未使用項目 */
  displayMode?: never
  /** 未使用項目 */
  onlyRoot?: never
}

/**
 * スキーマ定義編集におけるアプリケーション全体の状態。
 * データの持ち方こそ違うがデータの範囲は nijo.xml 1個分と対応する。
 */
export type ApplicationState = {
  /** XML要素をルート集約ごとの塊に分類したもの。 */
  xmlElementTrees: ModelPageForm[]
  /** XML要素の属性定義。 */
  attributeDefs: XmlElementAttribute[]
  /** 値メンバーの種類定義。 */
  valueMemberTypes: ValueMemberType[]
  /** プロジェクト設定の現在値 */
  projectOptions: ProjectOptions
  /** プロジェクト設定項目のメタ情報 */
  projectOptionPropertyInfos: ProjectOptionPropertyInfo[]
  /** カスタム属性定義 */
  customAttributes: NijoXmlCustomAttribute[]
  /** 汎用参照テーブルのカテゴリ定義 */
  genericLookupTableCategories: GenericLookupTableCategoriesData[]
  /** グラフのViewState */
  schemaGraphViewState?: AppSchemaDefinitionGraphDataSet | null
  /** 生成後アプリのデバッグ起動設定（nijo.preview.jsonの内容） */
  previewSetting: PreviewSetting
}

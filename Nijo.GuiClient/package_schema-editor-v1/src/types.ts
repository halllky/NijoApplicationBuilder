import { GraphViewProps, Node, Edge } from "@nijo/ui-components/layout/GraphView2"
import {
  RootAggregateXmlTree,
  XmlAttributeDef,
  ValueMemberType,
  ProjectOptions,
  ProjectOptionPropertyInfo,
  NijoXmlCustomAttribute,
  GenericLookupTableCategories,
  PreviewSetting,
} from "./types.nijoXml"

export * from "./types.nijoXml"

/**
 * スキーマ定義グラフの見た目の状態（nijo.viewState.jsonの内容）。
 * SchemaGraphViewState.cs に対応する。
 */
export type SchemaGraphViewState = {
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
 * スキーマ定義編集画面が扱う、nijo.xml とその同階層に置かれる固定名ファイル群
 * （nijo.viewState.json, nijo.preview.json）の内容の組。
 * GeneratedProjectInGui.cs に対応する。
 */
export type GeneratedProjectInGui = {
  /** XML要素をルート集約ごとの塊に分類したもの。 */
  xmlElementTrees: RootAggregateXmlTree[]
  /** XML要素の属性定義。 */
  attributeDefs: XmlAttributeDef[]
  /** 値メンバーの種類定義。 */
  valueMemberTypes: ValueMemberType[]
  /** プロジェクト設定の現在値 */
  projectOptions: ProjectOptions
  /** プロジェクト設定項目のメタ情報 */
  projectOptionPropertyInfos: ProjectOptionPropertyInfo[]
  /** カスタム属性定義 */
  customAttributes: NijoXmlCustomAttribute[]
  /** 汎用参照テーブルのカテゴリ定義 */
  genericLookupTableCategories: GenericLookupTableCategories[]
  /** グラフのViewState（nijo.viewState.jsonの内容） */
  schemaGraphViewState?: SchemaGraphViewState | null
  /** 生成後アプリのデバッグ起動設定（nijo.preview.jsonの内容） */
  previewSetting: PreviewSetting
}

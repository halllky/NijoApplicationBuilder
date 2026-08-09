import { GraphViewProps, Node, Edge } from "@nijo/ui-components/layout/GraphView2"

/**
 * サーバーとやり取りする、編集中のプロジェクト全体のデータ。
 * GET /nijo-api/load のレスポンス、POST /nijo-api/save 等のリクエストボディとして使う。
 * C#側の EditingProject.cs に対応する。
 * nijo.xml の書式（属性値の区切り文字・"True"文字列など）はサーバー側で吸収済みであり、
 * この型はNijoのスキーマ概念（モデル種別・メンバー種別など）だけを表す。
 */
export type EditingProject = {
  dataStructures: EditingRootAggregate[]
  commands: EditingRootAggregate[]
  staticEnums: EditingRootAggregate[]
  valueObjects: EditingRootAggregate[]
  constants: EditingRootAggregate[]
  customAttributes: EditingCustomAttribute[]
  /** プロジェクト設定の現在値 */
  projectOptions: Record<string, EditingAttributeValue>
  /** スキーマ定義グラフの見た目の状態（nijo.viewState.jsonの内容）。nullの場合は保存をスキップする。 */
  graphViewState?: EditingSchemaGraphViewState | null
  /** 生成後アプリのデバッグ起動設定（nijo.preview.jsonの内容） */
  previewSetting: EditingPreviewSetting
}

/** 属性値・プロジェクト設定値としてやり取りされる値の型。 */
export type EditingAttributeValue = string | number | boolean | null

/**
 * ルート集約1個分。データ構造・コマンド・静的区分・値オブジェクト・定数のいずれも、
 * この形（ルート自身の情報＋子孫要素の並び）で表される。
 */
export type EditingRootAggregate = {
  uniqueId: string
  /** XML要素のローカル名。 */
  physicalName?: string | null
  comment?: string | null
  /** ルート集約の種類（data-model, query-model, enum など）。値は {@link EditingProject} と一緒に返る SchemaEditorRule.models で列挙される。 */
  model?: string | null
  /** Type・UniqueId・UniqueConstraints を除いた属性。 */
  attributes: Record<string, EditingAttributeValue>
  uniqueConstraints: EditingUniqueConstraint[]
  /** IsGenericLookupTable が指定されたルート集約のみ値を持つ。 */
  genericLookupTable?: EditingGenericLookupTable | null
  /** 子孫要素。indent は1以上。 */
  members: EditingMember[]
}

/**
 * 集約の子孫要素（Child, Children, ValueMember, Ref）1個分。
 * 同じルート集約に属するメンバー同士の親子関係は indent によって表す
 * （グリッドでの行挿入・移動・インデント操作とそのまま噛み合うよう、あえてフラット配列のまま扱う）。
 */
export type EditingMember = {
  uniqueId: string
  /** 親子関係を表すインデントレベル。1以上。 */
  indent: number
  physicalName?: string | null
  /** XML要素のテキスト値。 */
  value?: string | null
  comment?: string | null
  type: EditingMemberType
  /** Type・UniqueId・UniqueConstraints を除いた属性。 */
  attributes: Record<string, EditingAttributeValue>
  /** child・children のみ意味を持つ。 */
  uniqueConstraints: EditingUniqueConstraint[]
}

/**
 * メンバーの種類。nijo.xml の Type 属性の書式（"child" | "children" | "ref-to:A/B" | 値の種類名）を
 * 判別可能ユニオンに変換したもの。
 */
export type EditingMemberType =
  | { kind: 'child' }
  | { kind: 'children' }
  | { kind: 'ref-to'; refToPath: string[] }
  | { kind: 'value'; valueTypeName: string }
  | { kind: 'unknown'; rawValue?: string | null }

/** ユニーク制約1件分（対象メンバーの UniqueId の組み合わせ）。 */
export type EditingUniqueConstraint = {
  memberUniqueIds: string[]
}

/** 汎用参照テーブル1個分のカテゴリ定義データ。IsGenericLookupTable が指定されたルート集約が所有する。 */
export type EditingGenericLookupTable = {
  categories: EditingGenericLookupTableCategory[]
}

/** 汎用参照テーブルの1カテゴリ分のデータ */
export type EditingGenericLookupTableCategory = {
  /** カテゴリ名（XML要素名）例: "Countries" */
  name: string
  /** 表示用名称 例: "国・地域区分" */
  displayName: string
  /** ハードコードされるキーの値: メンバーの UniqueId → Value のマッピング */
  hardCodedKeyValues: Record<string, string>
}

/** カスタム属性定義。 */
export type EditingCustomAttribute = {
  uniqueId?: string | null
  physicalName?: string | null
  displayName?: string | null
  comment?: string | null
  isValidation: boolean
  availableModels: string[]
  type?: 'Boolean' | 'Decimal' | 'Enum' | 'String' | null
  enumValues: string[]
}

// ---------------------------------

/**
 * スキーマ定義グラフの見た目の状態（nijo.viewState.jsonの内容）。
 * C#側の SchemaEditor.SchemaGraphViewState に対応する。
 */
export type EditingSchemaGraphViewState = {
  schemaDefinition: {
    nodes: { [id: string]: Node }
    edges: Edge[]
    nodePositions: GraphViewProps['defaultNodePositions']
  }
}

/** nijo.preview.json の内容。生成後アプリをデバッグ起動するための設定。 */
export type EditingPreviewSetting = {
  /** 起動完了時にこのURLをブラウザで開く。空文字なら開かない */
  browser: string
  /** true なら nijo serve の起動と同時にプロセス群を自動起動する */
  startOnNijoServe: boolean
  /** 並列実行するプロセスの定義 */
  concurrently: EditingPreviewProcessSetting[]
}

/** concurrently で並列起動する1プロセスの設定 */
export type EditingPreviewProcessSetting = {
  /** プロセスの識別名。GUI上の表示やログ取得のキーに使う */
  name: string
  process: {
    /** nijo.xml のディレクトリからの相対パス */
    cwd: string
    filename: string
    args: string
  }
  log: {
    /** 標準出力の書き出し先。nijo.xml のディレクトリからの相対パス。空文字ならログファイルを作らない */
    stdout: string
    /** 標準エラー出力の書き出し先。nijo.xml のディレクトリからの相対パス。空文字ならログファイルを作らない */
    stderr: string
    /** true=起動の度に追記 / false=起動の度にクリア */
    appendStdout: boolean
    /** true=起動の度に追記 / false=起動の度にクリア */
    appendStderr: boolean
  }
  /** コード再生成が成功した直後にこのプロセスを再起動するか */
  restartOnGenerateCode: boolean
}

/** 稼働中の1プロセスの状態（/nijo-api/preview/state のレスポンス） */
export type PreviewProcessState = {
  name: string
  isRunning: boolean
  processId: number | null
  exitCode: number | null
  stdout: PreviewLogIncrement
  stderr: PreviewLogIncrement
}

/** ログファイルの指定オフセット以降の増分 */
export type PreviewLogIncrement = {
  text: string
  offset: number
}

// ---------------------------------

/**
 * スキーマ編集画面が必要とする、プロジェクトに依存しない固定ルール。
 * GET /nijo-api/schema-rule のレスポンス。編集対象ではないため {@link EditingProject} とは別に取得する。
 */
export type SchemaEditorRule = {
  models: ModelDef[]
  attributeDefs: AttributeDef[]
  valueMemberTypes: ValueMemberTypeDef[]
  projectOptionPropertyInfos: ProjectOptionPropertyInfo[]
}

/** ルート集約の種類（data-model, query-model, enum など）。 */
export type ModelDef = {
  schemaName: string
}

/** 値メンバーの種類のスキーマ定義編集画面GUI上でのデータ構造。 */
export type ValueMemberTypeDef = {
  schemaTypeName: string
  typeDisplayName: string
}

/** nijo.xml のXML要素1個に定義できる属性の型定義。 */
export type AttributeDef = {
  /** この属性の識別子。XML要素の属性名になる。 */
  attributeName: string
  /** この属性の画面表示上の名称。 */
  displayName: string
  /** この属性が使用可能なモデルとノード種別の組み合わせの配列。 */
  availableElements: { model: string, nodeType: string }[]
  type: 'Boolean' | 'String' | 'Integer' | 'EnumSelect'
  /** typeが"EnumSelect"の場合のみ使用される、選択肢の候補。 */
  typeEnumValues?: string[] | null
}

/** プロジェクト設定項目のメタ情報 */
export type ProjectOptionPropertyInfo = {
  propertyName: string
  description: string
  propertyType: 'string' | 'bool' | 'int'
  defaultValue?: EditingAttributeValue
}

// ---------------------------------

/**
 * サーバーから返ってくる検証結果。XML要素のUniqueIdをキーにして引ける。
 * POST /nijo-api/validate がエラーありの場合（ステータスコード202）のレスポンス。
 */
export type ValidationErrorMap = {
  [xmlElementUniqueId: string]: {
    /** この要素自体に対するエラー */
    _own: string[]
    /** この要素の属性に対するエラー */
    [attributeName: string]: string[]
  }
}

// ---------------------------------
// 属性名の定数。
//
// EditingRootAggregate.attributes / EditingMember.attributes は
// 汎用的な Record<string, EditingAttributeValue> のままであり、
// 個別の属性は構造化されていない（Type・UniqueId・UniqueConstraints のみサーバー側で構造化済み）。
// また ValidationErrorMap の属性エラーキーは常にXML属性名の文字列である。
// そのため、以下の属性名は「XML書式の知識がクライアントに漏れる」ものではなく、
// このワイヤ契約を扱うために必要な最小限の定数である。

export const ATTR_TYPE = 'Type'
export const ATTR_UNIQUE_CONSTRAINTS = 'UniqueConstraints'
export const ATTR_DISPLAY_NAME = 'DisplayName'
export const ATTR_PARAMETER = 'Parameter'
export const ATTR_RETURN_VALUE = 'ReturnValue'
export const ATTR_CONSTANT_TYPE = 'ConstantType'
export const ATTR_CONSTANT_VALUE = 'ConstantValue'
export const ATTR_IS_GENERIC_LOOKUP_TABLE = 'IsGenericLookupTable'
export const ATTR_IS_HARD_CODED_PRIMARY_KEY = 'IsHardCodedPrimaryKey'

// 定数の種類（ConstantType属性の値）
export const CONSTANT_TYPE_CHILD = 'child'
export const CONSTANT_TYPE_STRING = 'string'
export const CONSTANT_TYPE_INT = 'int'
export const CONSTANT_TYPE_DECIMAL = 'decimal'
export const CONSTANT_TYPE_TEMPLATE = 'template'

// ルート集約のモデル種別（EditingRootAggregate.model の値）
export const MODEL_DATA = 'data-model'
export const MODEL_QUERY = 'query-model'
export const MODEL_COMMAND = 'command-model'
export const MODEL_STRUCTURE = 'structure-model'
export const MODEL_STATIC_ENUM = 'enum'
export const MODEL_VALUE_OBJECT = 'value-object'
export const MODEL_CONSTANT = 'constant-model'

// ノード種別（C#のE_NodeTypeに対応。AttributeDef.availableElements[].nodeType の値）
export const NODE_TYPE_ROOT_AGGREGATE = 'RootAggregate'
export const NODE_TYPE_CHILD_AGGREGATE = 'ChildAggregate'
export const NODE_TYPE_CHILDREN_AGGREGATE = 'ChildrenAggregate'
export const NODE_TYPE_VALUE_MEMBER = 'ValueMember'
export const NODE_TYPE_REF = 'Ref'
export const NODE_TYPE_STATIC_ENUM_VALUE = 'StaticEnumValue'
export const NODE_TYPE_UNKNOWN = 'Unknown'

/**
 * 指定された属性が、指定されたモデル種別・ノード種別の組み合わせで利用可能かを判定する。
 */
export const isAttributeAvailable = (attr: AttributeDef, modelType: string, nodeTypes: string[]): boolean => {
  if (!modelType) return false
  return attr.availableElements.some(ae => ae.model === modelType && nodeTypes.includes(ae.nodeType))
}

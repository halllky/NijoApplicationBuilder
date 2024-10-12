
/** ページ全体の状態 */
export type PageState = {
  /** プロジェクトのルートディレクトリ */
  projectRoot: string | undefined
  /** 編集対象XMLファイル名 */
  editingXmlFilePath: string | undefined
  /** 集約など */
  aggregates: AggregateOrMember[] | undefined
  /** 集約やメンバーの型定義 */
  aggregateOrMemberTypes: AggregateOrMemberTypeDef[] | undefined
  /** オプショナル属性定義 */
  optionalAttributes: OptionalAttributeDef[] | undefined
}

/** 集約または集約メンバー */
export type AggregateOrMember = {
  /** 集約または集約メンバーの画面表示名 */
  displayName: string | undefined
  /** 集約または集約メンバーの型 */
  type: AggregateOrMemberTypeKey | undefined
  /** オプショナル属性の値 */
  attrValues: OptionalAttributeValue[] | undefined
  /** 直近の子要素。計算コストの都合で画面表示時と保存時のみ更新される想定 */
  children: AggregateOrMember[] | undefined
  /** 備考 */
  comment: string | undefined
}

/** 集約または集約メンバーのオプショナル属性の値 */
export type OptionalAttributeValue = {
  id: OptionalAttributeKey
  /** オプショナル属性の値 */
  value: string | undefined
}

/** 集約または集約メンバーの型定義。`read-model-2`, `ref-to:`, `word`, ... など */
export type AggregateOrMemberTypeDef = {
  id: AggregateOrMemberTypeKey
  /** 画面表示名。XMLのis属性の名称との変換はサーバー側で行う。 */
  displayName: string | undefined
  /** ref-to の場合はUIが特殊なので */
  isRefTo: boolean | undefined
  /** variation-item の場合は区分値を指定する必要があるので */
  isVariationItem: boolean | undefined
}

/** オプショナル属性定義。DbName, key, required, ... など */
export type OptionalAttributeDef = {
  id: OptionalAttributeKey
  /** 画面表示名。XMLのis属性の名称との変換はサーバー側で行う。 */
  displayName: string | undefined
  /** この属性の説明文 */
  helpText: string | undefined
  /** 種類 */
  type: 'string' | 'number' | 'boolean' | undefined
}


const s1: unique symbol = Symbol()
const s2: unique symbol = Symbol()
/** 型定義のキー */
export type AggregateOrMemberTypeKey = string & { [s1]: never }
/** オプショナル属性のキー */
export type OptionalAttributeKey = string & { [s2]: never }

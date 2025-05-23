import { SchemaDefinitionGlobalState, SchemaDefinitionOutletContextType } from "./スキーマ定義編集/types"

/** アプリケーション全体の状態 */
export type ApplicationState = {
} & SchemaDefinitionGlobalState

/** アプリケーションの画面のコンテキスト */
export type NijoUiOutletContextType = {
  /** 選択中のルート集約のID */
  selectedRootAggregateId: string | undefined
} & SchemaDefinitionOutletContextType
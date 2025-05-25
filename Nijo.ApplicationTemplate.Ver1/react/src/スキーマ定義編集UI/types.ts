import { SchemaDefinitionGlobalState as InternalSchemaDefinitionGlobalState, SchemaDefinitionOutletContextType } from "./スキーマ定義編集/types"
import { TypedOutlinerGlobalState as InternalTypedOutlinerGlobalState } from "./型つきアウトライナー/types"

// 再エクスポート
export type SchemaDefinitionGlobalState = InternalSchemaDefinitionGlobalState;
export type TypedOutlinerGlobalState = InternalTypedOutlinerGlobalState;

/** アプリケーション全体の状態 */
export type ApplicationState = {
} & SchemaDefinitionGlobalState
  & TypedOutlinerGlobalState

/** アプリケーションの画面のコンテキスト */
export type NijoUiOutletContextType = {
} & SchemaDefinitionOutletContextType

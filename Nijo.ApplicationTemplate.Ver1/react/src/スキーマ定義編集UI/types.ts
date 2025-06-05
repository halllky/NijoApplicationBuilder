import { SchemaDefinitionGlobalState as InternalSchemaDefinitionGlobalState, SchemaDefinitionOutletContextType } from "./スキーマ定義編集/types"
import { TypedDocumentContextType } from "./型つきドキュメント/types";

// 再エクスポート
export type SchemaDefinitionGlobalState = InternalSchemaDefinitionGlobalState;

/** アプリケーション全体の状態 */
export type ApplicationState = {
} & SchemaDefinitionGlobalState

/** アプリケーションの画面のコンテキスト */
export type NijoUiOutletContextType = {
  typedDoc: TypedDocumentContextType
} & SchemaDefinitionOutletContextType

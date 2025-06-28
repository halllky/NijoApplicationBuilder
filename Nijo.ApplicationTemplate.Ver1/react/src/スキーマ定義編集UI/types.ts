import { SchemaDefinitionGlobalState, SchemaDefinitionOutletContextType } from "./スキーマ定義編集/types"
import { TypedDocumentContextType } from "./型つきドキュメント/types";

// 再エクスポート
export type { SchemaDefinitionGlobalState }

/** アプリケーションの画面のコンテキスト */
export type NijoUiOutletContextType = {
  typedDoc: TypedDocumentContextType
} & SchemaDefinitionOutletContextType

// -----------------------------------

/** アプリケーション全体の設定 */
export type AppSettingsForDisplay = {
  /** アプリケーション名 */
  applicationName: string
  /** 型つきドキュメントの定義 */
  entityTypeList: {
    entityTypeId: string
    entityTypeName: string
  }[]
  /** データプレビューの定義 */
  dataPreviewList: {
    id: string
    title: string
  }[]
}

/** アプリケーション全体の設定。保存時にサーバー側に送られる */
export type AppSettingsForSave = {
  /** アプリケーション名 */
  applicationName: string
  /** トップページでのエンティティ型の表示順 */
  entityTypeOrder: string[]
}

/** ユーザー自身にだけ適用される設定 */
export type PersonalSettings = {
  /** グリッドの操作説明ボタンを非表示にする */
  hideGridButtons?: boolean
}
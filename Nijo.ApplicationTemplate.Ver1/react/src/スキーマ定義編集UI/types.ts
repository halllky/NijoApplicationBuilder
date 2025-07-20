import { ImperativePanelHandle } from "react-resizable-panels";
import { TypedDocumentContextType } from "./型つきドキュメント/types";

/** アプリケーションの画面のコンテキスト */
export type NijoUiOutletContextType = {
  typedDoc: TypedDocumentContextType
  sideMenuPanelRef: React.RefObject<ImperativePanelHandle | null>
}

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
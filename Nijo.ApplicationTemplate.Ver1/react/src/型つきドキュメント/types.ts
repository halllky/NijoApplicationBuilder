import { ViewState } from "../layout/GraphView/Cy"

/** アプリケーション全体の設定 */
export type AppSettingsForDisplay = {
  /** アプリケーション名 */
  applicationName: string
  /** エンティティ型の定義 */
  entityTypeList: {
    entityTypeId: string
    entityTypeName: string
  }[]
}

/** アプリケーション全体の設定。保存時にサーバー側に送られる */
export type AppSettingsForSave = {
  /** アプリケーション名 */
  applicationName: string
  /** トップページでのエンティティ型の表示順 */
  entityTypeOrder: string[]
}


// -----------------------------------

/** アウトライナーのデータ1件 */
export type Entity = {
  /** アイテムの型ID */
  entityId: string
  /** アイテムの型名 */
  entityName: string
  /** 種類 */
  typeId?: string
  /** アイテムのインデント */
  indent: number
  /** アイテムの属性 */
  attributeValues: EntityAttributeValues
  /** このエンティティに対するコメント */
  comments: TypedDocumentComment[]
}

/** アウトライナーのデータ1件の属性の値 */
export type EntityAttributeValues = {
  [attributeId: string]: string
}

/** アウトライナーのデータ1件につけることができる属性の定義 */
export type EntityAttribute = {
  /** 属性の型ID */
  attributeId: string
  /** 属性の型名 */
  attributeName: string
  /** 属性の型 */
  attributeType: 'word' | 'description' | 'select'
  /** 選択肢（属性型が select の場合のみ） */
  selectOptions?: string[]
}

// -----------------------------------

/** グラフ */
export type Perspective = {
  /** 型ID */
  perspectiveId: string
  /** 型名 */
  name: string
  nodes: PerspectiveNode[]
  edges: PerspectiveEdge[]
  /** この種類のデータそれぞれに指定できる属性の定義 */
  attributes: EntityAttribute[]
  /** 詳細欄における単語型の属性のラベルの横幅。CSSの値で指定（"10rem"など） */
  detailPageLabelWidth?: string
  /** グラフの表示状態（pan, zoom, ノード位置など） */
  viewState?: ViewState
  /** パネルのサイズの保存。具体的な値の形は react-resizable-panels の PanelGroupStorage の仕様に従う。 */
  resizablePaneState?: { [key: string]: string }
  /** グリッドの列幅の保存。具体的な値の形は EditableGrid の仕様に従う。 */
  gridStates?: { [key in 'root-grid']: string }
}

/** グラフのノード */
export type PerspectiveNode = Entity

/** グラフのエッジ */
export type PerspectiveEdge = {
  sourceNodeId: string
  targetNodeId: string
  label: string | undefined
  /** このエッジに対するコメント */
  comments: TypedDocumentComment[]
  /** 古い情報かどうか */
  outdated?: boolean
}

// -----------------------------------

/** コメント */
export type TypedDocumentComment = {
  /** コメントのID */
  commentId: string
  /** コメントの内容 */
  content: string
  /** コメントの作成者 */
  author: string
  /** コメントの作成日時 */
  createdAt: string
  /** 古い情報かどうか */
  outdated?: boolean
}

// -----------------------------------

/** アプリケーション全体のコンテキスト情報。各画面が使用する機能。永続化層とのやり取りを行う。 */
export type TypedDocumentContextType = {
  /** コンテキストが準備できているかどうか */
  isReady: boolean

  /** アプリケーション全体の設定を取得する */
  loadAppSettings: () => Promise<AppSettingsForDisplay>

  /** アプリケーション全体の設定を保存する */
  saveAppSettings: (settings: AppSettingsForSave) => Promise<boolean>

  /** グラフを作成する。永続化まで伴う */
  createPerspective: (perspective: Perspective) => Promise<Perspective>

  //#region グラフ画面

  /** グラフ画面初期表示処理 */
  loadPerspectivePageData: (perspectiveId: string) => Promise<PerspectivePageData | undefined>

  /** グラフ画面で編集したデータを永続化する */
  savePerspective: (data: PerspectivePageData) => Promise<boolean>

  //#endregion グラフ画面
}

/** ナビゲーションメニューの項目 */
export type NavigationMenuItem = {
  type: 'folder'
  label: string
  /** 子メニュー */
  children: NavigationMenuItem[]
} | {
  type: 'perspective'
  label: string
  /** エンティティ型IDまたはグラフID */
  id: string
}

/** グラフ画面で取り扱うデータ */
export type PerspectivePageData = {
  perspective: Perspective
}

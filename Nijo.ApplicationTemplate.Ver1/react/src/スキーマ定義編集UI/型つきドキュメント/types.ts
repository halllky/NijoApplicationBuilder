import { ViewState } from "../../layout/GraphView/Cy"

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
  /** 古い情報かどうか */
  outdated?: boolean
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
  attributeType: 'word' | 'description'
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
  resizablePaneState?: {
    [key: string]: string
  }
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

  /** ナビゲーションメニューを取得する */
  loadNavigationMenus: () => Promise<NavigationMenuItem[]>

  /** グラフを作成する。永続化まで伴う */
  createPerspective: (perspective: Perspective) => Promise<Perspective>

  //#region エンティティ型画面

  /** エンティティ型ページ初期表示処理 */
  loadEntityTypePageData: (entityTypeId: string) => Promise<PerspectivePageData | undefined>

  /** 型を削除する。この型を参照しているエンティティがある場合は削除できない */
  tryDeleteEntityType: (entityTypeId: string) => Promise<boolean>

  //#endregion エンティティ型画面

  //#region グラフ画面

  /** グラフ画面初期表示処理 */
  loadPerspectivePageData: (perspectiveId: string) => Promise<PerspectivePageData | undefined>

  /** グラフ画面で編集したデータを永続化する */
  savePerspective: (data: PerspectivePageData) => Promise<void>

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

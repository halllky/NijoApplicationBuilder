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
  /** グラフの表示状態（pan, zoom, ノード位置など） */
  viewState?: ViewState
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
  loadEntityTypePageData: (entityTypeId: string) => Promise<EntityTypePageData | undefined>

  /**
   * 画面上で編集したエンティティの値で永続化されているデータを上書きする。
   * 永続化されたデータのうち、引数のエンティティ型だが、引数の配列に含まれないものは削除される。
   * エンティティの順番は引数の配列のそれに従う。
   * 引数の配列のエンティティのうち、種類が変わったもの（型IDが一致しないものもの）は、当該変更先の型の一覧の末尾に移動する。
   */
  saveEntities: (data: EntityTypePageData) => Promise<void>

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

/** エンティティ型画面で取り扱うデータ */
export type EntityTypePageData = {
  entityType: Perspective
  entities: Entity[]
}

/** グラフ画面で取り扱うデータ */
export type PerspectivePageData = {
  perspective: Perspective
}

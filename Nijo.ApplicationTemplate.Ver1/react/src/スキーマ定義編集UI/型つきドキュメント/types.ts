import * as ReactResizablePanels from "react-resizable-panels"
import * as CytoscapeWrapper from "../../layout/GraphView/Cy"
import { MentionUtil } from "../UI"
import { AppSettingsForDisplay, AppSettingsForSave } from "../types"

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
  /** グリッドの列に表示しない */
  invisibleInGrid?: boolean
  /** 詳細欄に表示しない */
  invisibleInDetail?: boolean
}

// -----------------------------------

/** グラフ */
export type Perspective = {
  /** 型ID */
  perspectiveId: string
  /** 型名 */
  name: string
  nodes: PerspectiveNode[]
  /** この種類のデータそれぞれに指定できる属性の定義 */
  attributes: EntityAttribute[]
  /** 書式条件。先頭のものがより優先度が高い。 */
  formatConditions?: FormatCondition[]
  /** グリッドでエンティティ名を折り返して表示するか */
  wrapEntityName?: boolean
  /** 詳細欄における単語型の属性のラベルの横幅。CSSの値で指定（"10rem"など） */
  detailPageLabelWidth?: string
  /** グラフがグリッドに対して縦方向と横方向のどちらに表示されるか。 */
  graphViewPosition?: ReactResizablePanels.PanelGroupProps['direction']
  /** グラフの表示状態（pan, zoom, ノード位置など） */
  viewState?: CytoscapeWrapper.ViewState
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
// 書式条件

export type FormatCondition = {
  /** 条件式 */
  if: {
    /** 対象の属性ID */
    attributeId: string
    /** 比較演算子 */
    logic: 'equals' | 'includes' | 'notEquals' | 'notIncludes'
    /** 検索文字列 */
    search: string
  }
  /** 条件式が真の場合の書式 */
  then: {
    /** グリッドのテキスト色。tailwindcssのクラス名。 */
    gridRowTextColor?: string
    /** グラフのノードのスタイル。tailwindcssのクラス名は使えないので、HEXカラーコードを指定する。 */
    graphNodeColor?: string
    /** グラフのノードを非表示にする */
    invisibleInGraph?: boolean
  }
}

/** 書式条件のオプション */
export const AVAILABLEFORMAT = {
  /** グリッドのテキスト色。tailwindcssのクラス名。 */
  GRID_TEXT_COLOR: [
    'text-gray-300',
    'text-amber-600',
    'text-sky-600',
    'text-rose-600',
  ],
  /** グラフのノードのスタイル。tailwindcssのクラス名は使えないので、HEXカラーコードを指定する。 */
  GRAPH_NODE_COLOR: {
    'オレンジ': '#D97706', // text-amber-600
    '青': '#0284C7', // text-sky-600
    '赤': '#E11D48', // text-rose-600
    '緑': '#059669', // text-emerald-600
  },
}

/**
 * 書式条件を適用する。戻り値は className
 */
export const applyFormatCondition = (row: Entity, formatCondition: FormatCondition[] | undefined): {
  gridRowTextColor?: string
  graphNodeColor?: string
  invisibleInGraph?: boolean
} => {
  if (!formatCondition) return {};

  let appliedTextColor = '';
  let appliedGraphNodeColor: string | undefined = undefined;
  let appliedInvisibleInGraph: boolean | undefined = undefined;

  for (const x of formatCondition) {
    const value = MentionUtil.toPlainText(row.attributeValues[x.if.attributeId] ?? '');
    if (value === undefined) continue;

    // 完全一致
    if (x.if.logic === 'equals' && value === x.if.search) {
      if (!appliedTextColor) appliedTextColor = x.then.gridRowTextColor ?? '';
      if (!appliedGraphNodeColor) appliedGraphNodeColor = x.then.graphNodeColor;
      if (!appliedInvisibleInGraph) appliedInvisibleInGraph = x.then.invisibleInGraph;
    }
    if (x.if.logic === 'notEquals' && value !== x.if.search) {
      if (!appliedTextColor) appliedTextColor = x.then.gridRowTextColor ?? '';
      if (!appliedGraphNodeColor) appliedGraphNodeColor = x.then.graphNodeColor;
      if (!appliedInvisibleInGraph) appliedInvisibleInGraph = x.then.invisibleInGraph;
    }

    // 部分一致
    if (x.if.logic === 'includes' && value.includes(x.if.search)) {
      if (!appliedTextColor) appliedTextColor = x.then.gridRowTextColor ?? '';
      if (!appliedGraphNodeColor) appliedGraphNodeColor = x.then.graphNodeColor;
      if (!appliedInvisibleInGraph) appliedInvisibleInGraph = x.then.invisibleInGraph;
    }
    if (x.if.logic === 'notIncludes' && !value.includes(x.if.search)) {
      if (!appliedTextColor) appliedTextColor = x.then.gridRowTextColor ?? '';
      if (!appliedGraphNodeColor) appliedGraphNodeColor = x.then.graphNodeColor;
      if (!appliedInvisibleInGraph) appliedInvisibleInGraph = x.then.invisibleInGraph;
    }

    if (appliedTextColor && appliedGraphNodeColor && appliedInvisibleInGraph) break;
  }

  return {
    gridRowTextColor: appliedTextColor,
    graphNodeColor: appliedGraphNodeColor,
    invisibleInGraph: appliedInvisibleInGraph,
  };
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

  /** アプリケーション全体の設定 */
  appSettings: AppSettingsForDisplay

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

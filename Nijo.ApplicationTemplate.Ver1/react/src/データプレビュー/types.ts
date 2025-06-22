/** クエリエディタのデータ構造 */
export type QueryEditor = {
  id: string
  title: string
  items: QueryEditorItem[]
  comments: Comment[]
}

/** クエリエディタのアイテム */
export type QueryEditorItem = SqlAndResult | DbTableEditor

/** SQLとその結果を表示するアイテム */
export type SqlAndResult = {
  id: string
  title: string
  type: "sqlAndResult"
  sql: string
  isSettingCollapsed: boolean
  layout: EditorItemLayout
}

/** データベースのテーブルを表示するアイテム */
export type DbTableEditor = {
  id: string
  title: string
  type: "dbTableEditor"
  tableName: string
  whereClause: string
  isSettingCollapsed: boolean
  layout: EditorItemLayout
}

/** 各クエリやテーブル編集がサーバーからの再読み込みをトリガーするためのトークン */
export type ReloadTrigger = unknown

export type EditorItemLayout = {
  x: number
  y: number
  width: number
  height: number
}

// ------------------------------------
/** コメント */
export type Comment = {
  id: string
  content: string
  layout: EditorItemLayout
}

// ------------------------------------
export type UseQueryEditorServerApiReturn = {
  /** クエリを実行する */
  executeQuery: (sql: string) => Promise<{ ok: true, records: ExecuteQueryReturn } | { ok: false, error: string }>
  /** テーブルメタデータ取得 */
  getTableMetadata: () => Promise<{ ok: true, data: DbTableMetadata[] } | { ok: false, error: string }>
  /** 更新用レコード取得 */
  getDbRecords: (query: GetDbRecordsParameter) => Promise<{ ok: true, data: GetDbRecordsReturn } | { ok: false, error: string }>
  /** レコード一括更新 */
  batchUpdate: (records: EditableDbRecord[]) => Promise<{ ok: true } | { ok: false, error: string }>
}

export type GetDbRecordsParameter = {
  tableName: string
  whereClause: string
}

export type DbTableMetadata = {
  tableName: string
  columns: DbColumnMetadata[]
}

export type DbColumnMetadata = {
  columnName: string
  type: string
  isPrimaryKey: boolean
  isNullable: boolean
  foreignKeyTableName: string | null
  foreignKeyColumnName: string | null
}

export type ExecuteQueryReturn = {
  columns: string[]
  rows: Record<string, string | null>[]
}

export type GetDbRecordsReturn = {
  records: EditableDbRecord[]
}

export type EditableDbRecord = {
  tableName: string
  values: Record<string, string | null>
  existsInDb: boolean
  changed: boolean
  deleted: boolean
}

import * as React from "react"
import type * as ReactHookForm from 'react-hook-form'
import { GetColumnDefsFunction } from "../cellType"

/** EditableGridのプロパティ */
export type EditableGridProps<TRow> = {
  /** 行のデータ。通常はuseFieldArrayの返り値を渡す。 */
  rows: TRow[]
  /** 列定義を取得する関数。この関数の参照が変わる度にグリッドが再レンダリングされるため、原則として `useCallback` を使用すること。 */
  getColumnDefs: GetColumnDefsFunction<TRow>
  /** 行データが変更された際のコールバック。これが指定されていない場合、そのグリッドは読み取り専用とみなす。 */
  onChangeRow?: (row: TRow, rowIndex: number) => void
  /** 行ヘッダのチェックボックスを表示するかどうか。 */
  showCheckBox?: true | ((row: TRow, rowIndex: number) => boolean)
  /** グリッドを読み取り専用にするかどうか。 */
  isReadOnly?: true | ((row: TRow, rowIndex: number) => boolean)
  /** グリッドのクラス名。 */
  className?: string
}

/** EditableGridのref */
export type EditableGridRef<TRow> = {
  /** 現在選択されている行を取得する */
  getSelectedRows: () => { row: TRow, rowIndex: number }[]
  /** 特定の行を選択する */
  selectRow: (startRowIndex: number, endRowIndex: number) => void
}

export interface CellPosition {
  rowIndex: number;
  colIndex: number;
}

export interface CellSelectionRange {
  startRow: number;
  startCol: number;
  endRow: number;
  endCol: number;
}

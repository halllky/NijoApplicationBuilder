import * as React from "react"
import type * as ReactHookForm from 'react-hook-form'
import { GetColumnDefsFunction } from "../cellType"
import type { ColumnDef as TanStackColumnDef } from '@tanstack/react-table'
import type { CellTypeDefs } from '../cellType/cellTypeDefinition'

/** EditableGridのプロパティ */
export type EditableGridProps<TRow extends ReactHookForm.FieldValues> = {
  /** 行のデータ。通常はuseFieldArrayの返り値を渡す。 */
  rows: TRow[]
  /** 列定義を取得する関数。この関数の参照が変わる度にグリッドが再レンダリングされるため、原則として `useCallback` を使用すること。 */
  getColumnDefs: (cellTypes: CellTypeDefs<TRow>) => ColumnDef<TRow>[];
  /** 行データが変更された際のコールバック。これが指定されていない場合、そのグリッドは読み取り専用とみなす。 */
  onChangeRow?: (row: TRow, rowIndex: number) => void
  /** 行ヘッダのチェックボックスを表示するかどうか。 */
  showCheckBox?: boolean | ((row: TRow, rowIndex: number) => boolean)
  /** グリッドを読み取り専用にするかどうか。 */
  isReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean)
  /** グリッドのクラス名。 */
  className?: string

  /** TanStack Tableの行選択状態 */
  rowSelection: Record<string, boolean>
  /** 行選択状態が変更されたときに呼び出されるコールバック */
  onRowSelectionChange: (updater: React.SetStateAction<Record<string, boolean>>) => void
}

/** EditableGridのref */
export type EditableGridRef<TRow extends ReactHookForm.FieldValues> = {
  /** 現在選択されている行を取得する */
  getSelectedRows: () => TRow[]
  /** 特定の行を選択する */
  selectRow: (startRowIndex: number, endRowIndex: number) => void
  getActiveCell: () => { rowIndex: number, colIndex: number } | undefined
  getSelectedRange: () => { startRow: number, startCol: number, endRow: number, endCol: number } | undefined
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

/** EditableGridの列定義 */
export type EditableGridColumnDef<TRow extends ReactHookForm.FieldValues> = {
  /** 列のヘッダーテキスト */
  header: string;
  /** react-hook-formのフィールドパス */
  fieldPath?: ReactHookForm.Path<TRow>;
  /** セルのデータ型 */
  cellType?: keyof CellTypeDefs<TRow>['components'];
  /** セルのオプション */
  cellOption?: any;
  /** 列の幅 */
  width?: number | string;
  /** 列が読み取り専用かどうか */
  isReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean);
}

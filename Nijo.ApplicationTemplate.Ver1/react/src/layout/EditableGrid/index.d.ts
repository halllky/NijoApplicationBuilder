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
  getColumnDefs: GetColumnDefsFunction<TRow>
  /** セルデータが変更されたときのコールバック */
  onChangeCell?: (rowIndex: number, fieldPath: string, newValue: any) => void
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
  getSelectedRows: () => { row: TRow, rowIndex: number }[]
  /** 特定の行を選択する */
  selectRow: (startRowIndex: number, endRowIndex: number) => void
  getActiveCell: () => { rowIndex: number, colIndex: number } | undefined
  getSelectedRange: () => { startRow: number, startCol: number, endRow: number, endCol: number } | undefined
}

/** ボディセルの位置を表す構造体。 */
export interface CellPosition {
  rowIndex: number
  colIndex: number
}

/** セル選択範囲を表す構造体。 */
export interface CellSelectionRange {
  startRow: number
  startCol: number
  endRow: number
  endCol: number
}

/**
 * 列定義を取得する関数の型。
 * この関数の参照が変わる度にグリッドが再レンダリングされるため、
 * 原則として `useCallback` を使用すること。
 *
 * @param cellType セル型定義ヘルパー関数の一覧。
 */
export type GetColumnDefsFunction<TRow extends ReactHookForm.FieldValues> = (cellType: ColumnDefFactories<TRow>) => CellTypeColumnDef<TRow>[]


/** EditableGridの列定義 */
export type EditableGridColumnDef<TRow extends ReactHookForm.FieldValues> = EditableGridColumnDefOptions<TRow> & {
  /** react-hook-formのフィールドパス。フォームのルートからではなく行データのルートからのパスを指定する。 */
  fieldPath?: ReactHookForm.Path<TRow>
  /** セルのデータ型 */
  cellType?: keyof CellTypeDefs<TRow>['components']
}

/** EditableGridの列定義のうち、セル型によらず共通のプロパティ。 */
export type EditableGridColumnDefOptions<TRow extends ReactHookForm.FieldValues> = {
  /** 列のヘッダーテキスト */
  header: string
  /** 画面初期表示時の列の幅 */
  defaultWidth?: number | string
  /** 列が読み取り専用かどうか */
  isReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean)
}

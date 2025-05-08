import * as React from "react"
import type * as ReactHookForm from 'react-hook-form'
import { GetColumnDefsFunction } from "../cellType"
import type { ColumnDef as TanStackColumnDef } from '@tanstack/react-table'
import type { CellTypeDefs } from '../cellType/cellTypeDefinition'
import type { ColumnDefFactories } from './useCellTypes'

/** EditableGridのプロパティ */
export type EditableGridProps<TRow extends ReactHookForm.FieldValues> = {
  /** 行のデータ。通常はuseFieldArrayの返り値を渡す。 */
  rows: TRow[]
  /** 列定義を取得する関数。この関数の参照が変わる度にグリッドが再レンダリングされるため、原則として `useCallback` を使用すること。 */
  getColumnDefs: GetColumnDefsFunction<TRow>

  /** セルデータが変更されたときのコールバック */
  onCellEdited?: CellValueEditedEvent<TRow>
  /** クリップボードからの貼り付けが行われたときのコールバック。セルの範囲選択のうえまとめてペーストされることがあるので、複数行分が一気に発火される。 */
  onPasted?: CellValuePastedEvent<TRow>
  /** 編集時、ペースト時に利用される、行オブジェクトのクローンのロジック。未指定の場合は window.structuredClone を使用する。 */
  cloneRow?: (item: TRow) => TRow

  /** 行ヘッダのチェックボックスを表示するかどうか。 */
  showCheckBox?: boolean | ((row: TRow, rowIndex: number) => boolean)
  /** グリッドを読み取り専用にするかどうか。 */
  isReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean)
  /** グリッドのクラス名。 */
  className?: string

  /** TanStack Tableの行選択状態 */
  rowSelection?: Record<string, boolean>
  /** 行選択状態が変更されたときに呼び出されるコールバック */
  onRowSelectionChange?: (updater: React.SetStateAction<Record<string, boolean>>) => void
}

/** セルデータが変更されたときのコールバック */
export type CellValueEditedEvent<TRow extends ReactHookForm.FieldValues> = (e: {
  rowIndex: number
  oldRow: TRow
  newRow: TRow
}) => void

/** クリップボードからの貼り付けが行われたときのコールバック。セルの範囲選択のうえまとめてペーストされることがあるので、複数行分が一気に発火される。 */
export type CellValuePastedEvent<TRow extends ReactHookForm.FieldValues> = (e: {
  pastedRows: {
    rowIndex: number
    oldRow: TRow
    newRow: TRow
  }[]
}) => void

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
  /** 列ヘッダに表示する文字列 */
  header: string
  /** react-hook-formのフィールドパス。フォームのルートからではなく行データのルートからのパスを指定する。 */
  fieldPath?: ReactHookForm.Path<TRow>
  /** セルのデータ型 */
  cellType?: keyof CellTypeDefs<TRow>['components']
}

/** EditableGridの列定義のうち、セル型によらず共通のプロパティ。 */
export type EditableGridColumnDefOptions<TRow extends ReactHookForm.FieldValues> = {
  /** 列ヘッダに必須を表すマークが表示されるかどうか。これをtrueにしても内容のチェックが行われるわけではない。 */
  required?: boolean
  /** 画面初期表示時の列の幅 */
  defaultWidth?: number | string
  /** 列が読み取り専用かどうか */
  isReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean)
  /** @deprecated このオプションは廃止されました。 `isReadOnly` を使用してください。 */
  editable?: boolean
}

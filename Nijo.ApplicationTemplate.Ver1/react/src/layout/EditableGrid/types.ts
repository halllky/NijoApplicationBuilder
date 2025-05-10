import * as React from "react"
import type * as ReactHookForm from 'react-hook-form'
import type { ColumnDefFactories } from './useCellTypes'
import { CellContext } from "@tanstack/react-table"

/** EditableGridのプロパティ */
export type EditableGridProps<TRow extends ReactHookForm.FieldValues> = {
  /** 行のデータ。通常はuseFieldArrayの返り値を渡す。 */
  rows: TRow[]
  /**
   * 列定義を取得する関数。
   * この関数の参照が変わる度にグリッドが再レンダリングされるため、原則として `useCallback` を使用すること。
   * 列の順番で識別子を振っているため、動的に列が変わる場合であっても、常に同じ数の列定義を返すこと。
   * 非表示の列については `invisible` オプションを使用すること。
   */
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
  /** 行頭のチェックボックスで選択されている行を取得する */
  getCheckedRows: () => { row: TRow, rowIndex: number }[]
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
export type GetColumnDefsFunction<TRow extends ReactHookForm.FieldValues> = (cellType: ColumnDefFactories<TRow>) => EditableGridColumnDef<TRow>[]


/** EditableGridの列定義 */
export type EditableGridColumnDef<TRow extends ReactHookForm.FieldValues> = EditableGridColumnDefOptions<TRow> & {
  /** 列ヘッダに表示する文字列 */
  header: string
  /** react-hook-formのフィールドパス。フォームのルートからではなく行データのルートからのパスを指定する。 */
  fieldPath?: ReactHookForm.Path<TRow>
}

/** EditableGridの列定義のうち、セル型によらず共通のプロパティ。 */
export type EditableGridColumnDefOptions<TRow extends ReactHookForm.FieldValues> = {
  /** 列ヘッダに必須を表すマークが表示されるかどうか。これをtrueにしても内容のチェックが行われるわけではない。 */
  required?: boolean
  /** 画面初期表示時の列の幅（pxで指定） */
  defaultWidth?: number
  /** 列が読み取り専用かどうか */
  isReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean)
  /** 列の幅を変更できるかどうか */
  enableResizing?: boolean
  /** 列が非表示になるかどうか */
  invisible?: boolean
  /** 列が固定されるかどうか */
  isFixed?: boolean
  /** セルのレンダリング処理をカスタマイズする関数。 */
  renderCell?: EditableGridColumnDefRenderCell<TRow>
  /** @deprecated このオプションは廃止されました。 `isReadOnly` を使用してください。 */
  editable?: boolean

  /** 編集開始時に呼び出される関数 */
  onStartEditing?: EditableGridColumnDefOnStartEditing<TRow>
}

/** セルのレンダリング処理をカスタマイズする関数。 */
export type EditableGridColumnDefRenderCell<TRow extends ReactHookForm.FieldValues> = (cell: CellContext<TRow, unknown>) => React.ReactNode

/** 編集開始時に呼び出される関数 */
export type EditableGridColumnDefOnStartEditing<TRow extends ReactHookForm.FieldValues> = (e: {
  rowIndex: number
  row: TRow
  /**
   * この関数を呼んで値を渡すとエディタでの編集が開始される。
   * 未指定の場合は `fieldPath` の値で行オブジェクトの値が参照される。
   * それも指定されていない場合はそのセルは編集不可とみなす。
   */
  setEditorValue: (value: string) => void
}) => void


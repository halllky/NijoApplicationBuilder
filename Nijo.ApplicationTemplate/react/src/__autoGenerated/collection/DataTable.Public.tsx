import React from 'react'
import { ComboProps } from '..'

export type DataTableRef<T> = {
  focus: () => void
  startEditing: () => void
  getSelectedRows: () => { row: T, rowIndex: number }[]
}

export type DataTableProps<T> = {
  data?: T[]
  onChangeRow?: (index: number, data: T) => void
  onKeyDown?: React.KeyboardEventHandler
  onActiveRowChanged?: (activeRow: { getRow: () => T, rowIndex: number } | undefined) => void
  columns?: DataTableColumn<T>[]
  hideHeader?: boolean
  showActiveCellBorderAlways?: boolean
  tableWidth?: 'fit' | 'dyanmic'
  className?: string
}

// -------------------------------------------
// 列定義
export type DataTableColumn<TRow> = {
  id: string
  header?: string
  /** ツールチップに表示される方 */
  headerTitle?: string
  render: (row: TRow, rowIndex: number) => React.ReactNode
  onClipboardCopy: (row: TRow, rowIndex: number) => string
  headerGroupName?: string
  defaultWidthPx?: number
  fixedWidth?: boolean
  editSetting?: ColumnEditSetting<TRow>
}

export type ColumnEditSetting<TRow, TOption = unknown> = {
  readOnly?: ((row: TRow) => boolean)
  onClipboardPaste: (row: TRow, value: string, rowIndex: number) => void
} & (TextColumnEditSetting<TRow>
  | TextareaColumndEditSetting<TRow>
  | SyncComboColumnEditSetting<TRow, TOption>
  | AsyncComboColumnEditSetting<TRow, TOption>)

type TextColumnEditSetting<TRow> = {
  type: 'text'
  onStartEditing: (row: TRow) => string | undefined
  onEndEditing: (row: TRow, value: string | undefined, rowIndex: number) => void
}
type TextareaColumndEditSetting<TRow> = {
  type: 'multiline-text'
  onStartEditing: (row: TRow) => string | undefined
  onEndEditing: (row: TRow, value: string | undefined, rowIndex: number) => void
}
type SyncComboColumnEditSetting<TRow, TOption = unknown> = {
  type: 'combo'
  onStartEditing: (row: TRow) => TOption | undefined
  onEndEditing: (row: TRow, value: TOption | undefined, rowIndex: number) => void
  comboProps: ComboProps<TOption, TOption>
}
type AsyncComboColumnEditSetting<TRow, TOption = unknown> = {
  type: 'async-combo'
  onStartEditing: (row: TRow) => TOption | undefined
  onEndEditing: (row: TRow, value: TOption | undefined, rowIndex: number) => void
  comboProps: ComboProps<TOption, TOption>
}

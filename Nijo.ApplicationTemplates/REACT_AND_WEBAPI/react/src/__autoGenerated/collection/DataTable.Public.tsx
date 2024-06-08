import React from 'react'
import * as RT from '@tanstack/react-table'
import { AsyncComboProps, CustomComponentProps } from '..'

export type DataTableProps<T> = {
  data?: T[]
  onChangeRow?: (index: number, data: T) => void
  onKeyDown?: React.KeyboardEventHandler
  onActiveRowChanged?: (activeRow: { getRow: () => T, rowIndex: number } | undefined) => void
  columns?: ColumnDefEx<T>[]
  className?: string
}
export type ColumnDefEx<TRow> = RT.ColumnDef<TRow> & {
  hidden?: boolean
  headerGroupName?: string
  editSetting?: ColumnEditSetting<TRow>
}

export type ColumnEditSetting<TRow, TOption = unknown> = {
  readOnly?: ((row: TRow) => boolean)
} & (TextColumndEditSetting<TRow>
  | AsyncComboColumnEditSetting<TRow, TOption>)

type TextColumndEditSetting<TRow> = {
  type: 'text'
  getTextValue: (row: TRow) => string | undefined
  setTextValue: (row: TRow, value: string | undefined) => void
}
type AsyncComboColumnEditSetting<TRow, TOption = unknown> = {
  type: 'async-combo'
  getValueFromRow: (row: TRow) => TOption | undefined
  setValueToRow: (row: TRow, value: TOption | undefined) => void
} & AsyncComboProps<TOption, TOption>

export type DataTableRef<T> = {
  getSelectedRows: () => { row: T, rowIndex: number }[]
  getSelectedIndexes: () => number[]
}

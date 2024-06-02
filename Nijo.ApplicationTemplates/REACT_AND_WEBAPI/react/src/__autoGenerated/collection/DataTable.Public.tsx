import React from 'react'
import * as RT from '@tanstack/react-table'
import { CustomComponentRef } from '../input'

export type DataTableProps<T> = {
  data?: T[]
  onChangeRow?: (index: number, data: T) => void
  onKeyDown?: React.KeyboardEventHandler
  onActiveRowChanged?: (activeRow: { getRow: () => T, rowIndex: number } | undefined) => void
  columns?: ColumnDefEx<T>[]
  className?: string
}
export type ColumnDefEx<TRow, TValue = any> = RT.ColumnDef<TRow> & {
  hidden?: boolean
  headerGroupName?: string
} & ({
  cellEditor?: never
  setValue?: never
} | {
  cellEditor: CellEditor<TValue>
  setValue: (data: TRow, value: TValue) => void
})

export type CellEditor<TValue> = (
  props: CellEditorProps<TValue>,
  ref: React.Ref<CustomComponentRef<TValue>>
) => JSX.Element

export type CellEditorProps<TValue> = {
  value: TValue | undefined
  onChange: (value: TValue | undefined) => void
  onKeyDown: React.KeyboardEventHandler<HTMLElement>
  onBlur: React.FocusEventHandler<HTMLElement>
  className: string
}

export type DataTableRef<T> = {
  getSelectedRows: () => { row: T, rowIndex: number }[]
  getSelectedIndexes: () => number[]
}

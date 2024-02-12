import * as RT from '@tanstack/react-table'
import * as Tree from '../util'
import * as Input from '../input'

export type DataTableProps<T> = {
  data?: T[]
  onChangeRow?: (index: number, data: T) => void
  columns?: ColumnDefEx<Tree.TreeNode<T>>[]
  className?: string
  treeView?: Tree.ToTreeArgs<T> & {
    rowHeader: (row: T) => React.ReactNode
  }
}
export type ColumnDefEx<T> = RT.ColumnDef<T> & ({
  cellEditor?: never
  setValue?: never
} | {
  cellEditor: Input.CustomComponent
  setValue: (data: T, value: any) => void
})

export type DataTableRef<T> = {
  getSelectedRows: () => { row: T, rowIndex: number }[]
  getSelectedItems: () => T[]
  getSelectedIndexes: () => number[]
}

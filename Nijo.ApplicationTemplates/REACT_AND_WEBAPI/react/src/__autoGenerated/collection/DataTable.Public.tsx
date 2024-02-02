import * as RT from '@tanstack/react-table'
import * as Tree from '../util'
import * as Util from '../util'

export type DataTableProps<T> = {
  data?: T[]
  onChangeRow?: (index: number, data: T) => void
  columns?: ColumnDefEx<Tree.TreeNode<T>>[]
  className?: string
  treeView?: Tree.ToTreeArgs<T> & {
    rowHeader: (row: T) => React.ReactNode
  }
  children?: React.ReactNode
  keepSelectWhenNotFocus?: boolean
}
export type ColumnDefEx<T> = RT.ColumnDef<T> & {
  cellEditor?: Util.CustomComponent
}

export type DataTableRef<T> = {
  getSelectedItems: () => T[]
  getSelectedIndexes: () => number[]
}

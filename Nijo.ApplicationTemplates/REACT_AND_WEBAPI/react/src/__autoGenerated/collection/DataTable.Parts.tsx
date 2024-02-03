import * as RT from '@tanstack/react-table'
import * as Icon from '@heroicons/react/24/outline'
import * as Input from '../input'
import * as Tree from '../util'
import { DataTableProps } from './DataTable.Public'

// --------------------------------------------
// z-index
export const TABLE_ZINDEX = {
  CELLEDITOR: 30 as const,
  ROWHEADER_THEAD: 21 as const,
  THEAD: 20 as const,
  ROWHEADER_SELECTION: 11 as const,
  ROWHEADER: 10 as const,
  SELECTION: 1 as const,
}

// --------------------------------------------
// 行ヘッダ
export const ROW_HEADER_ID = '__tree_explorer_row_header__'

export const getRowHeader = <T,>(
  helper: RT.ColumnHelper<Tree.TreeNode<T>>,
  treeView: DataTableProps<T>['treeView']
): RT.ColumnDef<Tree.TreeNode<T>> => helper.display({
  id: ROW_HEADER_ID,
  header: api => (
    <div className="inline-block h-full w-full bg-color-3">
      <Input.Button
        icon={Icon.MinusIcon} iconOnly small outlined className="m-1"
        onClick={() => api.table.toggleAllRowsExpanded()}>
        折りたたみ
      </Input.Button>
    </div>
  ),
  cell: api => (
    <div className="relative flex gap-1 w-full bg-color-0"
      style={{ paddingLeft: api.row.depth * 24 }}>
      <Input.Button
        iconOnly small
        icon={api.row.getIsExpanded() ? Icon.ChevronDownIcon : Icon.ChevronRightIcon}
        className={(api.row.subRows.length === 0) ? 'invisible' : undefined}
        onClick={e => { api.row.toggleExpanded(); e.stopPropagation() }}>
        折りたたむ
      </Input.Button>

      <span className="flex-1 whitespace-nowrap">
        {treeView?.rowHeader(api.row.original.item)}
      </span>
    </div>
  ),
})

import React, { useCallback, useMemo, useReducer, useState } from 'react'
import * as RT from '@tanstack/react-table'
import * as Icon from '@heroicons/react/24/outline'
import * as Util from '../util'
import * as Tree from '../util'
import * as Input from '../input'

export type DataTableProps<T> = {
  data?: T[]
  columns?: RT.ColumnDef<Tree.TreeNode<T>>[]
  className?: string
  treeView?: Tree.ToTreeArgs<T> & {
    rowHeader: (row: T) => React.ReactNode
  }
}

export const DataTable = <T,>(props: DataTableProps<T>) => {
  // 行
  const dataAsTree = useMemo(() => {
    if (!props.data) return []
    return props.treeView
      ? Tree.toTree(props.data, props.treeView)
      // ツリー表示に関する設定が無い場合は親子関係のないフラットな配列にする
      : Tree.toTree(props.data, undefined)
  }, [props.data])

  // 列
  const columnHelper = useMemo(() => RT.createColumnHelper<Tree.TreeNode<T>>(), [])
  const columns = useMemo(() => {
    return props.treeView
      ? ([getRowHeader(columnHelper, props), ...(props.columns ?? [])])
      : (props.columns ?? [])
  }, [props.columns, props.treeView?.rowHeader])

  // 表
  const { selectionOptions } = useSelectionOption()
  const optoins: RT.TableOptions<Tree.TreeNode<T>> = useMemo(() => ({
    data: dataAsTree,
    columns,
    getSubRows: row => row.children,
    getCoreRowModel: RT.getCoreRowModel(),
    ...selectionOptions,
    ...COLUMN_RESIZE_OPTION,
  }), [dataAsTree, columns, selectionOptions])

  const api = RT.useReactTable(optoins)
  const { selectRow, handleSelectionKeyDown, getCellBackColor } = useSelection<Util.TreeNode<T>>(api)
  const { columnSizeVars, getColWidth, ResizeHandler } = useColumnResizing(api)

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useCallback(e => {
    // console.log(e.key)
    if (e.key === ' ') {
      for (const row of api.getSelectedRowModel().flatRows) row.toggleExpanded()
      e.preventDefault()
      return
    }
    handleSelectionKeyDown(e)
    if (e.defaultPrevented) return
  }, [api, handleSelectionKeyDown])

  return (
    <div
      className={`overflow-auto select-none outline-none border border-1 border-color-4 bg-color-2 ${props.className}`}
      onKeyDown={handleKeyDown}
      tabIndex={0}
    >
      <table
        className="table-fixed border-separate border-spacing-0"
        style={{ ...columnSizeVars, width: api.getTotalSize() }}
      >
        {/* ヘッダ */}
        <thead>
          {api.getHeaderGroups().map(headerGroup => (
            <tr key={headerGroup.id}>

              {headerGroup.headers.map(header => (
                <th key={header.id}
                  className="relative overflow-x-hidden text-start sticky top-0 pl-1 bg-color-3"
                  style={{ width: getColWidth(header), ...getThStickeyStyle(header) }}>
                  {!header.isPlaceholder && RT.flexRender(
                    header.column.columnDef.header,
                    header.getContext())}
                  <ResizeHandler header={header} />
                </th>
              ))}

            </tr>
          ))}
        </thead>

        {/* ボディ */}
        <tbody>
          {api.getRowModel().flatRows.filter(row => row.getIsAllParentsExpanded()).map(row => (
            <tr
              key={row.id}
              className={`leading-tight ` + (row.getIsSelected() ? 'bg-color-selected' : undefined)}
              onClick={e => selectRow(row, e)}
            >
              {row.getVisibleCells().map(cell => (
                <td key={cell.id}
                  className={'overflow-x-hidden border-r border-1 border-color-3 '
                    + getCellBackColor(row)}
                  style={getTdStickeyStyle(cell)}>
                  {RT.flexRender(
                    cell.column.columnDef.cell,
                    cell.getContext())}
                  &nbsp; {/* <= すべての値が空の行がつぶれるのを防ぐ */}
                </td>
              ))}

            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

// -----------------------------------------------
/** 選択 */
const useSelectionOption = () => {
  const [rowSelection, onRowSelectionChange] = useState<RT.RowSelectionState>({})
  const selectionOptions: Partial<RT.TableOptions<Tree.TreeNode<any>>> = {
    onRowSelectionChange,
    enableSubRowSelection: true,
    state: { rowSelection },
  }
  return {
    selectionOptions,
  }
}
const useSelection = <T,>(api: RT.Table<T>) => {
  const [activeRow, setActiveRow] = useState<RT.Row<T> | undefined>(undefined)
  const [selectionStart, setSelectionStart] = useState<RT.Row<T> | undefined>(undefined)

  const getCellBackColor = useCallback((row: RT.Row<T>) => {
    return row.getIsSelected() ? 'bd-color-selected' : 'bg-color-0'
  }, [])

  const selectRow = useCallback((row: RT.Row<T>, e: { shiftKey: boolean, ctrlKey: boolean }) => {
    if (e.ctrlKey) {
      row.toggleSelected(undefined, { selectChildren: false })

    } else if (e.shiftKey && selectionStart) {
      // 範囲選択
      const flatRows = api.getRowModel().flatRows
      const ix1 = flatRows.indexOf(selectionStart)
      const ix2 = flatRows.indexOf(row)
      const since = Math.min(ix1, ix2)
      const until = Math.max(ix1, ix2)
      const newState: { [key: string]: true } = {}
      for (let i = since; i <= until; i++) {
        newState[flatRows[i].id] = true
      }
      api.setRowSelection(newState)
      setActiveRow(row)

    } else {
      // シングル選択。引数のrowのみが選択されている状態にする
      api.setRowSelection({ [row.id]: true })
      setActiveRow(row)
      setSelectionStart(row)
    }
  }, [api, activeRow, selectionStart])

  const handleSelectionKeyDown: React.KeyboardEventHandler<HTMLElement> = useCallback(e => {
    if (e.ctrlKey && e.key === 'a') {
      api.toggleAllRowsSelected(true)
      e.preventDefault()

    } else if (!e.ctrlKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      const flatRows = api.getRowModel().flatRows
      if (!activeRow) {
        // 未選択の状態なので先頭行を選択
        if (flatRows.length >= 1) selectRow(flatRows[0], e)
        e.preventDefault()
        return
      }
      // 1つ上または下の行を選択
      let ix = flatRows.indexOf(activeRow)
      if (e.key === 'ArrowUp') ix--; else ix++
      while (e.key === 'ArrowUp' ? (ix > -1) : (ix < flatRows.length)) {
        const row = flatRows[ix]
        if (row === undefined) return
        if (!row.getIsAllParentsExpanded()) {
          if (e.key === 'ArrowUp') ix--; else ix++
          continue
        }
        selectRow(row, e)
        e.preventDefault()
        return
      }
    }
  }, [api, selectRow, activeRow])

  return {
    selectRow,
    handleSelectionKeyDown,
    getCellBackColor,
  }
}

// -----------------------------------------------
// 行列ヘッダ固定
const getThStickeyStyle = (header: RT.Header<any, unknown>): React.CSSProperties => ({
  left: header.column.id === ROW_HEADER_ID ? '0' : undefined,
  // y方向にスクロールしたときに行ヘッダの列のtdが上にかぶさってthが見えなくなるのを防ぐ
  zIndex: header.column.id === ROW_HEADER_ID ? 1 : undefined,
})
const getTdStickeyStyle = (cell: RT.Cell<any, unknown>): React.CSSProperties => ({
  left: cell.column.id === ROW_HEADER_ID ? '0' : undefined,
  position: cell.column.id === ROW_HEADER_ID ? 'sticky' : undefined,
})

// -----------------------------------------------
/** 行ヘッダ */
const getRowHeader = <T,>(
  helper: RT.ColumnHelper<Tree.TreeNode<T>>,
  props: DataTableProps<T>
): RT.ColumnDef<Tree.TreeNode<T>> => helper.display({
  id: ROW_HEADER_ID,
  header: api => (
    <div>
      <Input.Button
        icon={Icon.MinusIcon} iconOnly small outlined className="m-1"
        onClick={() => api.table.toggleAllRowsExpanded()}>
        折りたたみ
      </Input.Button>
    </div>
  ),
  cell: api => (
    <div className="inline-flex gap-1 w-full"
      style={{ paddingLeft: api.row.depth * 24 }}>

      <Input.Button
        iconOnly small
        icon={api.row.getIsExpanded() ? Icon.ChevronDownIcon : Icon.ChevronRightIcon}
        className={(api.row.subRows.length === 0) ? 'invisible' : undefined}
        onClick={e => { api.row.toggleExpanded(); e.stopPropagation() }}>
        折りたたむ
      </Input.Button>

      <span className="flex-1 whitespace-nowrap">
        {props.treeView?.rowHeader(api.row.original.item)}
      </span>
    </div>
  ),
})
const ROW_HEADER_ID = '__tree_explorer_row_header__'

// -----------------------------------------------
/** 列幅変更 */
const useColumnResizing = <T,>(api: RT.Table<Util.TreeNode<T>>) => {

  const columnSizeVars = useMemo(() => {
    const headers = api.getFlatHeaders()
    const colSizes: { [key: string]: number } = {}
    for (let i = 0; i < headers.length; i++) {
      const header = headers[i]!
      colSizes[`--header-${header.id}-size`] = header.getSize()
      colSizes[`--col-${header.column.id}-size`] = header.column.getSize()
    }
    return colSizes
  }, [api.getState().columnSizingInfo])

  const getColWidth = useCallback((header: RT.Header<Util.TreeNode<T>, unknown>) => {
    return `calc(var(--header-${header?.id}-size) * 1px)`
  }, [])

  const ResizeHandler = useCallback(({ header }: {
    header: RT.Header<Util.TreeNode<T>, unknown>
  }) => {
    return (
      <div {...{
        onDoubleClick: () => header.column.resetSize(),
        onMouseDown: header.getResizeHandler(),
        onTouchStart: header.getResizeHandler(),
        className: `absolute top-0 bottom-0 right-0 w-3 cursor-ew-resize border-r border-color-4`,
      }}>
      </div>
    )
  }, [])

  return {
    columnSizeVars,
    getColWidth,
    ResizeHandler,
  }
}

const COLUMN_RESIZE_OPTION: Partial<RT.TableOptions<Tree.TreeNode<any>>> = {
  defaultColumn: {
    minSize: 60,
    maxSize: 800,
  },
  columnResizeMode: 'onChange',
}

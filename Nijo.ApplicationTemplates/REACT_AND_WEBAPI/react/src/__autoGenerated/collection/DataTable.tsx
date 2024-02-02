import React, { useCallback, useImperativeHandle, useMemo } from 'react'
import * as RT from '@tanstack/react-table'
import * as Util from '../util'
import * as Tree from '../util'
import { DataTableProps, DataTableRef } from './DataTable.Public'
import { getRowHeader, ROW_HEADER_ID, ZINDEX_BASE_TH, ZINDEX_BASE_TD } from './DataTable.Parts'
import { useCellEditing } from './DataTable.Editing'
import { useSelectionOption, useSelection } from './DataTable.Selecting'

export * from './DataTable.Public'

export const DataTable = Util.forwardRefEx(<T,>(props: DataTableProps<T>, ref: React.ForwardedRef<DataTableRef<T>>) => {
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
    const colDefs = props.treeView
      ? ([getRowHeader(columnHelper, props), ...(props.columns ?? [])])
      : (props.columns ?? [])
    return colDefs.map(col => ({
      ...col,
      cell: col.cell ?? (DEFAULT_CELL as RT.ColumnDefTemplate<RT.CellContext<Util.TreeNode<T>, unknown>>),
    }))
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

  const {
    editing,
    startEditing,
    cancelEditing,
    CellEditor,
    editingTdRefCallback,
  } = useCellEditing<T>(props)

  const {
    selectObject,
    caretCell,
    clearSelection,
    handleSelectionKeyDown,
    caretTdRefCallback,
    ActiveCellBorder,
    getSelectedRows,
    getSelectedIndexes,
  } = useSelection<T>(editing, api)

  const {
    columnSizeVars,
    getColWidth,
    ResizeHandler,
  } = useColumnResizing(api)

  const tdRefCallback = (td: HTMLTableCellElement | null, cell: RT.Cell<Tree.TreeNode<T>, unknown>) => {
    caretTdRefCallback(td, cell)
    editingTdRefCallback(td, cell)
  }

  const handleBlur: React.FocusEventHandler<HTMLDivElement> = useCallback(e => {
    if (props.keepSelectWhenNotFocus) return
    // フォーカスの移動先がこの要素の中(ex: props.children)にある場合
    if (e.target.contains(e.relatedTarget)) return
    clearSelection()
  }, [props.keepSelectWhenNotFocus, clearSelection])

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useCallback(e => {
    // console.log(e.key)
    if (e.key === ' ') {
      for (const row of getSelectedRows()) row.toggleExpanded()
      e.preventDefault()
      return
    } else if (e.key === 'Escape' && editing) {
      cancelEditing()
      e.preventDefault()
      return
    }
    handleSelectionKeyDown(e)
    if (e.defaultPrevented) return
  }, [getSelectedRows, handleSelectionKeyDown])

  useImperativeHandle(ref, () => ({
    getSelectedItems: () => getSelectedRows().map(row => row.original.item),
    getSelectedIndexes,
    clearSelection,
  }))

  return (
    <div className={`flex flex-col outline-none overflow-hidden ${props.className}`}
      onBlur={handleBlur}
      onKeyDown={handleKeyDown}
      tabIndex={0}
    >
      {props.children && (
        <div className="flex gap-1 justify-start items-center">
          {props.children}
        </div>
      )}
      <div className="flex-1 overflow-auto select-none relative border border-1 border-color-4 bg-color-2">
        <table
          className="table-fixed border-separate border-spacing-0"
          style={{ ...columnSizeVars, width: api.getTotalSize() }}
        >
          {/* ヘッダ */}
          <thead>
            {api.getHeaderGroups().map(headerGroup => (
              <tr key={headerGroup.id}>

                {headerGroup.headers.map((header, colIx) => (
                  <th key={header.id}
                    className="relative overflow-hidden p-0 text-start bg-color-3"
                    style={{ width: getColWidth(header), ...getThStickeyStyle(header, colIx) }}>
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
          <tbody className="bg-color-0">
            {api.getRowModel().flatRows.filter(row => row.getIsAllParentsExpanded()).map(row => (
              <tr
                key={row.id}
                className="leading-tight"
              >
                {row.getVisibleCells().map((cell, colIx) => (
                  <td key={cell.id}
                    ref={td => tdRefCallback(td, cell)}
                    className="relative overflow-hidden p-0 border-r border-1 border-color-3"
                    style={getTdStickeyStyle(cell, colIx)}
                    onClick={e => selectObject({ cell }, e)}
                    onDoubleClick={() => startEditing(cell)}
                  >
                    {RT.flexRender(
                      cell.column.columnDef.cell,
                      cell.getContext())}
                  </td>
                ))}

              </tr>
            ))}
          </tbody>
        </table>

        {!editing && (
          <ActiveCellBorder caretCell={caretCell} api={api} />
        )}
        {editing && (
          <CellEditor />
        )}
      </div>
    </div>
  )
})

// -----------------------------------------------
// 行列ヘッダ固定
const getThStickeyStyle = (header: RT.Header<any, unknown>, colIndex: number): React.CSSProperties => ({
  position: 'sticky',
  top: 0,
  left: header.column.id === ROW_HEADER_ID ? 0 : undefined,
  zIndex: ZINDEX_BASE_TH - colIndex,
})
const getTdStickeyStyle = (cell: RT.Cell<any, unknown>, colIndex: number): React.CSSProperties => ({
  position: cell.column.id === ROW_HEADER_ID ? 'sticky' : undefined,
  left: cell.column.id === ROW_HEADER_ID ? 0 : undefined,
  zIndex: ZINDEX_BASE_TD - colIndex,
})

// -----------------------------------------------
const DEFAULT_CELL: RT.ColumnDefTemplate<RT.CellContext<Util.TreeNode<unknown>, unknown>> = cellProps => {
  return (
    <span className="block w-full overflow-hidden whitespace-nowrap">
      {cellProps.getValue() as React.ReactNode}
      &nbsp; {/* <= すべての値が空の行がつぶれるのを防ぐ */}
    </span>
  )
}

// -----------------------------------------------
/** 列幅変更 */
const useColumnResizing = <T,>(api: RT.Table<Tree.TreeNode<T>>) => {

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

  const getColWidth = useCallback((header: RT.Header<Tree.TreeNode<T>, unknown>) => {
    return `calc(var(--header-${header?.id}-size) * 1px)`
  }, [])

  const ResizeHandler = useCallback(({ header }: {
    header: RT.Header<Tree.TreeNode<T>, unknown>
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

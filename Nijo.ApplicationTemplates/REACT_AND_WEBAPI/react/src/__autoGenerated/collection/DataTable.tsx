import React, { useCallback, useImperativeHandle, useMemo, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import * as Util from '../util'
import * as Tree from '../util'
import { ColumnDefEx, DataTableProps, DataTableRef } from './DataTable.Public'
import { getRowHeader, ROW_HEADER_ID, TABLE_ZINDEX } from './DataTable.Parts'
import { useCellEditing } from './DataTable.Editing'
import { useSelection } from './DataTable.Selecting'
import { COLUMN_RESIZE_OPTION, useColumnResizing } from './DataTable.ColResize'

export * from './DataTable.Public'

export const DataTable = Util.forwardRefEx(<T,>(props: DataTableProps<T>, ref: React.ForwardedRef<DataTableRef<T>>) => {
  const {
    data,
    columns: propsColumns,
    treeView,
    className,
  } = props

  // 行
  const dataAsTree = useMemo(() => {
    if (!data) return []
    return Tree.toTree(data, treeView)
  }, [data, treeView])

  // 列
  const columnHelper = useMemo(() => RT.createColumnHelper<Tree.TreeNode<T>>(), [])
  const columns: RT.ColumnDef<Tree.TreeNode<T>>[] = useMemo(() => {
    const result: RT.ColumnDef<Tree.TreeNode<T>>[] = []
    if (treeView) result.unshift(getRowHeader(columnHelper, treeView))
    const colgroups = Util.groupBy(propsColumns ?? [], col => col.headerGroupName ?? '')
    for (const [header, columns] of colgroups) {
      if (header) {
        result.push(columnHelper.group({ header, columns }))
      } else {
        result.push(...columns)
      }
    }
    return result
  }, [propsColumns, columnHelper, treeView])

  // 表
  const optoins: RT.TableOptions<Tree.TreeNode<T>> = useMemo(() => ({
    data: dataAsTree,
    columns,
    getSubRows: row => row.children,
    getCoreRowModel: RT.getCoreRowModel(),
    ...COLUMN_RESIZE_OPTION,
  }), [dataAsTree, columns])

  const api = RT.useReactTable(optoins)

  const {
    editing,
    startEditing,
    cancelEditing,
    CellEditor,
    cellEditorProps,
    editingTdRefCallback,
  } = useCellEditing<T>(props)

  const {
    caretCell,
    selectObject,
    handleSelectionKeyDown,
    caretTdRefCallback,
    ActiveCellBorder,
    activeCellBorderProps,
    getSelectedRows,
    getSelectedIndexes,
  } = useSelection<T>(api)

  const {
    columnSizeVars,
    getColWidth,
    ResizeHandler,
  } = useColumnResizing(api)

  const tdRefCallback = (td: HTMLTableCellElement | null, cell: RT.Cell<Tree.TreeNode<T>, unknown>) => {
    caretTdRefCallback(td, cell)
    editingTdRefCallback(td, cell)
  }

  const [isActive, setIsActive] = useState(false)
  const handleFocus: React.FocusEventHandler<HTMLDivElement> = useCallback(() => {
    setIsActive(true)
    if (!caretCell) selectObject({ any: api })
  }, [api, caretCell, selectObject])
  const handleBlur: React.FocusEventHandler<HTMLDivElement> = useCallback(e => {
    // フォーカスの移動先がこの要素の中にある場合はfalseにしない
    if (!e.target.contains(e.relatedTarget)) setIsActive(false)
  }, [])

  // セル編集完了時にフォーカスが外れてキー操作ができなくなるのを防ぐ
  const containerRef = useRef<HTMLDivElement>(null)
  const onEndEditing = useCallback(() => {
    setTimeout(() => {
      containerRef.current?.focus()
    }, 10)
  }, [])

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useCallback(e => {
    // セル編集中の場合のキーハンドリングはCellEditor側で行う
    if (editing) return

    // 選択に関する操作
    handleSelectionKeyDown(e)
    if (e.defaultPrevented) return

    if (e.key === ' ') {
      for (const row of getSelectedRows()) row.toggleExpanded()
      e.preventDefault()
      return
    } else if (caretCell && (e.key === 'F2' || e.key.length === 1 /*文字や数字や記号の場合*/)) {
      // caretCellは更新前の古いセルなので最新の配列から検索しなおす
      const row = api.getRow(caretCell.rowId)
      const cell = row.getAllCells().find(cell => cell.id === caretCell.cellId)
      if (cell) startEditing(cell)
      e.preventDefault()
      return
    }
  }, [api, editing, caretCell, getSelectedRows, handleSelectionKeyDown, startEditing, cancelEditing])

  useImperativeHandle(ref, () => ({
    getSelectedRows: () => getSelectedRows().map(row => ({
      row: row.original.item,
      rowIndex: row.index,
    })),
    getSelectedIndexes,
  }), [getSelectedRows])

  return (
    <div ref={containerRef}
      className={`outline-none overflow-auto select-none relative bg-color-2 border border-1 border-color-4 ${className}`}
      onFocus={handleFocus}
      onBlur={handleBlur}
      onKeyDown={handleKeyDown}
      tabIndex={0}
    >
      <table
        className="table-fixed mr-[50%] border-separate border-spacing-0 border-b border-1 border-color-4"
        style={{ ...columnSizeVars, width: api.getTotalSize() }}
      >
        {/* 列幅 */}
        <colgroup>
          {getLast(api.getHeaderGroups()).headers.map(header => (
            <col key={header.id} style={{ width: getColWidth(header) }} />
          ))}
        </colgroup>

        {/* ヘッダ */}
        <thead>
          {api.getHeaderGroups().map(headerGroup => (
            <tr key={headerGroup.id}>

              {headerGroup.headers.filter(h => !(h.column.columnDef as ColumnDefEx<T>).hidden).map(header => (
                <th key={header.id}
                  colSpan={header.colSpan}
                  className="relative overflow-hidden whitespace-nowrap px-1 py-0 text-start bg-color-3"
                  style={getThStickeyStyle(header)}>
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
              {row.getVisibleCells().filter(c => !(c.column.columnDef as ColumnDefEx<T>).hidden).map(cell => (
                <td key={cell.id}
                  ref={td => tdRefCallback(td, cell)}
                  className="relative overflow-hidden p-0 border-r border-1 border-color-4"
                  style={getTdStickeyStyle(cell)}
                  onMouseDown={e => selectObject({ cell, shiftKey: e.shiftKey })}
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

      {isActive && !editing && (
        <ActiveCellBorder api={api} {...activeCellBorderProps} />
      )}
      {editing && (
        <CellEditor onEndEditing={onEndEditing} {...cellEditorProps} />
      )}
    </div>
  )
})

// -----------------------------------------------
// 行列ヘッダ固定
const getThStickeyStyle = (header: RT.Header<any, unknown>): React.CSSProperties => ({
  position: 'sticky',
  top: 0,
  left: header.column.id === ROW_HEADER_ID ? 0 : undefined,
  zIndex: header.column.id === ROW_HEADER_ID ? TABLE_ZINDEX.ROWHEADER_THEAD : TABLE_ZINDEX.THEAD,
})
const getTdStickeyStyle = (cell: RT.Cell<any, unknown>): React.CSSProperties => ({
  position: cell.column.id === ROW_HEADER_ID ? 'sticky' : undefined,
  left: cell.column.id === ROW_HEADER_ID ? 0 : undefined,
  zIndex: cell.column.id === ROW_HEADER_ID ? TABLE_ZINDEX.ROWHEADER : undefined,
})

// -----------------------------------------------
// 配列の最後の要素を返す。配列の要素数が0の場合は考慮していない。
const getLast = <T,>(arr: T[]): T => {
  return arr[arr.length - 1]
}

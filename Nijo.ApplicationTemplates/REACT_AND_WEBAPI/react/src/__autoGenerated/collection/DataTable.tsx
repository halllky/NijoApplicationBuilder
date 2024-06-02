import React, { useCallback, useImperativeHandle, useMemo, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import * as Util from '../util'
import { ColumnDefEx, DataTableProps, DataTableRef } from './DataTable.Public'
import { TABLE_ZINDEX } from './DataTable.Parts'
import { useCellEditing } from './DataTable.Editing'
import { useSelection } from './DataTable.Selecting'
import { getColumnResizeOption, useColumnResizing } from './DataTable.ColResize'

export * from './DataTable.Public'

export const DataTable = Util.forwardRefEx(<T,>(props: DataTableProps<T>, ref: React.ForwardedRef<DataTableRef<T>>) => {
  const {
    data,
    columns: propsColumns,
    onKeyDown: propsKeyDown,
    onActiveRowChanged,
    className,
  } = props

  // 列
  const columnHelper = useMemo(() => RT.createColumnHelper<T>(), [])
  const columns: RT.ColumnDef<T>[] = useMemo(() => {
    const result: RT.ColumnDef<T>[] = []
    const colgroups = Util.groupBy(propsColumns ?? [], col => col.headerGroupName ?? '')
    for (const [header, columns] of colgroups) {
      if (header) {
        result.push(columnHelper.group({ header, columns }))
      } else {
        result.push(...columns)
      }
    }
    return result
  }, [propsColumns, columnHelper])

  // 表
  const optoins = useMemo((): RT.TableOptions<T> => ({
    data: data ?? [],
    columns,
    getCoreRowModel: RT.getCoreRowModel(),
    ...getColumnResizeOption(),
  }), [data, columns])

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
  } = useSelection<T>(api, data?.length ?? 0, columns.length, onActiveRowChanged)

  const {
    columnSizeVars,
    getColWidth,
    ResizeHandler,
  } = useColumnResizing(api)

  const tdRefCallback = (td: HTMLTableCellElement | null, cell: RT.Cell<T, unknown>) => {
    caretTdRefCallback(td, cell)
    editingTdRefCallback(td, cell)
  }

  const [isActive, setIsActive] = useState(false)
  const handleFocus: React.FocusEventHandler<HTMLDivElement> = useCallback(() => {
    setIsActive(true)
    if (!caretCell) selectObject({ target: 'any' })
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

    // 任意の操作
    if (propsKeyDown) {
      propsKeyDown(e)
      if (e.defaultPrevented) return
    }

    // 選択に関する操作
    handleSelectionKeyDown(e)
    if (e.defaultPrevented) return

    if (e.key === ' ') {
      for (const row of getSelectedRows()) row.toggleExpanded()
      e.preventDefault()
      return
    } else if (caretCell
      && (e.key === 'F2'
        || !e.ctrlKey && !e.metaKey && e.key.length === 1 /*文字や数字や記号の場合*/)
    ) {
      // caretCellは更新前の古いセルなので最新の配列から検索しなおす
      const row = api.getCoreRowModel().flatRows[caretCell.rowIndex]
      const cell = row.getAllCells().find(cell => cell.column.id === caretCell.colId)
      if (cell) startEditing(cell)
      e.preventDefault()
      return
    }
  }, [api, editing, caretCell, getSelectedRows, handleSelectionKeyDown, startEditing, cancelEditing, propsKeyDown])

  useImperativeHandle(ref, () => ({
    getSelectedRows: () => getSelectedRows().map(row => ({
      row: row.original,
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
                  style={getThStickeyStyle(false)}>
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
                  style={getTdStickeyStyle(false)}
                  onMouseDown={e => selectObject({ target: 'cell', cell: { rowIndex: cell.row.index, colId: cell.column.id }, shiftKey: e.shiftKey })}
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
const getThStickeyStyle = (isTopLeftCell: boolean): React.CSSProperties => ({
  position: 'sticky',
  top: 0,
  left: isTopLeftCell ? 0 : undefined,
  zIndex: isTopLeftCell ? TABLE_ZINDEX.ROWHEADER_THEAD : TABLE_ZINDEX.THEAD,
})
const getTdStickeyStyle = (isRowHeader: boolean): React.CSSProperties => ({
  position: isRowHeader ? 'sticky' : undefined,
  left: isRowHeader ? 0 : undefined,
  zIndex: isRowHeader ? TABLE_ZINDEX.ROWHEADER : undefined,
})

// -----------------------------------------------
// 配列の最後の要素を返す。配列の要素数が0の場合は考慮していない。
const getLast = <T,>(arr: T[]): T => {
  return arr[arr.length - 1]
}

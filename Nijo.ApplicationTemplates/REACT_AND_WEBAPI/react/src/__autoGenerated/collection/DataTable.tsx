import React, { useCallback, useImperativeHandle, useMemo, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import * as Util from '../util'
import { ColumnDefEx, DataTableProps, DataTableRef } from './DataTable.Public'
import { TABLE_ZINDEX, CellEditorRef } from './DataTable.Parts'
import { CellEditor } from './DataTable.Editing'
import { useSelection } from './DataTable.Selecting'
import { getColumnResizeOption, useColumnResizing } from './DataTable.ColResize'

export * from './DataTable.Public'

export const DataTable = Util.forwardRefEx(<T,>(props: DataTableProps<T>, ref: React.ForwardedRef<DataTableRef<T>>) => {
  const {
    data,
    columns: propsColumns,
    onKeyDown: propsKeyDown,
    onActiveRowChanged,
    onChangeRow,
    className,
    hideHeader,
    tableWidth,
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
    ...(tableWidth === 'fit' ? {} : getColumnResizeOption()),
  }), [data, columns])

  const api = RT.useReactTable(optoins)
  const cellEditorRef = useRef<CellEditorRef<T>>(null)
  const [editing, setEditing] = useState(false)

  const {
    caretCell,
    caretTdRef,
    selectObject,
    handleSelectionKeyDown,
    caretTdRefCallback,
    ActiveCellBorder,
    activeCellBorderProps,
    getSelectedRows,
    getSelectedIndexes,
  } = useSelection<T>(api, data?.length ?? 0, columns.length, onActiveRowChanged, cellEditorRef)

  const {
    columnSizeVars,
    getColWidth,
    ResizeHandler,
  } = useColumnResizing(api)

  const [isActive, setIsActive] = useState(false)
  const handleFocus: React.FocusEventHandler<HTMLDivElement> = useCallback(() => {
    setIsActive(true)
    cellEditorRef.current?.focus()
    if (!caretCell) selectObject({ target: 'any' })
  }, [api, caretCell, selectObject])
  const handleBlur: React.FocusEventHandler<HTMLDivElement> = useCallback(e => {
    // フォーカスの移動先がこの要素の中にある場合はfalseにしない
    if (!e.target.contains(e.relatedTarget)) setIsActive(false)
  }, [])

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useCallback(e => {
    // 任意の操作
    if (propsKeyDown) {
      propsKeyDown(e)
      if (e.defaultPrevented) return
    }
    // 選択に関する操作
    handleSelectionKeyDown(e)
    if (e.defaultPrevented) return
  }, [handleSelectionKeyDown, propsKeyDown])

  useImperativeHandle(ref, () => ({
    getSelectedRows: () => getSelectedRows().map(row => ({
      row: row.original,
      rowIndex: row.index,
    })),
    getSelectedIndexes,
  }), [getSelectedRows])

  return (
    <div
      className={`outline-none overflow-x-auto overflow-y-scroll select-none relative bg-color-2 border border-1 border-color-4 z-0 ${className}`}
      onFocus={handleFocus}
      onBlur={handleBlur}
      tabIndex={0}
    >
      <table
        className="border-separate border-spacing-0 border-b border-1 border-color-4"
        style={{
          ...columnSizeVars,
          marginRight: tableWidth !== 'fit' ? '50%' : undefined,
          width: tableWidth !== 'fit' ? api.getTotalSize() : '100%',
        }}
      >
        {/* 列幅 */}
        {tableWidth !== 'fit' && (
          <colgroup>
            {getLast(api.getHeaderGroups()).headers.map(header => (
              <col key={header.id} style={{ width: getColWidth(header) }} />
            ))}
          </colgroup>
        )}

        {/* ヘッダ */}
        {!hideHeader && (
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
                    {tableWidth !== 'fit' && (
                      <ResizeHandler header={header} />
                    )}
                  </th>
                ))}

              </tr>
            ))}
          </thead>
        )}

        {/* ボディ */}
        <tbody className="bg-color-0">
          {api.getRowModel().flatRows.map(row => (
            <tr
              key={row.id}
              className="leading-tight"
            >
              {row.getVisibleCells().filter(c => !(c.column.columnDef as ColumnDefEx<T>).hidden).map(cell => (
                <td key={cell.id}
                  ref={td => caretTdRefCallback(td, cell)}
                  className="relative overflow-hidden p-0 border-r border-1 border-color-4"
                  style={getTdStickeyStyle(false)}
                  onMouseDown={e => selectObject({ target: 'cell', cell: { rowIndex: cell.row.index, colId: cell.column.id }, shiftKey: e.shiftKey })}
                  onDoubleClick={() => cellEditorRef.current?.startEditing(cell)}
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

      <CellEditor
        ref={cellEditorRef}
        api={api}
        caretCell={caretCell}
        caretTdRef={caretTdRef}
        onChangeEditing={setEditing}
        onKeyDown={handleKeyDown}
        onChangeRow={onChangeRow}
      />
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

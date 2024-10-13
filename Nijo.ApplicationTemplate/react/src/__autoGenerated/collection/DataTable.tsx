import React, { useCallback, useImperativeHandle, useLayoutEffect, useMemo, useRef, useState } from 'react'
import useEvent from 'react-use-event-hook'
import * as RT from '@tanstack/react-table'
import * as Util from '../util'
import { DataTableProps, DataTableRef } from './DataTable.Public'
import { TABLE_ZINDEX, CellEditorRef, RTColumnDefEx, CellPosition } from './DataTable.Parts'
import { CellEditor } from './DataTable.Editing'
import { ActiveCellBorder, SelectTarget, useSelection } from './DataTable.Selecting'
import { getColumnResizeOption, useColumnResizing } from './DataTable.ColResize'
import { useCopyPaste } from './DataTable.CopyPaste'

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
    const rtColumns: RTColumnDefEx<T>[] = propsColumns?.map((c, ix) => ({
      id: `col-${ix}`,
      size: c.defaultWidthPx,
      enableResizing: !c.fixedWidth,
      header: c.header ?? '',
      cell: cellProps => c.render(cellProps.row.original, cellProps.row.index),
      ex: c,
    })) ?? []
    const colgroups = Util.groupBy(rtColumns, col => col.ex.headerGroupName ?? '')
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
  }), [tableWidth, data, columns])

  const api = RT.useReactTable(optoins)
  const cellEditorRef = useRef<CellEditorRef<T>>(null)
  const [editing, setEditing] = useState(false)

  // <td>のrefの二重配列
  const tdRefs = useRef<React.RefObject<HTMLTableCellElement>[][]>([])
  useLayoutEffect(() => {
    tdRefs.current = data?.map(() =>
      Array.from({ length: propsColumns?.length ?? 0 }).map(() => React.createRef())
    ) ?? []
  }, [data, propsColumns?.length, tdRefs])

  const [caretCell, setCaretCell] = React.useState<CellPosition | undefined>()
  const onCaretCellChanged = useEvent((cell: CellPosition | undefined) => {
    setCaretCell(cell)
  })
  const {
    caretTdRef,
    selectObject,
    handleSelectionKeyDown,
    activeCellBorderProps,
    getSelectedRows,
    getSelectedColumns,
  } = useSelection<T>(api, data?.length ?? 0, columns, tdRefs, onActiveRowChanged, onCaretCellChanged, cellEditorRef)

  const {
    columnSizeVars,
    getColWidth,
    ResizeHandler,
  } = useColumnResizing(api, columns)

  const {
    onCopy,
    onPaste,
    clearSelectedRange,
  } = useCopyPaste(api, getSelectedRows, getSelectedColumns, onChangeRow, editing)

  const [isActive, setIsActive] = useState(false)
  const handleFocus: React.FocusEventHandler<HTMLDivElement> = useEvent(() => {
    setIsActive(true)
    cellEditorRef.current?.focus()
    if (!caretCell) selectObject({ target: 'any' })
  })
  const handleBlur: React.FocusEventHandler<HTMLDivElement> = useEvent(e => {
    // フォーカスの移動先がこの要素の中にある場合はfalseにしない
    if (!e.target.contains(e.relatedTarget)) setIsActive(false)
  })

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useEvent(e => {
    // 任意の操作
    if (propsKeyDown) {
      propsKeyDown(e)
      if (e.defaultPrevented) return
    }
    // 選択に関する操作
    handleSelectionKeyDown(e)
    if (e.defaultPrevented) return
    // 選択範囲内クリア
    if (!e.ctrlKey && !e.metaKey && e.key === 'Delete') {
      clearSelectedRange()
      e.preventDefault()
    }
  })

  const divRef = useRef<HTMLDivElement>(null)
  const startEditing = useEvent(() => {
    if (!caretCell || !cellEditorRef.current) return
    const row = api.getCoreRowModel().flatRows[caretCell.rowIndex]
    const cell = row.getAllCells()[caretCell.colIndex]
    if (cell) cellEditorRef.current.startEditing(cell)
  })
  useImperativeHandle(ref, () => ({
    focus: () => divRef.current?.focus(),
    getSelectedRows: () => getSelectedRows().map(row => ({
      row: row.original,
      rowIndex: row.index,
    })),
    startEditing,
  }), [getSelectedRows, divRef, startEditing])

  return (
    <div
      ref={divRef}
      className={`outline-none overflow-x-auto overflow-y-scroll select-none relative bg-color-gutter z-0 ${className}`}
      onFocus={handleFocus}
      onBlur={handleBlur}
      onCopy={onCopy}
      onPaste={onPaste}
      tabIndex={0}
    >
      <table
        className="border-separate border-spacing-0"
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
              <col key={header.id} style={{ width: getColWidth(header.column) }} />
            ))}
          </colgroup>
        )}

        {/* ヘッダ */}
        {!hideHeader && (
          <thead>
            {api.getHeaderGroups().map((headerGroup, thY) => (
              <tr key={headerGroup.id}>

                {headerGroup.headers.map((header, thX) => (
                  <th key={header.id}
                    colSpan={header.colSpan}
                    className="relative bg-color-2 text-color-7 border-b border-color-3"
                    style={getThStyle(false, thX, thY)}>
                    <div
                      className="absolute inset-0 p-px text-sm text-start whitespace-nowrap text-ellipsis overflow-hidden"
                      title={typeof header.column.columnDef.header === 'string' ? header.column.columnDef.header : undefined}
                    >
                      {!header.isPlaceholder && RT.flexRender(
                        header.column.columnDef.header,
                        header.getContext())}
                    </div>
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
          {api.getRowModel().flatRows.map((row, rowIndex) => (
            <tr
              key={row.id}
              className="leading-tight"
            >
              {row.getVisibleCells().map((cell, colIndex) => (
                <MemorizedTd key={cell.id}
                  ref={tdRefs.current[rowIndex]?.[colIndex]}
                  cell={cell}
                  cellEditorRef={cellEditorRef}
                  getColWidth={getColWidth}
                  selectObject={selectObject}
                  colIndex={colIndex}
                />
              ))}

            </tr>
          ))}
        </tbody>
      </table>

      {/* 末尾の行をスクロールエリア内の最上部までスクロールできるようにするための余白。 */}
      {/* 4remは ヘッダ2行 + ボディ1行 + スクロールバー の縦幅のおおよその合計。 */}
      <div className="h-[calc(100%-4rem)]"></div>

      <ActiveCellBorder hidden={!isActive || editing} {...activeCellBorderProps} />

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

type MemorizedCellArgs<T> = {
  cell: RT.Cell<T, unknown>
  getColWidth: (column: RT.Column<T, unknown>) => string
  selectObject: (obj: SelectTarget) => void
  cellEditorRef: React.RefObject<CellEditorRef<T>>
  colIndex: number
}
type MemorizedCellComponent = <T>(props: MemorizedCellArgs<T> & { ref: React.Ref<HTMLTableCellElement> }) => JSX.Element

const MemorizedTd: MemorizedCellComponent = React.memo(React.forwardRef(<T,>({
  cell,
  getColWidth,
  selectObject,
  cellEditorRef,
  colIndex,
}: MemorizedCellArgs<T>, ref: React.ForwardedRef<HTMLTableCellElement>) => {
  return (
    <td
      ref={ref}
      className="relative overflow-hidden align-top p-0 border-r border-b border-1 border-color-3"
      style={{ ...getTdStickeyStyle(false), maxWidth: getColWidth(cell.column) }}
      onMouseDown={e => selectObject({ target: 'cell', cell: { rowIndex: cell.row.index, colIndex }, shiftKey: e.shiftKey })}
      onDoubleClick={() => cellEditorRef.current?.startEditing(cell)}
    >
      {RT.flexRender(
        cell.column.columnDef.cell,
        cell.getContext())}
    </td>
  )
}), (prev, next) => {
  return Object.is(prev.cell.row.original, next.cell.row.original)
    && Object.is(prev.getColWidth, next.getColWidth)
    && Object.is(prev.selectObject, next.selectObject)
    && Object.is(prev.cellEditorRef, next.cellEditorRef)
    && Object.is(prev.colIndex, next.colIndex)
}) as <T>(props: MemorizedCellArgs<T>) => JSX.Element

// -----------------------------------------------
// 行列ヘッダ固定
const getThStyle = (isTopLeftCell: boolean, x: number, y: number): React.CSSProperties => ({
  position: 'sticky',
  top: `calc((1rem + 8px) * ${y})`,
  left: isTopLeftCell ? 0 : undefined,
  height: x === 0 ? 'calc(1rem + 8px)' : undefined,
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

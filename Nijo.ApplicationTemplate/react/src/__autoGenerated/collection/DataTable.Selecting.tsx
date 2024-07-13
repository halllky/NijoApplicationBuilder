import React, { useState, useRef, useCallback, useEffect, useMemo, useImperativeHandle, useLayoutEffect } from 'react'
import * as RT from '@tanstack/react-table'
import * as Util from '../util'
import { CellEditorRef, CellPosition, TABLE_ZINDEX } from './DataTable.Parts'
import { ColumnDefEx, DataTableProps } from '..'

export type SelectTarget
  = { target: 'cell', cell: CellPosition, shiftKey: boolean }
  | { target: 'any' }
  | { target: 'all' }

export const useSelection = <T,>(
  api: RT.Table<T>,
  rowCount: number,
  columns: RT.ColumnDef<T>[],
  tdRefs: React.MutableRefObject<React.RefObject<HTMLTableCellElement>[][]>,
  onActiveRowChanged: DataTableProps<T>['onActiveRowChanged'] | undefined,
  cellEditorRef: React.RefObject<CellEditorRef<T>>,
) => {
  const colCount = columns.length

  const caretCell = useRef<CellPosition | undefined>()
  const selectionStart = useRef<CellPosition | undefined>()
  const [containsRowHeader, setContainsRowHeader] = useState(false)

  // <td>への参照
  const caretTdRef = useRef<HTMLTableCellElement>()
  const selectionStartTdRef = useRef<HTMLTableCellElement>()
  useLayoutEffect(() => {
    if (caretCell.current) {
      const colIndex = columns.findIndex(col => col.id === caretCell.current?.colId)
      caretTdRef.current = tdRefs.current[caretCell.current.rowIndex]?.[colIndex]?.current ?? undefined
    } else {
      caretTdRef.current = undefined
    }
  }, [caretCell.current, columns, tdRefs])

  useLayoutEffect(() => {
    if (selectionStart.current) {
      const colIndex = columns.findIndex(col => col.id === selectionStart.current?.colId)
      selectionStartTdRef.current = tdRefs.current[selectionStart.current.rowIndex]?.[colIndex]?.current ?? undefined
    } else {
      selectionStartTdRef.current = undefined
    }
  }, [selectionStart.current, columns, tdRefs])

  /** 選択 */
  const selectObject = useCallback((obj: SelectTarget) => {
    // シングル選択
    if (obj.target === 'cell') {
      selectionStart.current = { ...obj.cell }
      if (!obj.shiftKey) caretCell.current = obj.cell
      setContainsRowHeader(false)
      onActiveRowChanged?.({ rowIndex: obj.cell.rowIndex, getRow: () => api.getCoreRowModel().flatRows[obj.cell.rowIndex].original })
    }
    // 何か選択
    else if (obj.target === 'any') {
      if (rowCount > 0 && colCount >= 0) {
        const selected: CellPosition = { rowIndex: 0, colId: api.getAllColumns()[0].id }
        caretCell.current = selected
        selectionStart.current = selected
        setContainsRowHeader(false)
        onActiveRowChanged?.({ rowIndex: 0, getRow: () => api.getCoreRowModel().flatRows[0].original })

      } else {
        caretCell.current = undefined
        selectionStart.current = undefined
        setContainsRowHeader(false)
        onActiveRowChanged?.(undefined)
      }

    }
    // 全選択
    else {
      if (rowCount > 0 && colCount >= 0) {
        const columns = api.getAllColumns()
        caretCell.current = { rowIndex: 0, colId: columns[0].id }
        selectionStart.current = { rowIndex: rowCount - 1, colId: columns[columns.length - 1].id }
        setContainsRowHeader(true)
        onActiveRowChanged?.({ rowIndex: 0, getRow: () => api.getCoreRowModel().flatRows[rowCount - 1].original })

      } else {
        caretCell.current = undefined
        selectionStart.current = undefined
        setContainsRowHeader(false)
        onActiveRowChanged?.(undefined)
      }
    }
    // クイック編集のために常にCellEditorにフォーカスを当てる
    cellEditorRef.current?.focus()
  }, [caretCell, selectionStart, api, onActiveRowChanged, rowCount, colCount])

  const handleSelectionKeyDown: React.KeyboardEventHandler<HTMLElement> = useCallback(e => {
    if (e.ctrlKey && e.key === 'a') {
      selectObject({ target: 'all' })
      e.preventDefault()

    } else if (!e.ctrlKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      const movingCell = e.shiftKey ? selectionStart.current : caretCell.current
      if (!movingCell) {
        selectObject({ target: 'any' })
        e.preventDefault()
        return
      }
      // 1つ上または下のセルを選択
      const rowIndex = e.key === 'ArrowUp'
        ? Math.max(0, movingCell.rowIndex - 1)
        : Math.min(rowCount - 1, movingCell.rowIndex + 1)
      selectObject({ target: 'cell', cell: { rowIndex, colId: movingCell.colId }, shiftKey: e.shiftKey })
      e.preventDefault()
      activeCellRef.current?.scrollToActiveCell()
      return

    } else if (!e.ctrlKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
      const movingCell = e.shiftKey ? selectionStart.current : caretCell.current
      if (!movingCell) {
        selectObject({ target: 'any' })
        e.preventDefault()
        return
      }
      // 1つ左または右のセルを選択
      const columns = api.getAllColumns()
      const currentColIndex = columns.findIndex(col => col.id === movingCell.colId)
      const newColIndex = e.key === 'ArrowLeft'
        ? Math.max(0, currentColIndex - 1)
        : Math.min(columns.length - 1, currentColIndex + 1)
      const newColumn = columns[newColIndex]
      selectObject({ target: 'cell', cell: { rowIndex: movingCell.rowIndex, colId: newColumn.id }, shiftKey: e.shiftKey })
      e.preventDefault()
      activeCellRef.current?.scrollToActiveCell()
    }
  }, [api, selectObject, caretCell, selectionStart, rowCount, colCount])

  const ActiveCellBorder = useMemo(() => {
    return prepareActiveRangeSvg<T>(caretTdRef, selectionStartTdRef)
  }, [])

  // 矢印キーでのセル移動時にアクティブセルを画面内に移るようにするためのもの
  const activeCellRef = useRef<{ scrollToActiveCell: () => void }>(null)

  const getSelectedRows = useCallback(() => {
    if (!caretCell.current || !selectionStart.current) return []
    const flatRows = api.getRowModel().flatRows
    const since = Math.min(caretCell.current.rowIndex, selectionStart.current.rowIndex)
    const until = Math.max(caretCell.current.rowIndex, selectionStart.current.rowIndex)
    return flatRows.slice(since, until + 1)
  }, [api, caretCell, selectionStart])

  const getSelectedColumns = useCallback(() => {
    if (!caretCell.current || !selectionStart.current) return []
    const allColumns = api.getAllColumns()
    const caretCellColIndex = allColumns.findIndex(c => c.id === caretCell.current!.colId)
    const selectionStartColIndex = allColumns.findIndex(c => c.id === selectionStart.current!.colId)
    if (caretCellColIndex === -1 || selectionStartColIndex === -1) return []
    const since = Math.min(caretCellColIndex, selectionStartColIndex)
    const until = Math.max(caretCellColIndex, selectionStartColIndex)
    return allColumns.slice(since, until + 1).map(c => c.columnDef as ColumnDefEx<T>)
  }, [api, caretCell, selectionStart])

  return {
    caretCell,
    caretTdRef,
    selectObject,
    handleSelectionKeyDown,

    ActiveCellBorder,
    activeCellBorderProps: {
      caretCell,
      selectionStart,
      containsRowHeader,
      ref: activeCellRef,
    },

    getSelectedRows,
    getSelectedColumns,
  }
}


function prepareActiveRangeSvg<T>(
  caretTdRef: React.MutableRefObject<HTMLTableCellElement | undefined>,
  selectionStartTdRef: React.MutableRefObject<HTMLTableCellElement | undefined>,
) {
  return Util.forwardRefEx<{
    scrollToActiveCell: () => void,
  }, {
    caretCell: React.RefObject<CellPosition | undefined>
    selectionStart: React.RefObject<CellPosition | undefined>
    containsRowHeader: boolean
    api: RT.Table<T>
  }>(({ caretCell, selectionStart, containsRowHeader, api }, ref) => {
    const svgRef = useRef<SVGSVGElement>(null)
    const maskBlackRef = useRef<SVGRectElement>(null)
    useEffect(() => {
      if (!svgRef.current || !maskBlackRef.current) return

      const head = caretTdRef.current
      const root = selectionStartTdRef.current
      if (!head || !root) {
        svgRef.current.style.display = 'none'
        return
      }
      svgRef.current.style.display = ''

      const left = Math.min(
        head.offsetLeft,
        root.offsetLeft)
      const right = Math.max(
        head.offsetLeft + head.offsetWidth,
        root.offsetLeft + root.offsetWidth)
      const top = Math.min(
        head.offsetTop,
        root.offsetTop)
      const bottom = Math.max(
        head.offsetTop + head.offsetHeight,
        root.offsetTop + root.offsetHeight)

      svgRef.current.style.left = `${left}px`
      svgRef.current.style.top = `${top}px`
      svgRef.current.style.width = `${right - left}px`
      svgRef.current.style.height = `${bottom - top}px`

      maskBlackRef.current.setAttribute('x', `${head.offsetLeft - left - 3}px`) // 3はボーダーの分
      maskBlackRef.current.setAttribute('y', `${head.offsetTop - top - 3}px`) // 3はボーダーの分
      maskBlackRef.current.style.width = `${head.offsetWidth}px`
      maskBlackRef.current.style.height = `${head.offsetHeight}px`

      svgRef.current.style.zIndex = containsRowHeader
        ? TABLE_ZINDEX.ROWHEADER_SELECTION.toString()
        : TABLE_ZINDEX.SELECTION.toString()
    }, [
      containsRowHeader,
      caretCell.current,
      selectionStart.current,
      // 列幅変更時に範囲を再計算するため必要な依存
      // eslint-disable-next-line react-hooks/exhaustive-deps
      api.getState().columnSizing,
      // 行の折りたたみ変更時に範囲を再計算するため必要な依存
      // eslint-disable-next-line react-hooks/exhaustive-deps
      api.getState().expanded,
    ])

    useImperativeHandle(ref, () => ({
      scrollToActiveCell: () => {
        setTimeout(() => {
          svgRef.current?.scrollIntoView({
            behavior: 'instant',
            block: 'nearest',
            inline: 'nearest',
          })
        }, 10)
      }
    }))

    return (
      <svg ref={svgRef}
        version="1.1"
        xmlns="http://www.w3.org/2000/svg"
        xmlnsXlink="http://www.w3.org/1999/xlink"
        className="pointer-events-none absolute outline outline-2 outline-offset-[-2px] border-[3px] border border-color-0"
      >
        <defs>
          <mask id="selection-start-mask">
            <rect fill="white" x="0" y="0" width="calc(Infinity)" height="calc(Infinity)" />
            <rect ref={maskBlackRef} fill="black" />
          </mask>
        </defs>
        <rect
          x="0" y="0" width="100%" height="100%"
          className="bg-color-selected-svg"
          mask="url(#selection-start-mask)"
        />
      </svg>
    )
  })
}

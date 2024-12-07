import React, { useState, useRef, useCallback, useImperativeHandle, useMemo, useId } from 'react'
import useEvent from 'react-use-event-hook'
import * as RT from '@tanstack/react-table'
import * as Util from '../util'
import { CellEditorRef, CellPosition, GetPixelFunction, RTColumnDefEx, TABLE_ZINDEX } from './DataTable.Parts'
import { DataTableProps } from '..'

export type SelectTarget
  = { target: 'cell', cell: CellPosition, shiftKey: boolean }
  | { target: 'row', startRowIndex: number, endRowIndex: number }
  | { target: 'any' }
  | { target: 'all' }

export const useSelection = <T,>(
  api: RT.Table<T>,
  rowCount: number,
  columns: RT.ColumnDef<T>[],
  onActiveRowChanged: DataTableProps<T>['onActiveRowChanged'] | undefined,
  onCaretCellChanged: (cell: CellPosition | undefined) => void,
  cellEditorRef: React.RefObject<CellEditorRef<T>>,
) => {
  const colCount = columns.length

  const activeCellRef = useRef<ActiveRangeRef>(null)

  const caretCell = useRef<CellPosition | undefined>()
  const selectionStart = useRef<CellPosition | undefined>()
  const [containsRowHeader, setContainsRowHeader] = useState(false)

  // -----------------------------------------
  // 選択
  const selectObject = useEvent((obj: SelectTarget) => {
    // シングル選択
    if (obj.target === 'cell') {
      selectionStart.current = { ...obj.cell }
      if (!obj.shiftKey) {
        caretCell.current = { ...obj.cell }
        onCaretCellChanged(caretCell.current)
      }
      setContainsRowHeader(false)
      onActiveRowChanged?.({ rowIndex: obj.cell.rowIndex, getRow: () => api.getCoreRowModel().flatRows[obj.cell.rowIndex].original })
    }
    // 行選択
    else if (obj.target === 'row') {
      selectionStart.current = { colIndex: api.getAllLeafColumns().length - 1, rowIndex: obj.endRowIndex }
      caretCell.current = { colIndex: 0, rowIndex: obj.startRowIndex }
      onCaretCellChanged(caretCell.current)
      setContainsRowHeader(true)
      onActiveRowChanged?.({ rowIndex: obj.startRowIndex, getRow: () => api.getCoreRowModel().flatRows[obj.startRowIndex].original })
    }
    // 何か選択
    else if (obj.target === 'any') {
      if (rowCount > 0 && colCount >= 0) {
        const selected: CellPosition = { rowIndex: 0, colIndex: 0 }
        caretCell.current = selected
        selectionStart.current = selected
        setContainsRowHeader(false)
        onActiveRowChanged?.({ rowIndex: 0, getRow: () => api.getCoreRowModel().flatRows[0].original })
        onCaretCellChanged(selected)

      } else {
        caretCell.current = undefined
        selectionStart.current = undefined
        setContainsRowHeader(false)
        onActiveRowChanged?.(undefined)
        onCaretCellChanged(undefined)
      }

    }
    // 全選択
    else {
      if (rowCount > 0 && colCount >= 0) {
        const columns = api.getAllLeafColumns()
        caretCell.current = { rowIndex: 0, colIndex: 0 }
        selectionStart.current = { rowIndex: rowCount - 1, colIndex: columns.length - 1 }
        setContainsRowHeader(true)
        onActiveRowChanged?.({ rowIndex: 0, getRow: () => api.getCoreRowModel().flatRows[rowCount - 1].original })
        onCaretCellChanged(caretCell.current)

      } else {
        caretCell.current = undefined
        selectionStart.current = undefined
        setContainsRowHeader(false)
        onActiveRowChanged?.(undefined)
        onCaretCellChanged(undefined)
      }
    }
    // 選択範囲の表示のアップデート
    activeCellRef.current?.update(caretCell.current, selectionStart.current, containsRowHeader)
    // クイック編集のために常にCellEditorにフォーカスを当てる
    cellEditorRef.current?.focus()
  })

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
      selectObject({ target: 'cell', cell: { rowIndex, colIndex: movingCell.colIndex }, shiftKey: e.shiftKey })
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
      const columns = api.getAllLeafColumns()
      const colIndex = e.key === 'ArrowLeft'
        ? Math.max(0, movingCell.colIndex - 1)
        : Math.min(columns.length - 1, movingCell.colIndex + 1)
      selectObject({ target: 'cell', cell: { rowIndex: movingCell.rowIndex, colIndex }, shiftKey: e.shiftKey })
      e.preventDefault()
      activeCellRef.current?.scrollToActiveCell()
    }
  }, [api, selectObject, caretCell, selectionStart, rowCount, colCount, activeCellRef])

  const getSelectedRows = useCallback(() => {
    if (!caretCell.current || !selectionStart.current) return []
    const flatRows = api.getRowModel().flatRows
    const since = Math.min(caretCell.current.rowIndex, selectionStart.current.rowIndex)
    const until = Math.max(caretCell.current.rowIndex, selectionStart.current.rowIndex)
    return flatRows.slice(since, until + 1)
  }, [api, caretCell, selectionStart])

  const getSelectedColumns = useCallback(() => {
    if (!caretCell.current || !selectionStart.current) return []
    const allColumns = api.getAllLeafColumns()
    const caretCellColIndex = caretCell.current.colIndex
    const selectionStartColIndex = selectionStart.current.colIndex
    if (caretCellColIndex === -1 || selectionStartColIndex === -1) return []
    const since = Math.min(caretCellColIndex, selectionStartColIndex)
    const until = Math.max(caretCellColIndex, selectionStartColIndex)
    return allColumns.slice(since, until + 1).map(c => c.columnDef as RTColumnDefEx<T>)
  }, [api, caretCell, selectionStart])

  return {
    selectObject,
    handleSelectionKeyDown,
    activeCellRef,
    getSelectedRows,
    getSelectedColumns,
  }
}


type ActiveRangeRef = {
  /** 選択範囲の即時更新 */
  update: (caretCell: CellPosition | undefined, selectionStart: CellPosition | undefined, containsRowHeader: boolean) => void
  /** 選択範囲の再レンダリング */
  reRender: () => void
  scrollToActiveCell: () => void
}
type ActiveRangeProps = {
  getPixel: GetPixelFunction
  hidden: boolean
}

export const ActiveCellBorder = Util.forwardRefEx(({ getPixel, hidden }: ActiveRangeProps, ref: React.ForwardedRef<ActiveRangeRef>) => {
  const svgRef = useRef<SVGSVGElement>(null)
  const scrollTargetRef = useRef<SVGRectElement>(null)
  const [maskBlackProps, setMaskBlackProps] = useState<React.SVGProps<SVGRectElement>>({})
  const [scrollTargetProps, setScrollTargetProps] = useState<React.SVGProps<SVGRectElement>>({})
  const [svgPosition, setSvgPosition] = useState<React.CSSProperties>({})
  const [svgHidden, setSvgHidden] = useState(true)
  const svgStyle = useMemo((): React.CSSProperties => ({
    display: hidden || svgHidden ? 'none' : undefined,
    ...svgPosition,
  }), [hidden, svgHidden, svgPosition])

  /** 選択範囲の四角形の表示を更新する */
  const update: ActiveRangeRef['update'] = useCallback((caretCell, selectionStart, containsRowHeader) => {
    setCache([caretCell, selectionStart, containsRowHeader])

    const head = caretCell
    const root = selectionStart
    if (!head || !root) {
      setSvgHidden(true)
      return
    }
    // 選択範囲全体の矩形のサイズ
    const left = getPixel({ position: 'left', colIndex: Math.min(head.colIndex, root.colIndex) })
    const right = getPixel({ position: 'right', colIndex: Math.max(head.colIndex, root.colIndex) })
    const top = getPixel({ position: 'top', rowIndex: Math.min(head.rowIndex, root.rowIndex) })
    const bottom = getPixel({ position: 'bottom', rowIndex: Math.max(head.rowIndex, root.rowIndex) })

    // headの矩形のサイズ
    const headOffsetLeft = getPixel({ position: 'left', colIndex: head.colIndex })
    const headOffsetTop = getPixel({ position: 'top', rowIndex: head.rowIndex })
    const headOffsetWidth = getPixel({ position: 'right', colIndex: head.colIndex }) - headOffsetLeft + 1
    const headOffsetHeight = getPixel({ position: 'bottom', rowIndex: head.rowIndex }) - headOffsetTop + 1

    // rootの矩形のサイズ
    const rootOffsetLeft = getPixel({ position: 'left', colIndex: root.colIndex })
    const rootOffsetTop = getPixel({ position: 'top', rowIndex: root.rowIndex })
    const rootOffsetWidth = getPixel({ position: 'right', colIndex: root.colIndex }) - rootOffsetLeft + 1
    const rootOffsetHeight = getPixel({ position: 'bottom', rowIndex: root.rowIndex }) - rootOffsetTop + 1

    setSvgHidden(false)
    setSvgPosition({
      left: `${left}px`,
      top: `${top}px`,
      width: `${right - left}px`,
      height: `${bottom - top}px`,
      zIndex: containsRowHeader
        ? TABLE_ZINDEX.ROWHEADER_SELECTION.toString()
        : TABLE_ZINDEX.SELECTION.toString(),
    })
    setMaskBlackProps({
      x: `${headOffsetLeft - left - 3}px`, // 3はボーダーの分
      y: `${headOffsetTop - top - 3}px`, // 3はボーダーの分
      width: `${headOffsetWidth}px`,
      height: `${headOffsetHeight}px`,
    })
    setScrollTargetProps({
      x: `${rootOffsetLeft - left - SCROLL_RECT_MARGIN.X - 3}px`, // 3はボーダーの分
      y: `${rootOffsetTop - top - SCROLL_RECT_MARGIN.Y - 3}px`, // 3はボーダーの分
      width: `${rootOffsetWidth + (SCROLL_RECT_MARGIN.X * 2)}px`,
      height: `${rootOffsetHeight + (SCROLL_RECT_MARGIN.Y * 2)}px`,
    })
  }, [getPixel, setSvgHidden, setSvgPosition, setMaskBlackProps])

  // virtualizerの更新時のための再レンダリング処理
  const [cache, setCache] = React.useState<Parameters<ActiveRangeRef['update']>>()
  const reRender = useEvent(() => {
    if (cache) update(...cache)
  })

  useImperativeHandle(ref, () => ({
    update,
    reRender,
    scrollToActiveCell: () => {
      scrollTargetRef.current?.scrollIntoView({
        behavior: 'instant',
        block: 'nearest',
        inline: 'nearest',
      })
    },
  }), [update, svgRef])

  // 1画面内にDataTableが複数あるとき、ほかのDataTableのmaskを参照してしまうのを避けるためのid
  const uniqueId = useId()

  return (
    <svg ref={svgRef}
      version="1.1"
      xmlns="http://www.w3.org/2000/svg"
      xmlnsXlink="http://www.w3.org/1999/xlink"
      className="pointer-events-none absolute outline outline-2 outline-offset-[-2px] border-[3px] border border-color-0"
      style={svgStyle}
    >
      <defs>
        <mask id={`selection-start-mask-${uniqueId}`}>
          <rect fill="white" x="0" y="0" width="calc(Infinity)" height="calc(Infinity)" />
          <rect fill="black" {...maskBlackProps} />
        </mask>
      </defs>
      <rect
        x="0" y="0" width="100%" height="100%"
        className="bg-color-selected-svg"
        mask={`url(#selection-start-mask-${uniqueId})`}
      />

      {/* カーソルキー移動で移動したセルの位置にくるrect。svrollIntoViewにはこの要素が画面内に収まるようにスクロールさせる */}
      <rect ref={scrollTargetRef} {...scrollTargetProps} fill="transparent" />
    </svg>
  )
})

// カーソルキーでセル移動したとき、scrollIntoViewは要素がぎりぎり画面内に収まる位置までスクロールするが、
// それだと選択範囲の表示がテーブルのヘッダやスクロールバーに隠れてしまうため、
// スクロール対象のrectのサイズを少し大きめにしておくことで選択範囲の表示が常に見える位置に表示されるようにする
const SCROLL_RECT_MARGIN = {
  X: 96,
  Y: 96,
}

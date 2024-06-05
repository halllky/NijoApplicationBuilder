import { useState, useRef, useCallback, useEffect, useMemo, useImperativeHandle } from 'react'
import * as RT from '@tanstack/react-table'
import * as Util from '../util'
import { CellEditorRef, TABLE_ZINDEX } from './DataTable.Parts'
import { DataTableProps } from '..'

export const useSelection = <T,>(
  api: RT.Table<T>,
  rowCount: number,
  colCount: number,
  onActiveRowChanged: DataTableProps<T>['onActiveRowChanged'] | undefined,
  cellEditorRef: React.RefObject<CellEditorRef>,
) => {
  const [caretCell, setCaretCell] = useState<CellPosition | undefined>()
  const [selectionStart, setSelectionStart] = useState<CellPosition | undefined>()
  const [containsRowHeader, setContainsRowHeader] = useState(false)
  const caretTdRef = useRef<HTMLTableCellElement>()
  const selectionStartTdRef = useRef<HTMLTableCellElement>()

  type SelectTarget
    = { target: 'cell', cell: CellPosition, shiftKey: boolean }
    | { target: 'any' }
    | { target: 'all' }

  const selectObject = useCallback((obj: SelectTarget) => {
    // シングル選択
    if (obj.target === 'cell') {
      setCaretCell({ ...obj.cell })
      if (!obj.shiftKey) setSelectionStart(obj.cell)
      setContainsRowHeader(false)
      onActiveRowChanged?.({ rowIndex: obj.cell.rowIndex, getRow: () => api.getCoreRowModel().flatRows[obj.cell.rowIndex].original })
    }
    // 何か選択
    else if (obj.target === 'any') {
      if (rowCount > 0 && colCount >= 0) {
        const selected: CellPosition = { rowIndex: 0, colId: api.getAllColumns()[0].id }
        setCaretCell(selected)
        setSelectionStart(selected)
        setContainsRowHeader(false)
        onActiveRowChanged?.({ rowIndex: 0, getRow: () => api.getCoreRowModel().flatRows[0].original })

      } else {
        setCaretCell(undefined)
        setSelectionStart(undefined)
        setContainsRowHeader(false)
        onActiveRowChanged?.(undefined)
      }

    }
    // 全選択
    else {
      if (rowCount > 0 && colCount >= 0) {
        const columns = api.getAllColumns()
        setCaretCell({ rowIndex: 0, colId: columns[0].id })
        setSelectionStart({ rowIndex: rowCount - 1, colId: columns[columns.length - 1].id })
        setContainsRowHeader(true)
        onActiveRowChanged?.({ rowIndex: 0, getRow: () => api.getCoreRowModel().flatRows[rowCount - 1].original })

      } else {
        setCaretCell(undefined)
        setSelectionStart(undefined)
        setContainsRowHeader(false)
        onActiveRowChanged?.(undefined)
      }
    }
    // クイック編集のために常にCellEditorにフォーカスを当てる
    cellEditorRef.current?.focus()
  }, [api, onActiveRowChanged, rowCount, colCount])

  const handleSelectionKeyDown: React.KeyboardEventHandler<HTMLElement> = useCallback(e => {
    if (e.ctrlKey && e.key === 'a') {
      selectObject({ target: 'all' })
      e.preventDefault()

    } else if (!e.ctrlKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      if (!caretCell) {
        selectObject({ target: 'any' })
        e.preventDefault()
        return
      }
      // 1つ上または下のセルを選択
      const rowIndex = e.key === 'ArrowUp'
        ? Math.max(0, caretCell.rowIndex - 1)
        : Math.min(rowCount - 1, caretCell.rowIndex + 1)
      selectObject({ target: 'cell', cell: { rowIndex, colId: caretCell.colId }, shiftKey: e.shiftKey })
      e.preventDefault()
      activeCellRef.current?.scrollToActiveCell()
      return

    } else if (!e.ctrlKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
      if (!caretCell) {
        selectObject({ target: 'any' })
        e.preventDefault()
        return
      }
      // 1つ左または右のセルを選択
      const columns = api.getAllColumns()
      const currentColIndex = columns.findIndex(col => col.id === caretCell.colId)
      const newColIndex = e.key === 'ArrowLeft'
        ? Math.max(0, currentColIndex - 1)
        : Math.min(columns.length - 1, currentColIndex + 1)
      const newColumn = columns[newColIndex]
      selectObject({ target: 'cell', cell: { rowIndex: caretCell.rowIndex, colId: newColumn.id }, shiftKey: e.shiftKey })
      e.preventDefault()
      activeCellRef.current?.scrollToActiveCell()
    }
  }, [api, selectObject, caretCell, rowCount, colCount])

  const caretTdRefCallback = useCallback((td: HTMLTableCellElement | null, cell: RT.Cell<T, unknown>) => {
    if (td !== null
      && cell.row.index === caretCell?.rowIndex
      && cell.column.id === caretCell.colId)
      caretTdRef.current = td
    if (td !== null
      && cell.row.index === selectionStart?.rowIndex
      && cell.column.id === selectionStart.colId)
      selectionStartTdRef.current = td
  }, [caretCell, selectionStart])

  const ActiveCellBorder = useMemo(() => {
    return prepareActiveRangeSvg<T>(caretTdRef, selectionStartTdRef)
  }, [])

  // 矢印キーでのセル移動時にアクティブセルを画面内に移るようにするためのもの
  const activeCellRef = useRef<{ scrollToActiveCell: () => void }>(null)

  const getSelectedRows = useCallback(() => {
    if (!caretCell || !selectionStart) return []
    const flatRows = api.getRowModel().flatRows
    const since = Math.min(caretCell.rowIndex, selectionStart.rowIndex)
    const until = Math.max(caretCell.rowIndex, selectionStart.rowIndex)
    return flatRows.slice(since, until + 1)
  }, [api, caretCell, selectionStart])

  const getSelectedIndexes = useCallback(() => {
    if (!caretCell || !selectionStart) return []
    const since = Math.min(caretCell.rowIndex, selectionStart.rowIndex)
    const until = Math.max(caretCell.rowIndex, selectionStart.rowIndex)
    return [...Array(until - since + 1)].map((_, i) => i + since)
  }, [api, caretCell, selectionStart])

  return {
    caretCell,
    selectObject,
    handleSelectionKeyDown,
    caretTdRefCallback,

    ActiveCellBorder,
    activeCellBorderProps: {
      caretCell,
      containsRowHeader,
      ref: activeCellRef,
    },

    getSelectedRows,
    getSelectedIndexes,
  }
}


function prepareActiveRangeSvg<T>(
  caretTdRef: React.MutableRefObject<HTMLTableCellElement | undefined>,
  selectionStartTdRef: React.MutableRefObject<HTMLTableCellElement | undefined>,
) {
  return Util.forwardRefEx<{
    scrollToActiveCell: () => void,
  }, {
    caretCell: object | undefined
    containsRowHeader: boolean
    api: RT.Table<T>
  }>(({ caretCell, containsRowHeader, api }, ref) => {
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

      maskBlackRef.current.setAttribute('x', `${root.offsetLeft - left - 3}px`) // 3はボーダーの分
      maskBlackRef.current.setAttribute('y', `${root.offsetTop - top - 3}px`) // 3はボーダーの分
      maskBlackRef.current.style.width = `${root.offsetWidth}px`
      maskBlackRef.current.style.height = `${root.offsetHeight}px`

      svgRef.current.style.zIndex = containsRowHeader
        ? TABLE_ZINDEX.ROWHEADER_SELECTION.toString()
        : TABLE_ZINDEX.SELECTION.toString()
    }, [
      containsRowHeader,
      caretCell,
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

type CellPosition = {
  rowIndex: number
  colId: string
}

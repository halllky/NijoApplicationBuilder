import { useState, useRef, useCallback, useEffect, useMemo } from 'react'
import * as RT from '@tanstack/react-table'
import * as Tree from '../util'
import { ROW_HEADER_ID, TABLE_ZINDEX } from './DataTable.Parts'

export const useSelection = <T,>(editing: boolean, api: RT.Table<Tree.TreeNode<T>>) => {
  const [caretCell, setCaretCell] = useState<RT.Cell<Tree.TreeNode<T>, unknown> | undefined>()
  const [selectionStart, setSelectionStart] = useState<RT.Cell<Tree.TreeNode<T>, unknown> | undefined>()
  const [containsRowHeader, setContainsRowHeader] = useState(false)
  const caretTdRef = useRef<HTMLTableCellElement>()
  const selectionStartTdRef = useRef<HTMLTableCellElement>()

  type SelectTarget
    = { cell: RT.Cell<Tree.TreeNode<T>, unknown>, shiftKey: boolean }
    | { any: RT.Table<Tree.TreeNode<T>>, cell?: undefined }
    | { all: RT.Table<Tree.TreeNode<T>>, cell?: undefined, any?: undefined }

  const selectObject = useCallback((obj: SelectTarget) => {
    if (obj.cell) {
      if (obj.cell.column.id === ROW_HEADER_ID) {
        // シングル選択（行ヘッダが選択された場合）
        const visibleCells = obj.cell.row.getVisibleCells()
        if (visibleCells.length === 0) return
        setCaretCell(visibleCells[0])
        if (!obj.shiftKey) setSelectionStart(visibleCells[visibleCells.length - 1])
        setContainsRowHeader(true)

      } else {
        // シングル選択
        setCaretCell(obj.cell)
        if (!obj.shiftKey) setSelectionStart(obj.cell)
        setContainsRowHeader(false)
      }

    } else if (obj.any) {
      // 何か選択
      const cell = obj.any.getRowModel().flatRows[0]?.getAllCells()?.[0]
      if (cell) {
        setCaretCell(cell)
        setSelectionStart(cell)
        setContainsRowHeader(false)
      }

    } else {
      // 全選択
      const flatRows = obj.all.getRowModel().flatRows
      const firstRow = flatRows[0]
      const lastRow = flatRows[flatRows.length - 1]
      const firstRowVisibleCells = firstRow?.getVisibleCells()
      const lastRowVisibleCells = lastRow?.getVisibleCells()
      const topLeftCell = firstRowVisibleCells?.[0]
      const bottomRightCell = lastRowVisibleCells?.[lastRowVisibleCells.length - 1]
      setCaretCell(topLeftCell)
      setSelectionStart(bottomRightCell)
      setContainsRowHeader(true)
    }
  }, [])

  const handleSelectionKeyDown: React.KeyboardEventHandler<HTMLElement> = useCallback(e => {
    if (editing) return
    if (e.ctrlKey && e.key === 'a') {
      selectObject({ all: api })
      e.preventDefault()

    } else if (!e.ctrlKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      if (!caretCell) {
        selectObject({ any: api })
        e.preventDefault()
        return
      }
      // 1つ上または下のセルを選択
      const flatRows = api.getRowModel().flatRows
      let rowIndex = flatRows.indexOf(caretCell.row)
      if (e.key === 'ArrowUp') rowIndex--; else rowIndex++
      while (e.key === 'ArrowUp' ? (rowIndex > -1) : (rowIndex < flatRows.length)) {
        const row = flatRows[rowIndex]
        if (row === undefined) return
        if (!row.getIsAllParentsExpanded()) {
          if (e.key === 'ArrowUp') rowIndex--; else rowIndex++
          continue
        }
        const colIndex = caretCell.row.getAllCells().indexOf(caretCell)
        selectObject({ cell: row.getAllCells()[colIndex], shiftKey: e.shiftKey })
        e.preventDefault()
        return

      }
    } else if (!e.ctrlKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
      if (!caretCell) {
        selectObject({ any: api })
        e.preventDefault()
        return
      }
      // 1つ左または右のセルを選択
      const allCells = caretCell.row.getAllCells()
      let colIndex = allCells.indexOf(caretCell)
      if (e.key === 'ArrowLeft') colIndex--; else colIndex++
      while (e.key === 'ArrowLeft' ? (colIndex > -1) : (colIndex < allCells.length)) {
        const neighborCell = allCells[colIndex]
        if (neighborCell === undefined) return
        selectObject({ cell: neighborCell, shiftKey: e.shiftKey })
        e.preventDefault()
        return
      }
    }
  }, [editing, api, selectObject, caretCell])

  const caretTdRefCallback = useCallback((td: HTMLTableCellElement | null, cell: RT.Cell<Tree.TreeNode<T>, unknown>) => {
    if (td && cell.id === caretCell?.id) caretTdRef.current = td
    if (td && cell.id === selectionStart?.id) selectionStartTdRef.current = td
  }, [caretCell, selectionStart])

  const ActiveCellBorder = useMemo(() => {
    return prepareActiveRangeSvg<T>(caretTdRef, selectionStartTdRef)
  }, [])

  const getSelectedRows = useCallback(() => {
    if (!caretCell || !selectionStart) return []
    const flatRows = api.getRowModel().flatRows
    const ix1 = flatRows.indexOf(selectionStart.row)
    const ix2 = flatRows.indexOf(caretCell.row)
    const since = Math.min(ix1, ix2)
    const until = Math.max(ix1, ix2)
    return flatRows.slice(since, until + 1)
  }, [api, caretCell, selectionStart])

  const getSelectedIndexes = useCallback(() => {
    if (!caretCell || !selectionStart) return []
    const flatRows = api.getRowModel().flatRows
    const ix1 = flatRows.indexOf(selectionStart.row)
    const ix2 = flatRows.indexOf(caretCell.row)
    const since = Math.min(ix1, ix2)
    const until = Math.max(ix1, ix2)
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
    },

    getSelectedRows,
    getSelectedIndexes,
  }
}


function prepareActiveRangeSvg<T>(
  caretTdRef: React.MutableRefObject<HTMLTableCellElement | undefined>,
  selectionStartTdRef: React.MutableRefObject<HTMLTableCellElement | undefined>,
) {
  return ({ caretCell, containsRowHeader, api }: {
    caretCell: RT.Cell<Tree.TreeNode<T>, unknown> | undefined
    containsRowHeader: boolean
    api: RT.Table<Tree.TreeNode<T>>
  }) => {
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

      svgRef.current?.scrollIntoView({
        behavior: 'instant',
        block: 'nearest',
        inline: 'nearest',
      })
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
          className="fill-black opacity-[.15]"
          mask="url(#selection-start-mask)"
        />
      </svg>
    )
  }
}

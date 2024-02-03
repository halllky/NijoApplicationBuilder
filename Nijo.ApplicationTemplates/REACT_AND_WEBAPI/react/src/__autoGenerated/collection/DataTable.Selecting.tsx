import { useState, useRef, useCallback, useEffect } from 'react'
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
    = { all: RT.Table<Tree.TreeNode<T>> }
    | { all?: undefined, cell: RT.Cell<Tree.TreeNode<T>, unknown> }

  const selectObject = useCallback((obj: SelectTarget, e: { shiftKey: boolean }) => {
    if (obj.all) {
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

    } else if (obj.cell.column.id === ROW_HEADER_ID) {
      // シングル選択（行ヘッダが選択された場合）
      const visibleCells = obj.cell.row.getVisibleCells()
      if (visibleCells.length === 0) return
      setCaretCell(visibleCells[0])
      if (!e.shiftKey) setSelectionStart(visibleCells[visibleCells.length - 1])
      setContainsRowHeader(true)

    } else {
      // シングル選択
      setCaretCell(obj.cell)
      if (!e.shiftKey) setSelectionStart(obj.cell)
      setContainsRowHeader(false)
    }
  }, [])

  const clearSelection = useCallback(() => {
    setCaretCell(undefined)
    setSelectionStart(undefined)
    setContainsRowHeader(false)
    caretTdRef.current = undefined
    selectionStartTdRef.current = undefined
  }, [])

  const handleSelectionKeyDown: React.KeyboardEventHandler<HTMLElement> = useCallback(e => {
    if (editing) return
    if (e.ctrlKey && e.key === 'a') {
      selectObject({ all: api }, e)
      e.preventDefault()

    } else if (!e.ctrlKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      const flatRows = api.getRowModel().flatRows
      if (!caretCell) {
        // 未選択の状態なので先頭行を選択
        const cell = flatRows[0]?.getAllCells()?.[0]
        if (cell) selectObject({ cell }, e)
        e.preventDefault()
        return
      }
      // 1つ上または下のセルを選択
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
        selectObject({ cell: row.getAllCells()[colIndex] }, e)
        e.preventDefault()
        return

      }
    } else if (!e.ctrlKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
      const flatRows = api.getRowModel().flatRows
      if (!caretCell) {
        // 未選択の状態なので先頭行を選択
        const cell = flatRows[0]?.getAllCells()?.[0]
        if (cell) selectObject({ cell }, e)
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
        selectObject({ cell: neighborCell }, e)
        e.preventDefault()
        return
      }
    }
  }, [editing, api, selectObject, caretCell])

  const caretTdRefCallback = useCallback((td: HTMLTableCellElement | null, cell: RT.Cell<Tree.TreeNode<T>, unknown>) => {
    if (td && cell === caretCell) caretTdRef.current = td
    if (td && cell === selectionStart) selectionStartTdRef.current = td
  }, [caretCell, selectionStart])

  const ActiveCellBorder = useCallback((props: {
    caretCell: typeof caretCell
    containsRowHeader: boolean
    api: typeof api
  }) => {
    const svgRef = useRef<SVGSVGElement>(null)
    const maskBlackRef = useRef<SVGRectElement>(null)
    useEffect(() => {
      const head = caretTdRef.current
      const root = selectionStartTdRef.current
      if (!head || !root || !svgRef.current || !maskBlackRef.current) return

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

      maskBlackRef.current.setAttribute('x', `${root.offsetLeft - left}px`)
      maskBlackRef.current.setAttribute('y', `${root.offsetTop - top}px`)
      maskBlackRef.current.style.width = `${root.offsetWidth}px`
      maskBlackRef.current.style.height = `${root.offsetHeight}px`

      svgRef.current.style.zIndex = props.containsRowHeader
        ? TABLE_ZINDEX.ROWHEADER_SELECTION.toString()
        : TABLE_ZINDEX.SELECTION.toString()

      svgRef.current?.scrollIntoView({
        behavior: 'instant',
        block: 'nearest',
        inline: 'nearest',
      })
    }, [
      containsRowHeader,
      props.caretCell,
      api.getState().columnSizing,
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
  }, [api])

  return {
    selectObject,
    clearSelection,
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

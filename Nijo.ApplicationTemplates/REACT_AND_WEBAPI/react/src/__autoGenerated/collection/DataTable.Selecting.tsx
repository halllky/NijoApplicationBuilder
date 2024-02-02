import { useState, useRef, useCallback, useEffect } from 'react'
import * as RT from '@tanstack/react-table'
import * as Tree from '../util'
import { ROW_HEADER_ID, TABLE_ZINDEX } from './DataTable.Parts'

export const useSelection = <T,>(editing: boolean, api: RT.Table<Tree.TreeNode<T>>) => {
  const [caretCell, setCaretCell] = useState<RT.Cell<Tree.TreeNode<T>, unknown> | undefined>()
  const [selectionStart, setSelectionStart] = useState<RT.Cell<Tree.TreeNode<T>, unknown> | undefined>()
  const caretTdRef = useRef<HTMLTableCellElement>()
  const selectionStartTdRef = useRef<HTMLTableCellElement>()

  type SelectTarget
    = { all: true }
    | { all?: undefined, row: RT.Row<Tree.TreeNode<T>>, cell?: undefined }
    | { all?: undefined, row?: undefined, cell: RT.Cell<Tree.TreeNode<T>, unknown> }
  const selectObject = useCallback((obj: SelectTarget, e: { shiftKey: boolean }) => {
    if (obj.all) {
      // 全選択
      const flatRows = api.getRowModel().flatRows
      const firstRow = flatRows[0]
      const lastRow = flatRows[flatRows.length - 1]
      const firstRowVisibleCells = firstRow?.getVisibleCells()
      const lastRowVisibleCells = lastRow?.getVisibleCells()
      const topLeftCell = firstRowVisibleCells?.[0]
      const bottomRightCell = lastRowVisibleCells?.[lastRowVisibleCells.length - 1]
      setCaretCell(topLeftCell)
      setSelectionStart(bottomRightCell)

    } else if (e.shiftKey && selectionStart) {
      // 範囲選択
      if (obj.row) {
        setCaretCell(obj.row.getVisibleCells()[0])
      } else if (obj.cell.column.id === ROW_HEADER_ID) {
        // 行ヘッダが選択された場合は行全体の選択に読み替え
        setCaretCell(obj.cell.row.getVisibleCells()[0])
      } else {
        setCaretCell(obj.cell)
      }

    } else {
      // シングル選択。引数のrowのみが選択されている状態にする
      if (obj.row) {
        const visibleCells = obj.row.getVisibleCells()
        if (visibleCells.length === 0) return
        setCaretCell(visibleCells[0])
        setSelectionStart(visibleCells[visibleCells.length - 1])

      } else if (obj.cell.column.id === ROW_HEADER_ID) {
        // 行ヘッダが選択された場合は行全体の選択に読み替え
        const visibleCells = obj.cell.row.getVisibleCells()
        if (visibleCells.length === 0) return
        setCaretCell(visibleCells[0])
        setSelectionStart(visibleCells[visibleCells.length - 1])

      } else {
        setCaretCell(obj.cell)
        setSelectionStart(obj.cell)
      }
    }
  }, [api, selectionStart])

  const clearSelection = useCallback(() => {
    setCaretCell(undefined)
    setSelectionStart(undefined)
    caretTdRef.current = undefined
    selectionStartTdRef.current = undefined
  }, [api])

  const handleSelectionKeyDown: React.KeyboardEventHandler<HTMLElement> = useCallback(e => {
    if (editing) return
    if (e.ctrlKey && e.key === 'a') {
      selectObject({ all: true }, e)
      e.preventDefault()

    } else if (!e.ctrlKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      const flatRows = api.getRowModel().flatRows
      if (!caretCell) {
        // 未選択の状態なので先頭行を選択
        if (flatRows.length >= 1) selectObject({ row: flatRows[0] }, e)
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
        if (flatRows.length >= 1) selectObject({ row: flatRows[0] }, e)
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
    api: typeof api
  }) => {
    const divRef = useRef<HTMLDivElement>(null)
    useEffect(() => {
      const head = caretTdRef.current
      const root = selectionStartTdRef.current
      if (!head || !root || !divRef.current) return

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

      divRef.current.style.left = `${left}px`
      divRef.current.style.top = `${top}px`
      divRef.current.style.width = `${right - left}px`
      divRef.current.style.height = `${bottom - top}px`

      divRef.current.style.zIndex = TABLE_ZINDEX.CELLEDITOR.toString()
      divRef.current.scrollIntoView({
        behavior: 'instant',
        block: 'nearest',
        inline: 'nearest',
      })
    }, [props.caretCell, api.getState().columnSizing, api.getState().expanded])

    return (
      <div ref={divRef}
        className="absolute pointer-events-none outline outline-2 outline-offset-[-2px] bg-color-selected"
      ></div>
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
    caretCell,
    clearSelection,
    handleSelectionKeyDown,
    caretTdRefCallback,
    ActiveCellBorder,
    getSelectedRows,
    getSelectedIndexes,
  }
}

import React, { useCallback, useImperativeHandle, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import { DataTableProps, ColumnDefEx } from './DataTable.Public'
import { CellEditorRef, CellPosition, TABLE_ZINDEX } from './DataTable.Parts'
import * as Input from '../input'
import * as Util from '../util'

export type CellEditorProps<T> = {
  api: RT.Table<T>
  caretCell: CellPosition | undefined
  caretTdRef: React.RefObject<HTMLTableCellElement | undefined>
  onChangeEditing: (editing: boolean) => void
  onChangeRow: DataTableProps<T>['onChangeRow']
  onKeyDown: React.KeyboardEventHandler
}

export const CellEditor = Util.forwardRefEx(<T,>({
  api,
  caretCell,
  caretTdRef,
  onChangeEditing,
  onChangeRow,
  onKeyDown,
}: CellEditorProps<T>,
  ref: React.ForwardedRef<CellEditorRef<T>>
) => {
  const [editingCellInfo, setEditingCellInfo] = useState<{
    row: T
    rowIndex: number
    cellId: string
    setValue: ColumnDefEx<T>['setValue']
  } | undefined>(undefined)

  const [uncomittedValue, setUnComittedValue] = useState<string>()

  const editorRef = useRef<Input.CustomComponentRef<string>>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  const startEditing = useCallback((cell: RT.Cell<T, unknown>) => {
    const columnDef = cell.column.columnDef as ColumnDefEx<T>
    const setValue = columnDef.setValue

    if (!onChangeRow) return // 値が編集されてもコミットできないので編集開始しない
    if (columnDef.cellEditor === undefined) return // 編集不可のセル
    if (setValue === undefined) return // 編集不可のセル

    setEditingCellInfo({
      cellId: cell.id,
      setValue,
      rowIndex: cell.row.index,
      row: cell.row.original,
    })
    onChangeEditing(true)

    // TODO: ColumnDefの定義に従って、このコンポーネントのInput.Wordの設定を変える（コンボボックスにする、単行複数行を変える、など）

    // 現在のセルの値をエディタに渡す
    const cellValue = cell.getValue()
    setUnComittedValue((cellValue as string) ?? '') // TODO: string決め打ちにしたい

    // エディタを編集対象セルの位置に移動させる
    if (caretTdRef.current && containerRef.current) {
      containerRef.current.style.left = `${caretTdRef.current.offsetLeft}px`
      containerRef.current.style.top = `${caretTdRef.current.offsetTop}px`
      containerRef.current.style.minWidth = `${caretTdRef.current.clientWidth}px`
    }
    // エディタにスクロール
    containerRef.current?.scrollIntoView({
      behavior: 'instant',
      block: 'nearest',
      inline: 'nearest',
    })
  }, [setEditingCellInfo, onChangeEditing, onChangeRow])

  // TODO: クイック編集を実現するために、任意のCellEditorを設定できる仕様を変更したい
  // const cellEditor = useMemo(() => {
  //   const editor = (editingCell?.column.columnDef as ColumnDefEx<T>)?.cellEditor
  //   if (editor) return Util.forwardRefEx(editor)

  //   // セル編集コンポーネント未指定の場合
  //   return Util.forwardRefEx<
  //     Input.CustomComponentRef<any>,
  //     Input.CustomComponentProps<any>
  //   >((props, ref) => <Input.Word ref={ref} {...props} />)

  // }, [editingCell?.column])

  const commitEditing = useCallback(() => {
    if (editingCellInfo !== undefined && onChangeRow) {
      editingCellInfo.setValue?.(editingCellInfo.row, editorRef.current?.getValue())
      onChangeRow(editingCellInfo.rowIndex, editingCellInfo.row)
    }
    setEditingCellInfo(undefined)
    onChangeEditing(false)
  }, [editingCellInfo, setEditingCellInfo, onChangeRow, onChangeEditing])

  const cancelEditing = useCallback(() => {
    setEditingCellInfo(undefined)
    onChangeEditing(false)
  }, [setEditingCellInfo, onChangeEditing])

  const [{ isImeOpen }] = Util.useIMEOpened()
  const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = useCallback(e => {
    if (editingCellInfo) {
      // 編集を終わらせる
      if ((e.key === 'Enter' || e.key === 'Tab') && !isImeOpen) {
        commitEditing()
        e.stopPropagation()
        e.preventDefault()

      } else if (e.key === 'Escape') {
        cancelEditing()
        e.preventDefault()
      }
    } else {
      // セル移動や選択
      onKeyDown(e)
      if (e.defaultPrevented) return

      // 編集を始める
      if (caretCell
        && (e.key === 'F2'
          // クイック編集（編集モードでない状態でいきなり文字入力して編集を開始する）
          || isImeOpen
          || e.key === 'Process' // IMEが開いている場合のkeyはこれになる
          || !e.ctrlKey && !e.metaKey && e.key.length === 1 /*文字や数字や記号の場合*/)) {

        const row = api.getCoreRowModel().flatRows[caretCell.rowIndex]
        const cell = row.getAllCells().find(cell => cell.column.id === caretCell.colId)
        if (cell) startEditing(cell)
      }
    }
  }, [isImeOpen, editingCellInfo, startEditing, commitEditing, cancelEditing, onKeyDown, api, caretCell])

  useImperativeHandle(ref, () => ({
    focus: () => editorRef.current?.focus(),
    startEditing,
  }), [startEditing])

  return (
    <div ref={containerRef}
      className="absolute min-w-4 min-h-4"
      style={{
        zIndex: TABLE_ZINDEX.CELLEDITOR,
        opacity: editingCellInfo === undefined ? 0 : undefined,
        pointerEvents: editingCellInfo === undefined ? 'none' : undefined,
      }}
    >
      <Input.Word
        ref={editorRef}
        value={uncomittedValue as string}
        onChange={setUnComittedValue}
        onKeyDown={handleKeyDown}
        onBlur={commitEditing}
        className="block w-full"
      />
      {/* {React.createElement(cellEditor, {
          ref: editorRef,
          value: uncomittedValue,
          onChange: setUnComittedValue,
          onKeyDown: handleKeyDown,
          onBlur: commitEditing,
          className: 'block resize',
        })}
         */}
    </div>
  )
})


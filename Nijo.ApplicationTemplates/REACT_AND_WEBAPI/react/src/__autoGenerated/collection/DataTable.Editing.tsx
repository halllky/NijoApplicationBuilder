import React, { useCallback, useEffect, useImperativeHandle, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import { DataTableProps, ColumnDefEx, ColumnEditSetting } from './DataTable.Public'
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

  // 編集中セルの情報。undefined以外の値が入っているときは編集モード。
  const [editingCellInfo, setEditingCellInfo] = useState<{
    row: T
    rowIndex: number
    cellId: string
  } | undefined>(undefined)

  // エディタの値
  const [uncomittedText, setUnComittedText] = useState<string>()
  const [comboSelectedItem, setComboSelectedItem] = useState<unknown | undefined>()

  // エディタ設定。caretセルが移動するたびに更新される。
  const [caretCellEditingInfo, setCaretCellEditingInfo] = useState<ColumnEditSetting<T>>()
  const containerRef = useRef<HTMLLabelElement>(null)
  const editorRef = useRef<Input.CustomComponentRef<string | unknown>>(null)
  useEffect(() => {
    if (caretCell) {
      const columnDef = api.getColumn(caretCell.colId)?.columnDef as ColumnDefEx<T> | undefined
      setCaretCellEditingInfo(columnDef?.editSetting)

      // エディタを編集対象セルの位置に移動させる
      if (caretTdRef.current && containerRef.current) {
        containerRef.current.style.left = `${caretTdRef.current.offsetLeft}px`
        containerRef.current.style.top = `${caretTdRef.current.offsetTop}px`
        containerRef.current.style.minWidth = `${caretTdRef.current.clientWidth}px`
        containerRef.current.style.minHeight = `${caretTdRef.current.clientHeight}px`
      }
      // エディタにスクロール
      containerRef.current?.scrollIntoView({
        behavior: 'instant',
        block: 'nearest',
        inline: 'nearest',
      })
      // 前のセルで入力した値をクリアする
      setUnComittedText('')
    } else {
      setCaretCellEditingInfo(undefined)
    }
  }, [caretCell, api, caretTdRef, containerRef])
  useEffect(() => {
    editorRef.current?.focus()
  }, [caretCellEditingInfo])

  /** 編集開始 */
  const startEditing = useCallback((cell: RT.Cell<T, unknown>) => {
    const columnDef = cell.column.columnDef as ColumnDefEx<T>

    if (!onChangeRow) return // 値が編集されてもコミットできないので編集開始しない
    if (!columnDef.editSetting) return // 編集不可のセル
    if (columnDef.editSetting.readOnly?.(cell.row.original)) return // 編集不可のセル

    setEditingCellInfo({
      cellId: cell.id,
      rowIndex: cell.row.index,
      row: cell.row.original,
    })
    onChangeEditing(true)

    // 現在のセルの値をエディタに渡す
    if (columnDef.editSetting.type === 'text') {
      const cellValue = columnDef.editSetting.getTextValue(cell.row.original)
      setUnComittedText(cellValue)
    } else if (columnDef.editSetting.type === 'multiline-text') {
      const cellValue = columnDef.editSetting.getTextValue(cell.row.original)
      setUnComittedText(cellValue)
    } else if (columnDef.editSetting.type === 'combo') {
      const selectedValue = columnDef.editSetting.getValueFromRow(cell.row.original)
      setComboSelectedItem(selectedValue)
    } else if (columnDef.editSetting.type === 'async-combo') {
      const selectedValue = columnDef.editSetting.getValueFromRow(cell.row.original)
      setComboSelectedItem(selectedValue)
    }
  }, [setEditingCellInfo, onChangeEditing, onChangeRow])

  /** 編集確定 */
  const commitEditing = useCallback((value?: string | unknown | undefined) => {
    if (editingCellInfo === undefined) return
    if (caretCellEditingInfo === undefined) return

    // set value
    if (caretCellEditingInfo.type === 'text') {
      caretCellEditingInfo.setTextValue(editingCellInfo.row, (value ?? editorRef.current?.getValue()) as string | undefined)
    } else if (caretCellEditingInfo.type === 'multiline-text') {
      caretCellEditingInfo.setTextValue(editingCellInfo.row, (value ?? editorRef.current?.getValue()) as string | undefined)
    } else if (caretCellEditingInfo.type === 'combo') {
      caretCellEditingInfo.setValueToRow(editingCellInfo.row, (value ?? editorRef.current?.getValue()) as unknown | undefined)
    } else if (caretCellEditingInfo.type === 'async-combo') {
      caretCellEditingInfo.setValueToRow(editingCellInfo.row, (value ?? editorRef.current?.getValue()) as unknown | undefined)
    }

    onChangeRow?.(editingCellInfo.rowIndex, editingCellInfo.row)
    setEditingCellInfo(undefined)
    onChangeEditing(false)
  }, [editingCellInfo, caretCellEditingInfo, onChangeRow, onChangeEditing])

  /** 編集キャンセル */
  const cancelEditing = useCallback(() => {
    setEditingCellInfo(undefined)
    onChangeEditing(false)
  }, [setEditingCellInfo, onChangeEditing])

  // ----------------------------------
  // イベント
  const handleChangeCombobox = useCallback((selectedItem: unknown | undefined) => {
    setComboSelectedItem(selectedItem)
    commitEditing(selectedItem)
  }, [setComboSelectedItem, commitEditing])

  const [{ isImeOpen }] = Util.useIMEOpened()
  const handleKeyDown: React.KeyboardEventHandler<HTMLLabelElement> = useCallback(e => {
    if (editingCellInfo) {
      // 編集を終わらせる
      if (e.key === 'Enter' || e.key === 'Tab') {
        if (isImeOpen) return // IMEが開いているときのEnterやTabでは編集終了しないようにする
        if (caretCellEditingInfo?.type === 'multiline-text' && !e.ctrlKey && !e.metaKey) return // セル内改行のため普通のEnterでは編集終了しないようにする
        commitEditing()
        e.stopPropagation()
        e.preventDefault()

      } else if (e.key === 'Escape') {
        cancelEditing()
        e.preventDefault()
      }
    } else {
      // 編集を始める
      if (caretCell && (
        e.key === 'F2'

        // クイック編集（編集モードでない状態でいきなり文字入力して編集を開始する）
        || isImeOpen
        || e.key === 'Process' // IMEが開いている場合のkeyはこれになる
        || !e.ctrlKey && !e.metaKey && e.key.length === 1 /*文字や数字や記号の場合*/

        // コンボボックスならば Alt + ArrowDown で編集開始
        || (caretCellEditingInfo?.type === 'combo' || caretCellEditingInfo?.type === 'async-combo')
        && e.altKey
        && e.key === 'ArrowDown'
      )) {
        const row = api.getCoreRowModel().flatRows[caretCell.rowIndex]
        const cell = row.getAllCells().find(cell => cell.column.id === caretCell.colId)
        if (cell) startEditing(cell)
        return
      }

      // セル移動や選択
      if (e.type === 'keydown') {
        onKeyDown(e)
        if (e.defaultPrevented) return
      }
    }
  }, [isImeOpen, caretCellEditingInfo, editingCellInfo, startEditing, commitEditing, cancelEditing, onKeyDown, api, caretCell])

  Util.useOutsideClick(containerRef, () => {
    commitEditing()
  }, [commitEditing])

  useImperativeHandle(ref, () => ({
    focus: () => editorRef.current?.focus(),
    startEditing,
  }), [startEditing])

  return (
    <label ref={containerRef}
      className="absolute min-w-4 min-h-4 flex items-stretch outline-none"
      style={{
        zIndex: TABLE_ZINDEX.CELLEDITOR,
        // クイック編集のためCellEditor自体は常に存在し続けるが、セル編集モードでないときは見えないようにする
        opacity: editingCellInfo === undefined ? 0 : undefined,
        pointerEvents: editingCellInfo === undefined ? 'none' : undefined,
      }}
      onKeyDown={handleKeyDown}
      tabIndex={0}

    // なぜかIMEオンの状態でキー入力したときたまにkeydownイベントが発火しなくなることがあるので念のためkeyupでも同様の制御をおこなう、
    // というのをやりたいが、セルの値をペーストしたときにCtrlキーを先に放しその後Vを放したときに編集開始が誤爆するのでオフにする
    // onKeyUp={handleKeyDown}
    >
      {(caretCellEditingInfo === undefined || caretCellEditingInfo?.type === 'text') && (
        <Input.Word
          ref={editorRef as React.RefObject<Input.CustomComponentRef<string>>}
          value={uncomittedText}
          onChange={setUnComittedText}
          className="flex-1"
        />
      )}
      {caretCellEditingInfo?.type === 'multiline-text' && (
        <Input.Description
          ref={editorRef as React.RefObject<Input.CustomComponentRef<string>>}
          value={uncomittedText}
          onChange={setUnComittedText}
          className="flex-1"
        />
      )}
      {caretCellEditingInfo?.type === 'combo' && (
        <Input.ComboBox
          dropdownAutoOpen={editingCellInfo !== undefined}
          ref={editorRef}
          value={comboSelectedItem}
          onChange={handleChangeCombobox}
          {...caretCellEditingInfo.comboProps}
          className="flex-1"
        />
      )}
      {caretCellEditingInfo?.type === 'async-combo' && (
        <Input.AsyncComboBox
          dropdownAutoOpen={editingCellInfo !== undefined}
          ref={editorRef}
          value={comboSelectedItem}
          onChange={handleChangeCombobox}
          {...caretCellEditingInfo.comboProps}
          className="flex-1"
        />
      )}
    </label>
  )
})


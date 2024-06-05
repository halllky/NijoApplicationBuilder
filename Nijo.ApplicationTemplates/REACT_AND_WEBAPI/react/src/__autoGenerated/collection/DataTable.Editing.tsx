import React, { useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import { DataTableProps, ColumnDefEx } from './DataTable.Public'
import { CellEditorRef, TABLE_ZINDEX } from './DataTable.Parts'
import * as Input from '../input'
import * as Util from '../util'

export const useCellEditing = <T,>(props: DataTableProps<T>) => {
  const [editingCell, setEditingCell] = useState<RT.Cell<T, unknown> | undefined>(undefined)
  const [editingItemIndex, setEditingItemIndex] = useState<number | undefined>()

  const editingTdRef = useRef<HTMLTableCellElement>()
  const editingTdRefCallback = useCallback((td: HTMLTableCellElement | null, cell: RT.Cell<T, unknown>) => {
    if (td && cell.id === editingCell?.id) editingTdRef.current = td
  }, [editingCell])

  const startEditing = useCallback((cell: RT.Cell<T, unknown>) => {
    if (!props.onChangeRow) return // 値が編集されてもコミットできないので編集開始しない
    if (!(cell.column.columnDef as ColumnDefEx<T>)?.cellEditor) return // 編集不可のセル
    setEditingCell(cell)
    setEditingItemIndex(props.data?.indexOf(cell.row.original))
  }, [props.data, props.onChangeRow])

  const cancelEditing = useCallback(() => {
    setEditingCell(undefined)
  }, [])

  const CellEditor = useMemo(() => {
    return prepareCellEditor(setEditingCell, editingTdRef)
  }, [])

  return {
    editing: editingCell !== undefined,
    startEditing,
    cancelEditing,

    CellEditor,
    cellEditorProps: {
      editing: editingCell !== undefined,
      editingCell,
      editingItemIndex,
      onChangeRow: props.onChangeRow,
      data: props.data,
    },
    editingTdRefCallback,
  }
}

export type CellEditorProps<T> = {
  editing: boolean
  editingItemIndex: number | undefined
  onChangeRow: DataTableProps<T>['onChangeRow']
  onKeyDown: React.KeyboardEventHandler
  editingCell: RT.Cell<T, unknown> | undefined
  onEndEditing?: () => void
  requestStartEditing: () => void
}

function prepareCellEditor<T,>(
  setEditingCell: (v: RT.Cell<T, unknown> | undefined) => void,
  editingTdRef: React.MutableRefObject<HTMLTableCellElement | undefined>,
) {
  return Util.forwardRefEx<CellEditorRef, CellEditorProps<T>>(({ editing, editingItemIndex, onChangeRow, onKeyDown, editingCell, onEndEditing, requestStartEditing }, ref) => {
    const [uncomittedValue, setUnComittedValue] = useState<unknown>(() => {
      if (!editingCell?.column.accessorFn || editingItemIndex === undefined) return undefined
      return editingCell.column.accessorFn(editingCell.row.original, editingItemIndex)
    })

    // TODO: クイック編集を実現するために、任意のCellEditorを設定できる仕様を変更したい
    const cellEditor = useMemo(() => {
      const editor = (editingCell?.column.columnDef as ColumnDefEx<T>)?.cellEditor
      if (editor) return Util.forwardRefEx(editor)

      // セル編集コンポーネント未指定の場合
      return Util.forwardRefEx<
        Input.CustomComponentRef<any>,
        Input.CustomComponentProps<any>
      >((props, ref) => <Input.Word ref={ref} {...props} />)

    }, [editingCell?.column])

    const commitEditing = useCallback(() => {
      if (editingItemIndex !== undefined && onChangeRow && editingCell) {
        const setValue = (editingCell?.column.columnDef as ColumnDefEx<T>)?.setValue
        if (!setValue) {
          // セッターがないのにこのコンポーネントが存在するのはあり得ないのでエラー
          throw new Error('value setter is not defined.')
        }
        setValue(editingCell.row.original, editorRef.current?.getValue())
        onChangeRow(editingItemIndex, editingCell.row.original)
      }
      setEditingCell(undefined)
      onEndEditing?.()
    }, [editingItemIndex, onChangeRow, editingCell, onEndEditing])

    const cancelEditing = useCallback(() => {
      setEditingCell(undefined)
      onEndEditing?.()
    }, [onEndEditing])

    const editorRef = useRef<Input.CustomComponentRef<string>>(null)
    const containerRef = useRef<HTMLDivElement>(null)
    useEffect(() => {
      // エディタの初期値設定
      if (editing) {
        setUnComittedValue(editingCell!.getValue())
      }
      // エディタを編集対象セルの位置に移動させる
      if (editingTdRef.current && containerRef.current) {
        containerRef.current.style.left = `${editingTdRef.current.offsetLeft}px`
        containerRef.current.style.top = `${editingTdRef.current.offsetTop}px`
        containerRef.current.style.minWidth = `${editingTdRef.current.clientWidth}px`
      }
      // エディタにスクロール
      containerRef.current?.scrollIntoView({
        behavior: 'instant',
        block: 'nearest',
        inline: 'nearest',
      })
    }, [editing])

    const [{ isImeOpen }] = Util.useIMEOpened()
    const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = useCallback(e => {
      if (editing) {
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
        if (e.key === 'F2'
          || isImeOpen
          || e.key === 'Process' // IMEが開いている場合のkeyはこれになる
          || !e.ctrlKey && !e.metaKey && e.key.length === 1 /*文字や数字や記号の場合*/) {
          requestStartEditing()
        }
      }
    }, [isImeOpen, editing, commitEditing, cancelEditing, onKeyDown, requestStartEditing])

    useImperativeHandle(ref, () => ({
      focus: () => editorRef.current?.focus(),
    }))

    return (
      <div ref={containerRef}
        className="absolute min-w-4 min-h-4"
        style={{
          zIndex: TABLE_ZINDEX.CELLEDITOR,
          opacity: editing ? undefined : 0,
          pointerEvents: editing ? undefined : 'none',
        }}
      >
        <Input.Word
          ref={editorRef}
          value={uncomittedValue as string}
          onChange={setUnComittedValue}
          onKeyDown={handleKeyDown}
          onBlur={commitEditing}
          className="block resize w-full"
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
}

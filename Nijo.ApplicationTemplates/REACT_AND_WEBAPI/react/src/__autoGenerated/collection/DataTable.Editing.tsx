import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import { DataTableProps, ColumnDefEx } from './DataTable.Public'
import { TABLE_ZINDEX } from './DataTable.Parts'
import * as Input from '../input'
import * as Tree from '../util'
import * as Util from '../util'

export const useCellEditing = <T,>(props: DataTableProps<T>) => {
  const [editingCell, setEditingCell] = useState<RT.Cell<Tree.TreeNode<T>, unknown> | undefined>(undefined)

  const editingTdRef = useRef<HTMLTableCellElement>()
  const editingTdRefCallback = useCallback((td: HTMLTableCellElement | null, cell: RT.Cell<Tree.TreeNode<T>, unknown>) => {
    if (td && cell === editingCell) editingTdRef.current = td
  }, [editingCell])

  const startEditing = useCallback((cell: RT.Cell<Tree.TreeNode<T>, unknown>) => {
    if (!props.onChangeRow) return // 値が編集されてもコミットできないので編集開始しない
    setEditingCell(cell)
  }, [props.onChangeRow])

  const cancelEditing = useCallback(() => {
    setEditingCell(undefined)
  }, [])

  const CellEditor = useCallback(({ onEndEditing }: {
    onEndEditing?: () => void
  }) => {
    const [uncomittedValue, setUnComittedValue] = useState<unknown>(() => {
      if (!editingCell?.column.id) return undefined
      const row = editingCell?.row.original.item as { [key: string]: unknown }
      return row?.[editingCell.column.id]
    })

    const cellEditor: Util.CustomComponent = useMemo(() => {
      const editor = (editingCell?.column.columnDef as ColumnDefEx<T>)?.cellEditor
      return editor ?? Input.Description
    }, [editingCell?.column])

    const commitEditing = useCallback(() => {
      if (props.data && props.onChangeRow && editingCell) {
        const item = editingCell.row.original.item as { [key: string]: unknown }
        item[editingCell.column.id] = editorRef.current?.getValue()
        props.onChangeRow(props.data.indexOf(item as T), item as T)
      }
      setEditingCell(undefined)
      onEndEditing?.()
    }, [props.data, props.onChangeRow, editingCell, onEndEditing])

    const cancelEditing2 = useCallback(() => {
      setEditingCell(undefined)
      onEndEditing?.()
    }, [onEndEditing])

    const editorRef = useRef<Util.CustomComponentRef<any>>(null)
    const containerRef = useRef<HTMLDivElement>(null)
    useEffect(() => {
      // エディタを編集対象セルの位置に移動させる
      if (editingTdRef.current && containerRef.current) {
        containerRef.current.style.left = `${editingTdRef.current.offsetLeft}px`
        containerRef.current.style.top = `${editingTdRef.current.offsetTop}px`
      }
      // エディタにフォーカスを当ててスクロール
      editorRef.current?.focus()
      containerRef.current?.scrollIntoView({
        behavior: 'instant',
        block: 'nearest',
        inline: 'nearest',
      })
    }, [])

    const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = useCallback(e => {
      if (e.ctrlKey && e.key === 'Enter') {
        commitEditing()
        e.stopPropagation()
        e.preventDefault()
      } else if (e.key === 'Escape') {
        cancelEditing2()
        e.preventDefault()
      }
    }, [commitEditing, cancelEditing2])

    return (
      <div ref={containerRef}
        className="absolute min-w-4 min-h-4"
        style={{ zIndex: TABLE_ZINDEX.CELLEDITOR }}
      >
        {React.createElement(cellEditor, {
          ref: editorRef,
          value: uncomittedValue,
          onChange: setUnComittedValue,
          onKeyDown: handleKeyDown,
          className: 'block resize',
        })}
        {/* <div className="flex justify-start gap-1">
          <Input.IconButton fill className="text-xs" onClick={commitEditing}>確定(Ctrl+Enter)</Input.IconButton>
          <Input.IconButton fill className="text-xs" onClick={cancelEditing2}>キャンセル(Esc)</Input.IconButton>
        </div> */}
      </div>
    )
  }, [editingCell, props.data, props.onChangeRow])

  return {
    editing: editingCell !== undefined,
    startEditing,
    cancelEditing,
    CellEditor,
    editingTdRefCallback,
  }
}

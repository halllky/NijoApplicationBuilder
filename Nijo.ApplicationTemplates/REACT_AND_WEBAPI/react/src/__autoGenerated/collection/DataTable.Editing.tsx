import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import { DataTableProps, ColumnDefEx } from './DataTable.Public'
import { TABLE_ZINDEX } from './DataTable.Parts'
import * as Input from '../input'
import * as Tree from '../util'
import * as Util from '../util'

export const useCellEditing = <T,>(props: DataTableProps<T>) => {
  const [editingCell, setEditingCell] = useState<RT.Cell<Tree.TreeNode<T>, unknown> | undefined>(undefined)
  const [editingItemIndex, setEditingItemIndex] = useState<number | undefined>()

  const editingTdRef = useRef<HTMLTableCellElement>()
  const editingTdRefCallback = useCallback((td: HTMLTableCellElement | null, cell: RT.Cell<Tree.TreeNode<T>, unknown>) => {
    if (td && cell.id === editingCell?.id) editingTdRef.current = td
  }, [editingCell])

  const startEditing = useCallback((cell: RT.Cell<Tree.TreeNode<T>, unknown>) => {
    if (!props.onChangeRow) return // 値が編集されてもコミットできないので編集開始しない
    if (!(cell.column.columnDef as ColumnDefEx<Tree.TreeNode<T>>)?.cellEditor) return // 編集不可のセル
    setEditingCell(cell)
    setEditingItemIndex(props.data?.indexOf(cell.row.original.item))
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
      editingCell,
      editingItemIndex,
      onChangeRow: props.onChangeRow,
      data: props.data,
    },
    editingTdRefCallback,
  }
}


function prepareCellEditor<T,>(
  setEditingCell: (v: RT.Cell<Tree.TreeNode<T>, unknown> | undefined) => void,
  editingTdRef: React.MutableRefObject<HTMLTableCellElement | undefined>,
) {
  return ({ editingItemIndex, onChangeRow, editingCell, onEndEditing }: {
    editingItemIndex: number | undefined,
    onChangeRow: DataTableProps<T>['onChangeRow']
    editingCell: RT.Cell<Tree.TreeNode<T>, unknown> | undefined
    onEndEditing?: () => void
  }) => {
    const [uncomittedValue, setUnComittedValue] = useState<unknown>(() => {
      if (!editingCell?.column.accessorFn || editingItemIndex === undefined) return undefined
      return editingCell.column.accessorFn(editingCell.row.original, editingItemIndex)
    })

    const cellEditor = useMemo(() => {
      const editor = (editingCell?.column.columnDef as ColumnDefEx<Tree.TreeNode<T>>)?.cellEditor
      if (editor) return Util.forwardRefEx(editor)

      // セル編集コンポーネント未指定の場合
      return Util.forwardRefEx<
        Input.CustomComponentRef<any>,
        Input.CustomComponentProps<any>
      >((props, ref) => <Input.Description ref={ref} {...props} />)

    }, [editingCell?.column])

    const commitEditing = useCallback(() => {
      if (editingItemIndex !== undefined && onChangeRow && editingCell) {
        const setValue = (editingCell?.column.columnDef as ColumnDefEx<Tree.TreeNode<T>>)?.setValue
        if (!setValue) {
          // セッターがないのにこのコンポーネントが存在するのはあり得ないのでエラー
          throw new Error('value setter is not defined.')
        }
        setValue(editingCell.row.original, editorRef.current?.getValue())
        onChangeRow(editingItemIndex, editingCell.row.original.item)
      }
      setEditingCell(undefined)
      onEndEditing?.()
    }, [editingItemIndex, onChangeRow, editingCell, onEndEditing])

    const cancelEditing = useCallback(() => {
      setEditingCell(undefined)
      onEndEditing?.()
    }, [onEndEditing])

    const editorRef = useRef<Input.CustomComponentRef<any>>(null)
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

    const [{ isImeOpen }] = Util.useIMEOpened()
    const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = useCallback(e => {
      if ((e.key === 'Enter' || e.key === 'Tab') && !isImeOpen) {
        commitEditing()
        e.stopPropagation()
        e.preventDefault()
      } else if (e.key === 'Escape') {
        cancelEditing()
        e.preventDefault()
      }
    }, [isImeOpen, commitEditing, cancelEditing])

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
          onBlur: commitEditing,
          className: 'block resize',
        })}
        {/* <div className="flex justify-start gap-1">
          <Input.IconButton fill className="text-xs" onClick={commitEditing}>確定(Ctrl+Enter)</Input.IconButton>
          <Input.IconButton fill className="text-xs" onClick={cancelEditing2}>キャンセル(Esc)</Input.IconButton>
        </div> */}
      </div>
    )
  }
}

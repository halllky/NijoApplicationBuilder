import React, { useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState } from 'react'
import * as RT from '@tanstack/react-table'
import * as Icon from '@heroicons/react/24/outline'
import * as Util from '../util'
import * as Tree from '../util'
import * as Input from '../input'

export type DataTableProps<T> = {
  data?: T[]
  columns?: RT.ColumnDef<Tree.TreeNode<T>>[]
  className?: string
  treeView?: Tree.ToTreeArgs<T> & {
    rowHeader: (row: T) => React.ReactNode
  }
  editArrayPath?: string
  children?: React.ReactNode
  keepSelectWhenNotFocus?: boolean
}
export type DataTableRef<T> = {
  getSelectedItems: () => T[]
  getSelectedIndexes: () => number[]
}

export const DataTable = Util.forwardRefEx(<T,>(props: DataTableProps<T>, ref: React.ForwardedRef<DataTableRef<T>>) => {
  // 行
  const dataAsTree = useMemo(() => {
    if (!props.data) return []
    return props.treeView
      ? Tree.toTree(props.data, props.treeView)
      // ツリー表示に関する設定が無い場合は親子関係のないフラットな配列にする
      : Tree.toTree(props.data, undefined)
  }, [props.data])

  // 列
  const columnHelper = useMemo(() => RT.createColumnHelper<Tree.TreeNode<T>>(), [])
  const columns = useMemo(() => {
    const colDefs = props.treeView
      ? ([getRowHeader(columnHelper, props), ...(props.columns ?? [])])
      : (props.columns ?? [])
    return colDefs.map(col => ({
      ...col,
      cell: col.cell ?? (DEFAULT_CELL as RT.ColumnDefTemplate<RT.CellContext<Util.TreeNode<T>, unknown>>),
    }))
  }, [props.columns, props.treeView?.rowHeader])

  // 表
  const { selectionOptions } = useSelectionOption()
  const optoins: RT.TableOptions<Tree.TreeNode<T>> = useMemo(() => ({
    data: dataAsTree,
    columns,
    getSubRows: row => row.children,
    getCoreRowModel: RT.getCoreRowModel(),
    ...selectionOptions,
    ...COLUMN_RESIZE_OPTION,
  }), [dataAsTree, columns, selectionOptions])

  const api = RT.useReactTable(optoins)
  const { editing, editingCell, startEditing, CellEditor } = useCellEditing<Tree.TreeNode<T>>(props.editArrayPath)
  const { selectRow, clearSelection, handleSelectionKeyDown, getCellBackColor } = useSelection<Tree.TreeNode<T>>(editing, api)
  const { columnSizeVars, getColWidth, ResizeHandler } = useColumnResizing(api)

  const handleBlur: React.FocusEventHandler<HTMLDivElement> = useCallback(e => {
    if (props.keepSelectWhenNotFocus) return
    // フォーカスの移動先がこの要素の中(ex: props.children)にある場合
    if (e.target.contains(e.relatedTarget)) return
    clearSelection()
  }, [props.keepSelectWhenNotFocus, clearSelection])
  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useCallback(e => {
    // console.log(e.key)
    if (e.key === ' ') {
      for (const row of api.getSelectedRowModel().flatRows) row.toggleExpanded()
      e.preventDefault()
      return
    }
    handleSelectionKeyDown(e)
    if (e.defaultPrevented) return
  }, [api, handleSelectionKeyDown])

  useImperativeHandle(ref, () => ({
    getSelectedItems: () => api.getSelectedRowModel().flatRows.map(row => row.original.item),
    getSelectedIndexes: () => api.getSelectedRowModel().flatRows.map(row => row.index),
    clearSelection,
  }))

  return (
    <div className={`flex flex-col outline-none ${props.className}`}
      onBlur={handleBlur}
      onKeyDown={handleKeyDown}
      tabIndex={0}
    >
      {props.children && (
        <div className="flex gap-1 justify-start items-center">
          {props.children}
        </div>
      )}
      <div className="flex-1 overflow-auto select-none border border-1 border-color-4 bg-color-2">
        <table
          className="table-fixed border-separate border-spacing-0"
          style={{ ...columnSizeVars, width: api.getTotalSize() }}
        >
          {/* ヘッダ */}
          <thead>
            {api.getHeaderGroups().map(headerGroup => (
              <tr key={headerGroup.id}>

                {headerGroup.headers.map(header => (
                  <th key={header.id}
                    className="relative overflow-x-hidden text-start sticky top-0 pl-1 bg-color-3"
                    style={{ width: getColWidth(header), ...getThStickeyStyle(header) }}>
                    {!header.isPlaceholder && RT.flexRender(
                      header.column.columnDef.header,
                      header.getContext())}
                    <ResizeHandler header={header} />
                  </th>
                ))}

              </tr>
            ))}
          </thead>

          {/* ボディ */}
          <tbody>
            {api.getRowModel().flatRows.filter(row => row.getIsAllParentsExpanded()).map(row => (
              <tr
                key={row.id}
                className={`leading-tight ` + (row.getIsSelected() ? 'bg-color-selected' : undefined)}
                onClick={e => selectRow(row, e)}
              >
                {row.getVisibleCells().map(cell => (
                  <td key={cell.id}
                    className={'relative border-r border-1 border-color-3 '
                      + getCellBackColor(row)}
                    style={getTdStickeyStyle(cell)}
                    onDoubleClick={() => startEditing(cell)}
                  >
                    {RT.flexRender(
                      cell.column.columnDef.cell,
                      cell.getContext())}
                    {cell === editingCell && (
                      <CellEditor className="absolute top-0 left-0 min-w-12 min-h-4" />
                    )}
                  </td>
                ))}

              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
})

// -----------------------------------------------
/** 編集 */
const useCellEditing = <T,>(arrayPath: string | undefined) => {
  const [editingCell, setEditingCell] = useState<RT.Cell<T, unknown> | undefined>(undefined)
  const { getValues, setValue } = Util.useFormContextEx()

  const startEditing = useCallback((cell: RT.Cell<T, unknown>) => {
    if (!arrayPath) return // 値が編集されてもコミットできないので編集開始しない
    setEditingCell(cell)
  }, [arrayPath])

  const CellEditor = useCallback(({ className }: { className?: string }) => {
    const [uncomittedValue, setUnComittedValue] = useState<unknown>(() => {
      const name = `${arrayPath}.[${editingCell?.row.index}].${editingCell?.column.id}`
      return getValues(name)
    })

    const commitEditing = useCallback(() => {
      if (arrayPath && editingCell) {
        const array = getValues(arrayPath) as []
        const row = array[editingCell.row.index] as { [key: string]: unknown }
        row[editingCell.column.id] = uncomittedValue
        setValue(arrayPath, [...array])
      }
      setEditingCell(undefined)
    }, [arrayPath, editingCell, getValues, setValue, uncomittedValue])

    const cancelEditing = useCallback(() => {
      setEditingCell(undefined)
    }, [])

    const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = useCallback(e => {
      if (e.ctrlKey && e.key === 'Enter') {
        commitEditing()
        e.stopPropagation()
        e.preventDefault()
      } else if (e.key === 'Escape') {
        cancelEditing()
        e.preventDefault()
      }
    }, [uncomittedValue])

    const editorRef = useRef<Util.CustomComponentRef<any>>(null)
    useEffect(() => {
      editorRef.current?.focus()
    }, [])

    return (
      <div className={`z-10 ${className}`}>
        <Input.Description
          ref={editorRef}
          value={uncomittedValue as any}
          onChange={setUnComittedValue}
          onKeyDown={handleKeyDown}
          className="block resize"
        />
        <div className="flex justify-start gap-1">
          <Input.Button className="text-xs" onClick={commitEditing}>確定(Ctrl+Enter)</Input.Button>
          <Input.Button className="text-xs" onClick={cancelEditing}>キャンセル(Esc)</Input.Button>
        </div>
      </div>
    )
  }, [editingCell, arrayPath])

  return {
    editing: editingCell !== undefined,
    editingCell,
    startEditing,
    CellEditor,
  }
}

// -----------------------------------------------
/** 行選択 */
const useSelectionOption = () => {
  const [rowSelection, onRowSelectionChange] = useState<RT.RowSelectionState>({})
  const selectionOptions: Partial<RT.TableOptions<Tree.TreeNode<any>>> = {
    onRowSelectionChange,
    enableSubRowSelection: true,
    state: { rowSelection },
  }
  return {
    selectionOptions,
  }
}
const useSelection = <T,>(editing: boolean, api: RT.Table<T>) => {
  const [activeRow, setActiveRow] = useState<RT.Row<T> | undefined>(undefined)
  const [selectionStart, setSelectionStart] = useState<RT.Row<T> | undefined>(undefined)

  const getCellBackColor = useCallback((row: RT.Row<T>) => {
    return row.getIsSelected() ? 'bd-color-selected' : 'bg-color-0'
  }, [])

  const selectRow = useCallback((row: RT.Row<T>, e: { shiftKey: boolean, ctrlKey: boolean }) => {
    if (e.ctrlKey) {
      row.toggleSelected(undefined, { selectChildren: false })

    } else if (e.shiftKey && selectionStart) {
      // 範囲選択
      const flatRows = api.getRowModel().flatRows
      const ix1 = flatRows.indexOf(selectionStart)
      const ix2 = flatRows.indexOf(row)
      const since = Math.min(ix1, ix2)
      const until = Math.max(ix1, ix2)
      const newState: { [key: string]: true } = {}
      for (let i = since; i <= until; i++) {
        newState[flatRows[i].id] = true
      }
      api.setRowSelection(newState)
      setActiveRow(row)

    } else {
      // シングル選択。引数のrowのみが選択されている状態にする
      api.setRowSelection({ [row.id]: true })
      setActiveRow(row)
      setSelectionStart(row)
    }
  }, [api, activeRow, selectionStart])

  const clearSelection = useCallback(() => {
    api.resetRowSelection()
    setActiveRow(undefined)
    setSelectionStart(undefined)
  }, [api])

  const handleSelectionKeyDown: React.KeyboardEventHandler<HTMLElement> = useCallback(e => {
    if (editing) return
    if (e.ctrlKey && e.key === 'a') {
      api.toggleAllRowsSelected(true)
      e.preventDefault()

    } else if (!e.ctrlKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      const flatRows = api.getRowModel().flatRows
      if (!activeRow) {
        // 未選択の状態なので先頭行を選択
        if (flatRows.length >= 1) selectRow(flatRows[0], e)
        e.preventDefault()
        return
      }
      // 1つ上または下の行を選択
      let ix = flatRows.indexOf(activeRow)
      if (e.key === 'ArrowUp') ix--; else ix++
      while (e.key === 'ArrowUp' ? (ix > -1) : (ix < flatRows.length)) {
        const row = flatRows[ix]
        if (row === undefined) return
        if (!row.getIsAllParentsExpanded()) {
          if (e.key === 'ArrowUp') ix--; else ix++
          continue
        }
        selectRow(row, e)
        e.preventDefault()
        return
      }
    }
  }, [editing, api, selectRow, activeRow])

  return {
    selectRow,
    clearSelection,
    handleSelectionKeyDown,
    getCellBackColor,
  }
}

// -----------------------------------------------
// 行列ヘッダ固定
const getThStickeyStyle = (header: RT.Header<any, unknown>): React.CSSProperties => ({
  left: header.column.id === ROW_HEADER_ID ? '0' : undefined,
  // y方向にスクロールしたときに行ヘッダの列のtdが上にかぶさってthが見えなくなるのを防ぐ
  zIndex: header.column.id === ROW_HEADER_ID ? 1 : undefined,
})
const getTdStickeyStyle = (cell: RT.Cell<any, unknown>): React.CSSProperties => ({
  left: cell.column.id === ROW_HEADER_ID ? '0' : undefined,
  position: cell.column.id === ROW_HEADER_ID ? 'sticky' : undefined,
})

// -----------------------------------------------
/** 行ヘッダ */
const getRowHeader = <T,>(
  helper: RT.ColumnHelper<Tree.TreeNode<T>>,
  props: DataTableProps<T>
): RT.ColumnDef<Tree.TreeNode<T>> => helper.display({
  id: ROW_HEADER_ID,
  header: api => (
    <div>
      <Input.Button
        icon={Icon.MinusIcon} iconOnly small outlined className="m-1"
        onClick={() => api.table.toggleAllRowsExpanded()}>
        折りたたみ
      </Input.Button>
    </div>
  ),
  cell: api => (
    <div className="inline-flex gap-1 w-full"
      style={{ paddingLeft: api.row.depth * 24 }}>

      <Input.Button
        iconOnly small
        icon={api.row.getIsExpanded() ? Icon.ChevronDownIcon : Icon.ChevronRightIcon}
        className={(api.row.subRows.length === 0) ? 'invisible' : undefined}
        onClick={e => { api.row.toggleExpanded(); e.stopPropagation() }}>
        折りたたむ
      </Input.Button>

      <span className="flex-1 whitespace-nowrap">
        {props.treeView?.rowHeader(api.row.original.item)}
      </span>
    </div>
  ),
})
const ROW_HEADER_ID = '__tree_explorer_row_header__'

// -----------------------------------------------
const DEFAULT_CELL: RT.ColumnDefTemplate<RT.CellContext<Util.TreeNode<unknown>, unknown>> = cellProps => {
  return (
    <span className="block w-full overflow-hidden whitespace-nowrap">
      {cellProps.getValue() as React.ReactNode}
      &nbsp; {/* <= すべての値が空の行がつぶれるのを防ぐ */}
    </span>
  )
}

// -----------------------------------------------
/** 列幅変更 */
const useColumnResizing = <T,>(api: RT.Table<Tree.TreeNode<T>>) => {

  const columnSizeVars = useMemo(() => {
    const headers = api.getFlatHeaders()
    const colSizes: { [key: string]: number } = {}
    for (let i = 0; i < headers.length; i++) {
      const header = headers[i]!
      colSizes[`--header-${header.id}-size`] = header.getSize()
      colSizes[`--col-${header.column.id}-size`] = header.column.getSize()
    }
    return colSizes
  }, [api.getState().columnSizingInfo])

  const getColWidth = useCallback((header: RT.Header<Tree.TreeNode<T>, unknown>) => {
    return `calc(var(--header-${header?.id}-size) * 1px)`
  }, [])

  const ResizeHandler = useCallback(({ header }: {
    header: RT.Header<Tree.TreeNode<T>, unknown>
  }) => {
    return (
      <div {...{
        onDoubleClick: () => header.column.resetSize(),
        onMouseDown: header.getResizeHandler(),
        onTouchStart: header.getResizeHandler(),
        className: `absolute top-0 bottom-0 right-0 w-3 cursor-ew-resize border-r border-color-4`,
      }}>
      </div>
    )
  }, [])

  return {
    columnSizeVars,
    getColWidth,
    ResizeHandler,
  }
}

const COLUMN_RESIZE_OPTION: Partial<RT.TableOptions<Tree.TreeNode<any>>> = {
  defaultColumn: {
    minSize: 60,
    maxSize: 800,
  },
  columnResizeMode: 'onChange',
}

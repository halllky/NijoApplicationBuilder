import * as React from "react"
import { useRef, useState, useCallback, useEffect, useImperativeHandle } from "react"
import { EditableGridProps, EditableGridRef } from "./EditableGrid.d"
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
  type Row,
  type Header,
  type HeaderGroup,
  type Cell,
  type RowModel,
  type ColumnDef as TanStackColumnDef
} from '@tanstack/react-table'
import {
  useVirtualizer,
  type VirtualItem
} from '@tanstack/react-virtual'
import { useCellTypes } from "./cellType/useFieldArrayEx"
import type * as ReactHookForm from 'react-hook-form'

/**
 * 編集可能なグリッドを表示するコンポーネント
*/
export const EditableGrid = React.forwardRef(<TRow extends ReactHookForm.FieldValues,>(
  props: EditableGridProps<TRow>,
  ref: React.ForwardedRef<EditableGridRef<TRow>>
) => {
  const {
    rows,
    getColumnDefs,
    onChangeRow,
    showCheckBox,
    isReadOnly,
    className
  } = props

  // テーブルの参照
  const tableContainerRef = useRef<HTMLDivElement>(null)
  const tableBodyRef = useRef<HTMLDivElement>(null)

  // 列定義の取得
  const cellType = useCellTypes<TRow>()
  const columnDefs = getColumnDefs(cellType)

  // 選択状態の管理
  const [activeCell, setActiveCell] = useState<{ rowIndex: number; colIndex: number } | null>(null)
  const [selectedRange, setSelectedRange] = useState<{
    startRow: number;
    startCol: number;
    endRow: number;
    endCol: number;
  } | null>(null)
  const [selectedRows, setSelectedRows] = useState<Set<number>>(new Set())
  const [allRowsSelected, setAllRowsSelected] = useState(false)
  const [isEditing, setIsEditing] = useState(false)
  const [editValue, setEditValue] = useState<string>("")

  // 編集可否の判定
  const getIsReadOnly = useCallback((rowIndex: number): boolean => {
    if (isReadOnly === true) return true
    if (typeof isReadOnly === 'function' && rowIndex >= 0 && rowIndex < rows.length) {
      return isReadOnly(rows[rowIndex], rowIndex)
    }
    return false
  }, [isReadOnly, rows])

  // チェックボックス表示判定
  const getShouldShowCheckBox = useCallback((rowIndex: number): boolean => {
    if (!showCheckBox) return false
    if (showCheckBox === true) return true
    if (typeof showCheckBox === 'function' && rowIndex >= 0 && rowIndex < rows.length) {
      return showCheckBox(rows[rowIndex], rowIndex)
    }
    return false
  }, [showCheckBox, rows])

  // テーブル定義
  const columnHelper = createColumnHelper<TRow>()
  const columns = [
    // 行ヘッダー（チェックボックス列）
    columnHelper.display({
      id: 'rowHeader',
      header: () => (
        <div className="w-10 h-10 flex justify-center items-center">
          {showCheckBox && (
            <input
              type="checkbox"
              checked={allRowsSelected}
              onChange={(e) => {
                if (e.target.checked) {
                  const allIndices = new Set(rows.map((_, i) => i))
                  setSelectedRows(allIndices)
                  setAllRowsSelected(true)
                } else {
                  setSelectedRows(new Set())
                  setAllRowsSelected(false)
                }
              }}
            />
          )}
        </div>
      ),
      cell: ({ row }: { row: Row<TRow> }) => {
        const rowIndex = row.index
        return (
          <div className="w-10 h-8 flex justify-center items-center">
            {getShouldShowCheckBox(rowIndex) && (
              <input
                type="checkbox"
                checked={selectedRows.has(rowIndex)}
                onChange={(e) => {
                  const newSelectedRows = new Set(selectedRows)
                  if (e.target.checked) {
                    newSelectedRows.add(rowIndex)
                  } else {
                    newSelectedRows.delete(rowIndex)
                  }
                  setSelectedRows(newSelectedRows)
                  setAllRowsSelected(newSelectedRows.size === rows.length)
                }}
              />
            )}
          </div>
        )
      },
    }),
    // 列ヘッダーとデータ列
    ...columnDefs.map((colDef, colIndex) =>
      columnHelper.accessor(
        (row: TRow) => {
          // fieldPathがある場合そのパスに対応する値を取得
          if (colDef.fieldPath) {
            return colDef.fieldPath.split('.').reduce((obj: any, path) => {
              return obj && obj[path] !== undefined ? obj[path] : undefined
            }, row)
          }
          return undefined
        },
        {
          id: colDef.fieldPath || `col-${colIndex}`,
          header: () => colDef.header,
          cell: ({ row, column, getValue }: { row: Row<TRow>; column: any; getValue: () => any }) => {
            const rowIndex = row.index
            const isActive = activeCell?.rowIndex === rowIndex && activeCell?.colIndex === colIndex
            const isInRange = selectedRange &&
              rowIndex >= Math.min(selectedRange.startRow, selectedRange.endRow) &&
              rowIndex <= Math.max(selectedRange.startRow, selectedRange.endRow) &&
              colIndex >= Math.min(selectedRange.startCol, selectedRange.endCol) &&
              colIndex <= Math.max(selectedRange.startCol, selectedRange.endCol)

            // 編集モードの場合はテキスト入力を表示
            if (isActive && isEditing && !getIsReadOnly(rowIndex)) {
              return (
                <input
                  className="w-full h-full outline-none border-none p-1"
                  value={editValue}
                  onChange={(e) => setEditValue(e.target.value)}
                  onKeyDown={(e) => {
                    // Enter キーで編集を確定（ただしIME確定のEnterは除く）
                    if (e.key === 'Enter' && !e.nativeEvent.isComposing) {
                      e.preventDefault()
                      confirmEdit(rowIndex, colIndex)
                    }
                    // Escape キーで編集をキャンセル
                    else if (e.key === 'Escape') {
                      e.preventDefault()
                      setIsEditing(false)
                    }
                  }}
                  autoFocus
                />
              )
            }

            // 通常セル
            const cellValue = getValue()
            return (
              <div
                className={`p-1 h-8 w-full overflow-hidden ${isActive ? 'bg-blue-100' : ''} ${isInRange ? 'bg-blue-50' : ''}`}
                onClick={(e) => {
                  setActiveCell({ rowIndex, colIndex })
                  setSelectedRange({
                    startRow: rowIndex,
                    startCol: colIndex,
                    endRow: rowIndex,
                    endCol: colIndex
                  })
                }}
                onDoubleClick={() => {
                  if (!getIsReadOnly(rowIndex)) {
                    startEditing(rowIndex, colIndex)
                  }
                }}
              >
                {cellValue?.toString() || ''}
              </div>
            )
          }
        }
      )
    )
  ]

  const table = useReactTable({
    data: rows,
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  // 仮想化設定
  const { rows: tableRows } = table.getRowModel()

  const rowVirtualizer = useVirtualizer({
    count: tableRows.length,
    getScrollElement: () => tableBodyRef.current,
    estimateSize: () => 35, // 行の高さの推定値
    overscan: 5,
  })

  const columnVirtualizer = useVirtualizer({
    count: table.getAllColumns().length,
    getScrollElement: () => tableContainerRef.current,
    estimateSize: () => 150, // 列の幅の推定値
    horizontal: true,
    overscan: 2,
  })

  // ref用の公開メソッド
  useImperativeHandle(ref, () => ({
    getSelectedRows: () => {
      return Array.from(selectedRows).map(rowIndex => ({
        row: rows[rowIndex],
        rowIndex
      }))
    },
    selectRow: (startRowIndex: number, endRowIndex: number) => {
      const newSelectedRows = new Set<number>()
      for (let i = Math.min(startRowIndex, endRowIndex); i <= Math.max(startRowIndex, endRowIndex); i++) {
        if (i >= 0 && i < rows.length) {
          newSelectedRows.add(i)
        }
      }
      setSelectedRows(newSelectedRows)
      setAllRowsSelected(newSelectedRows.size === rows.length)
    }
  }), [rows, selectedRows])

  // 編集開始
  const startEditing = (rowIndex: number, colIndex: number) => {
    if (getIsReadOnly(rowIndex)) return

    const column = table.getAllColumns()[colIndex + 1] // +1 は行ヘッダー用
    if (!column) return

    const row = rows[rowIndex]
    if (!row) return

    const fieldPath = columnDefs[colIndex]?.fieldPath
    if (!fieldPath) return

    let value = fieldPath.split('.').reduce((obj: any, path) => {
      return obj && obj[path] !== undefined ? obj[path] : ''
    }, row)

    setEditValue(value?.toString() || '')
    setIsEditing(true)
  }

  // 編集確定
  const confirmEdit = (rowIndex: number, colIndex: number) => {
    if (!onChangeRow || getIsReadOnly(rowIndex)) {
      setIsEditing(false)
      return
    }

    const fieldPath = columnDefs[colIndex]?.fieldPath
    if (!fieldPath) {
      setIsEditing(false)
      return
    }

    // 変更を適用した新しい行オブジェクトを作成
    const newRow = { ...rows[rowIndex] }
    const paths = fieldPath.split('.')
    let current: any = newRow

    // ネストしたオブジェクトの場合、最後のプロパティ以外のパスを辿る
    for (let i = 0; i < paths.length - 1; i++) {
      if (!current[paths[i]]) {
        current[paths[i]] = {}
      }
      current = current[paths[i]]
    }

    // 値の型に応じて変換
    const lastPath = paths[paths.length - 1]
    if (typeof current[lastPath] === 'number') {
      current[lastPath] = Number(editValue)
    } else if (typeof current[lastPath] === 'boolean') {
      current[lastPath] = editValue.toLowerCase() === 'true'
    } else {
      current[lastPath] = editValue
    }

    // 変更を親コンポーネントに通知
    onChangeRow(newRow as TRow, rowIndex)
    setIsEditing(false)
  }

  // キーボードイベントのハンドリング
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!activeCell) return

      const { rowIndex, colIndex } = activeCell
      const rowCount = rows.length
      const colCount = columnDefs.length

      // 編集モード中は矢印キーを処理しない
      if (isEditing) return

      switch (e.key) {
        case 'ArrowUp':
          e.preventDefault()
          if (rowIndex > 0) {
            setActiveCell({ rowIndex: rowIndex - 1, colIndex })
            if (!e.shiftKey) {
              setSelectedRange({
                startRow: rowIndex - 1,
                startCol: colIndex,
                endRow: rowIndex - 1,
                endCol: colIndex
              })
            } else if (selectedRange) {
              setSelectedRange({
                ...selectedRange,
                endRow: rowIndex - 1
              })
            }
          }
          break
        case 'ArrowDown':
          e.preventDefault()
          if (rowIndex < rowCount - 1) {
            setActiveCell({ rowIndex: rowIndex + 1, colIndex })
            if (!e.shiftKey) {
              setSelectedRange({
                startRow: rowIndex + 1,
                startCol: colIndex,
                endRow: rowIndex + 1,
                endCol: colIndex
              })
            } else if (selectedRange) {
              setSelectedRange({
                ...selectedRange,
                endRow: rowIndex + 1
              })
            }
          }
          break
        case 'ArrowLeft':
          e.preventDefault()
          if (colIndex > 0) {
            setActiveCell({ rowIndex, colIndex: colIndex - 1 })
            if (!e.shiftKey) {
              setSelectedRange({
                startRow: rowIndex,
                startCol: colIndex - 1,
                endRow: rowIndex,
                endCol: colIndex - 1
              })
            } else if (selectedRange) {
              setSelectedRange({
                ...selectedRange,
                endCol: colIndex - 1
              })
            }
          }
          break
        case 'ArrowRight':
          e.preventDefault()
          if (colIndex < colCount - 1) {
            setActiveCell({ rowIndex, colIndex: colIndex + 1 })
            if (!e.shiftKey) {
              setSelectedRange({
                startRow: rowIndex,
                startCol: colIndex + 1,
                endRow: rowIndex,
                endCol: colIndex + 1
              })
            } else if (selectedRange) {
              setSelectedRange({
                ...selectedRange,
                endCol: colIndex + 1
              })
            }
          }
          break
        case 'F2':
          e.preventDefault()
          if (!getIsReadOnly(rowIndex)) {
            startEditing(rowIndex, colIndex)
          }
          break
        case 'c':
          // Ctrl+C でコピー
          if (e.ctrlKey && selectedRange) {
            e.preventDefault()
            const startRow = Math.min(selectedRange.startRow, selectedRange.endRow)
            const endRow = Math.max(selectedRange.startRow, selectedRange.endRow)
            const startCol = Math.min(selectedRange.startCol, selectedRange.endCol)
            const endCol = Math.max(selectedRange.startCol, selectedRange.endCol)

            let copyText = ''
            for (let i = startRow; i <= endRow; i++) {
              let rowData = []
              for (let j = startCol; j <= endCol; j++) {
                const fieldPath = columnDefs[j]?.fieldPath
                if (fieldPath && i < rows.length) {
                  const value = fieldPath.split('.').reduce((obj: any, path) => {
                    return obj && obj[path] !== undefined ? obj[path] : ''
                  }, rows[i])
                  rowData.push(value?.toString() || '')
                } else {
                  rowData.push('')
                }
              }
              copyText += rowData.join('\t') + '\n'
            }

            navigator.clipboard.writeText(copyText).catch(err => {
              console.error('クリップボードへのコピーに失敗しました', err)
            })
          }
          break
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [activeCell, selectedRange, rows, columnDefs, isEditing, getIsReadOnly])

  // 初期状態設定
  useEffect(() => {
    if (rows.length > 0 && columnDefs.length > 0 && !activeCell) {
      setActiveCell({ rowIndex: 0, colIndex: 0 })
      setSelectedRange({
        startRow: 0,
        startCol: 0,
        endRow: 0,
        endCol: 0
      })
    }
  }, [rows, columnDefs, activeCell])

  // マウスドラッグの実装
  const [isDragging, setIsDragging] = useState(false)
  const [dragStartCell, setDragStartCell] = useState<{ rowIndex: number; colIndex: number } | null>(null)

  const handleMouseDown = (rowIndex: number, colIndex: number) => {
    setActiveCell({ rowIndex, colIndex })
    setSelectedRange({
      startRow: rowIndex,
      startCol: colIndex,
      endRow: rowIndex,
      endCol: colIndex
    })
    setDragStartCell({ rowIndex, colIndex })
    setIsDragging(true)
  }

  const handleMouseMove = (rowIndex: number, colIndex: number) => {
    if (isDragging && dragStartCell) {
      setSelectedRange({
        startRow: dragStartCell.rowIndex,
        startCol: dragStartCell.colIndex,
        endRow: rowIndex,
        endCol: colIndex
      })
    }
  }

  const handleMouseUp = () => {
    setIsDragging(false)
    setDragStartCell(null)
  }

  useEffect(() => {
    document.addEventListener('mouseup', handleMouseUp)
    return () => {
      document.removeEventListener('mouseup', handleMouseUp)
    }
  }, [])

  return (
    <div
      className={`overflow-auto border border-gray-300 ${className || ''}`}
      ref={tableContainerRef}
      style={{ height: '100%', width: '100%' }}
    >
      <div
        className="relative"
        style={{ width: columnVirtualizer.getTotalSize() + 'px' }}
      >
        {/* ヘッダー行 */}
        <div className="sticky top-0 bg-gray-100 z-10">
          {table.getHeaderGroups().map((headerGroup: HeaderGroup<TRow>) => (
            <div
              key={headerGroup.id}
              className="flex"
            >
              {columnVirtualizer.getVirtualItems().map((virtualColumn: VirtualItem) => {
                const column = headerGroup.headers[virtualColumn.index]
                if (!column) return null

                return (
                  <div
                    key={column.id}
                    className="border-b border-r border-gray-300 font-bold p-2"
                    style={{
                      position: 'absolute',
                      left: virtualColumn.start + 'px',
                      width: virtualColumn.size + 'px',
                      height: '40px',
                    }}
                  >
                    {column.isPlaceholder ? null : (
                      flexRender(
                        column.column.columnDef.header,
                        column.getContext()
                      )
                    )}
                  </div>
                )
              })}
            </div>
          ))}
        </div>

        {/* テーブル本体 */}
        <div
          ref={tableBodyRef}
          className="relative overflow-auto"
          style={{
            height: `calc(100% - 40px)`,
            width: '100%'
          }}
        >
          {rowVirtualizer.getVirtualItems().map((virtualRow: VirtualItem) => {
            const row = tableRows[virtualRow.index]
            if (!row) return null

            return (
              <div
                key={row.id}
                className="flex"
                style={{
                  position: 'absolute',
                  top: virtualRow.start + 'px',
                  height: `${virtualRow.size}px`,
                  width: '100%'
                }}
              >
                {columnVirtualizer.getVirtualItems().map((virtualColumn: VirtualItem) => {
                  const cell = row.getVisibleCells()[virtualColumn.index]
                  if (!cell) return null

                  return (
                    <div
                      key={cell.id}
                      className="border-b border-r border-gray-300"
                      style={{
                        position: 'absolute',
                        left: virtualColumn.start + 'px',
                        width: virtualColumn.size + 'px',
                        height: '100%',
                      }}
                      onMouseDown={() => handleMouseDown(virtualRow.index, virtualColumn.index - 1)} // -1 は行ヘッダー分の調整
                      onMouseMove={() => handleMouseMove(virtualRow.index, virtualColumn.index - 1)}
                    >
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
                    </div>
                  )
                })}
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}) as (<TRow extends ReactHookForm.FieldValues, >(props: EditableGridProps<TRow> & { ref: React.ForwardedRef<EditableGridRef<TRow>> }) => React.ReactNode)

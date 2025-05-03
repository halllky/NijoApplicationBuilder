import * as React from "react";
import { useRef, useState, useCallback, useEffect, useImperativeHandle, useMemo } from "react";
import { EditableGridProps, EditableGridRef } from "./index.d";
import {
  createColumnHelper,
  getCoreRowModel,
  useReactTable,
  type Row,
  type Cell,
  flexRender
} from '@tanstack/react-table';
import {
  useVirtualizer,
  type VirtualItem,
  notUndefined
} from '@tanstack/react-virtual';
import { useCellTypes } from "./useCellTypes";
import type * as ReactHookForm from 'react-hook-form';
import { getValueByPath } from "./EditableGrid.utils";

// コンポーネントのインポート
import { RowCheckboxCell } from "./EditableGrid.RowCheckboxCell";
import { EmptyDataMessage } from "./EditableGrid.EmptyDataMessage";

// カスタムフックのインポート
import { useSelection } from "./EditableGrid.hooks/useSelection";
import { useEditing } from "./EditableGrid.hooks/useEditing";
import { useGridKeyboard } from "./EditableGrid.hooks/useGridKeyboard";
import { useDragSelection } from "./EditableGrid.hooks/useDragSelection";

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
  } = props;

  // テーブルの参照
  const tableContainerRef = useRef<HTMLDivElement>(null);
  const tableBodyRef = useRef<HTMLTableSectionElement>(null);

  // 列定義の取得
  const cellType = useCellTypes<TRow>();
  const columnDefs = getColumnDefs(cellType);

  // 列のデフォルトサイズ (8rem をピクセル換算。環境依存可能性あり)
  const defaultColumnWidth = 128;

  // 列状態 (サイズ変更用)
  const [columnSizing, setColumnSizing] = useState({});

  // チェックボックス表示判定
  const getShouldShowCheckBox = useCallback((rowIndex: number): boolean => {
    if (!showCheckBox) return false;
    if (showCheckBox === true) return true;
    if (typeof showCheckBox === 'function' && rowIndex >= 0 && rowIndex < rows.length) {
      return showCheckBox(rows[rowIndex], rowIndex);
    }
    return false;
  }, [showCheckBox, rows]);

  // 編集可否の判定
  const getIsReadOnly = useCallback((rowIndex: number): boolean => {
    if (isReadOnly === true) return true;
    if (typeof isReadOnly === 'function' && rowIndex >= 0 && rowIndex < rows.length) {
      return isReadOnly(rows[rowIndex], rowIndex);
    }
    return false;
  }, [isReadOnly, rows]);

  // 選択状態の管理をフックに移動
  const selection = useSelection(rows.length);
  const {
    activeCell,
    selectedRange,
    selectedRows,
    allRowsSelected,
    setActiveCell,
    setSelectedRange,
    handleCellClick,
    handleToggleAllRows,
    handleToggleRow,
    selectRows
  } = selection;

  // 編集機能の管理をフックに移動
  const editing = useEditing<TRow>(rows, columnDefs, onChangeRow, isReadOnly);
  const {
    isEditing,
    editValue,
    startEditing,
    confirmEdit,
    cancelEdit,
    handleEditValueChange
  } = editing;

  // ドラッグ選択機能の管理をフックに移動
  const dragSelection = useDragSelection(setActiveCell, setSelectedRange);
  const {
    isDragging,
    handleMouseDown,
    handleMouseMove
  } = dragSelection;

  // キーボード操作の管理をフックに移動
  useGridKeyboard({
    activeCell,
    selectedRange,
    isEditing,
    rowCount: rows.length,
    colCount: columnDefs.length,
    setActiveCell,
    setSelectedRange,
    startEditing,
    getIsReadOnly
  });

  // テーブル定義
  const columnHelper = createColumnHelper<TRow>();
  const columns = useMemo(() => [
    // 行ヘッダー（チェックボックス列）
    columnHelper.display({
      id: 'rowHeader',
      size: 32,
      enableResizing: false,
      header: ({ table }) => {
        const handleClick = (e: React.MouseEvent) => {
          e.stopPropagation(); // イベント伝播を停止

          // 全セルを選択
          setSelectedRange({
            startRow: 0,
            startCol: 0,
            endRow: rows.length - 1,
            endCol: columnDefs.length - 1
          });

          // アクティブセルを左上ボディセルに設定
          if (rows.length > 0 && columnDefs.length > 0) {
            setActiveCell({ rowIndex: 0, colIndex: 0 });
          }
        };

        return (
          <div
            className="h-10 flex justify-center items-center cursor-pointer"
            onClick={handleClick} // onClickハンドラを追加
          >
            {showCheckBox && (
              <input
                type="checkbox"
                checked={table.getIsAllRowsSelected()}
                onChange={(e) => handleToggleAllRows(e.target.checked)}
                aria-label="全行選択"
              />
            )}
          </div>
        );
      },
      cell: ({ row }: { row: Row<TRow> }) => {
        const rowIndex = row.index;
        return (
          <div className="h-8 flex justify-center items-center">
            {getShouldShowCheckBox(rowIndex) && (
              <input
                type="checkbox"
                checked={selectedRows.has(rowIndex)}
                onChange={(e) => handleToggleRow(rowIndex, e.target.checked)}
                aria-label={`行${rowIndex + 1}を選択`}
              />
            )}
          </div>
        );
      },
    }),
    // 列ヘッダーとデータ列
    ...columnDefs.map((colDef, colIndex) =>
      columnHelper.accessor(
        (row: TRow) => {
          // fieldPathがある場合そのパスに対応する値を取得
          if (colDef.fieldPath) {
            return getValueByPath(row, colDef.fieldPath);
          }
          return undefined;
        },
        {
          id: colDef.fieldPath || `col-${colIndex}`,
          header: colDef.header,
          size: colDef.width ?? defaultColumnWidth,
          enableResizing: colDef.resizable ?? true,
          cell: ({ getValue }: { getValue: () => any }) => {
            // tbody側で編集/表示の切り替えやイベントハンドラを設定するため、
            // ここでは単純に値を表示する or 基本的なラッパーコンポーネントを返す程度に留める
            return (
              <div className="p-1 h-8 w-full overflow-hidden">
                {getValue()?.toString() || ''}
              </div>
            );
          }
        }
      )
    )
  ], [columnDefs, showCheckBox, allRowsSelected, handleToggleAllRows, getShouldShowCheckBox, selectedRows, handleToggleRow, activeCell, selectedRange, handleCellClick, getIsReadOnly, startEditing, isEditing, editValue, handleEditValueChange, confirmEdit, cancelEdit, columnHelper]);

  const table = useReactTable({
    data: rows,
    columns,
    columnResizeMode: 'onChange',
    state: {
      columnSizing,
    },
    onColumnSizingChange: setColumnSizing,
    getCoreRowModel: getCoreRowModel(),
    enableColumnResizing: true,
    defaultColumn: {
      size: defaultColumnWidth,
      minSize: 50,
      maxSize: 500,
    },
  });

  // テーブル全体の合計幅を取得 (最初のヘッダーグループから)
  const tableTotalWidth = table.getTotalSize();

  // 仮想化設定
  const { rows: tableRows } = table.getRowModel();

  const rowVirtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => tableBodyRef.current,
    estimateSize: () => 35,
    overscan: 5,
  });

  const virtualItems = rowVirtualizer.getVirtualItems();

  // Before/After 行の高さを計算
  const [paddingTop, paddingBottom] = useMemo(() => {
    if (!virtualItems.length) {
      return [0, 0];
    }
    return [
      notUndefined(virtualItems[0]).start - rowVirtualizer.options.scrollMargin,
      rowVirtualizer.getTotalSize() - notUndefined(virtualItems[virtualItems.length - 1]).end,
    ];
  }, [virtualItems, rowVirtualizer]);

  // ref用の公開メソッド
  useImperativeHandle(ref, () => ({
    getSelectedRows: () => Array.from(selectedRows).map(rowIndex => ({
      row: rows[rowIndex],
      rowIndex,
    })),
    selectRow: selectRows,
    getActiveCell: () => activeCell ?? undefined,
    getSelectedRange: () => selectedRange ?? undefined,
  }), [selectedRows, rows, selectRows, activeCell, selectedRange]);

  // 初期状態設定
  useEffect(() => {
    if (rows.length > 0 && columnDefs.length > 0 && !activeCell) {
      setActiveCell({ rowIndex: 0, colIndex: 0 });
      setSelectedRange({
        startRow: 0,
        startCol: 0,
        endRow: 0,
        endCol: 0
      });
    }
  }, [rows, columnDefs, activeCell, setActiveCell, setSelectedRange]);

  return (
    <div
      ref={tableContainerRef}
      className={`overflow-auto resize-y bg-gray-200 relative ${className ?? ''}`}
      onMouseMove={(e) => {
        if (isDragging && tableBodyRef.current) {
          // マウス位置からテーブル内の行と列のインデックスを計算
          const rect = tableBodyRef.current.getBoundingClientRect();
          const x = e.clientX - rect.left;
          const y = e.clientY - rect.top;

          // 行インデックスの計算（仮想化を考慮）
          const rowHeight = 35; // 推定行高さ
          const visibleStartRow = Math.floor(tableBodyRef.current.scrollTop / rowHeight);
          const rowIndex = visibleStartRow + Math.floor(y / rowHeight);

          // 列インデックスの計算（簡易的）
          const colWidth = rect.width / columnDefs.length;
          const colIndex = Math.floor(x / colWidth);

          if (rowIndex >= 0 && rowIndex < rows.length && colIndex >= 0 && colIndex < columnDefs.length) {
            handleMouseMove(rowIndex, colIndex);
          }
        }
      }}
    >
      <table
        className="border-collapse table-fixed"
        style={{ minWidth: tableTotalWidth }}
      >
        <thead className="sticky top-0 z-10 bg-gray-100 grid-header-group">
          {table.getHeaderGroups().map(headerGroup => (
            <tr key={headerGroup.id}>
              {headerGroup.headers.map(header => (
                <th
                  key={header.id}
                  className="border border-gray-300 px-1 relative text-left select-none"
                  style={{ width: header.getSize() }}
                >
                  {header.isPlaceholder
                    ? null
                    : flexRender(
                      header.column.columnDef.header,
                      header.getContext()
                    )}
                  {header.column.getCanResize() && (
                    <div
                      onMouseDown={header.getResizeHandler()}
                      onTouchStart={header.getResizeHandler()}
                      className={`absolute top-0 right-0 h-full w-1.5 cursor-col-resize select-none touch-none ${header.column.getIsResizing() ? 'bg-blue-500 opacity-50' : 'hover:bg-gray-400'}`}
                    >
                    </div>
                  )}
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody ref={tableBodyRef} className="grid-body">
          {/* 上部パディング行 */}
          {paddingTop > 0 && (
            <tr>
              <td
                style={{ height: `${paddingTop}px` }}
                colSpan={table.getAllColumns().length}
              />
            </tr>
          )}

          {/* 仮想化された行 */}
          {virtualItems.map(virtualRow => {
            const row = tableRows[virtualRow.index];
            if (!row) return null;

            return (
              <tr
                key={row.id}
                className="bg-gray-100"
                style={{ height: `${virtualRow.size}px` }}
              >
                {row.getVisibleCells().map(cell => {
                  const rowIndex = row.index;
                  const colIndex = cell.column.id === 'rowHeader' ? -1 : columnDefs.findIndex(col =>
                    col.fieldPath === cell.column.id || `col-${col.fieldPath}` === cell.column.id
                  );

                  const isActive = activeCell?.rowIndex === rowIndex && activeCell?.colIndex === colIndex;
                  const isInRange = Boolean(selectedRange &&
                    rowIndex >= selectedRange.startRow && rowIndex <= selectedRange.endRow &&
                    colIndex >= selectedRange.startCol && colIndex <= selectedRange.endCol);

                  if (cell.column.id === 'rowHeader') {
                    return (
                      <td
                        key={cell.id}
                        className="border border-gray-300 align-middle text-center"
                        style={{ width: cell.column.getSize() }}
                      >
                        <RowCheckboxCell
                          rowIndex={rowIndex}
                          isChecked={selectedRows.has(rowIndex)}
                          onToggle={(checked) => handleToggleRow(rowIndex, checked)}
                          showCheckBox={getShouldShowCheckBox(rowIndex)}
                        />
                      </td>
                    );
                  }

                  return (
                    <td
                      key={cell.id}
                      className={`border border-gray-300 outline-none p-1 align-middle ${isActive ? 'bg-blue-200' : ''} ${isInRange ? 'bg-blue-100' : ''}`}
                      style={{ width: cell.column.getSize() }}
                      onClick={(e) => handleCellClick(e, rowIndex, colIndex)}
                      onDoubleClick={() => {
                        if (!getIsReadOnly(rowIndex)) {
                          startEditing(rowIndex, colIndex);
                        }
                      }}
                      onMouseDown={() => {
                        if (!isEditing) {
                          handleMouseDown(rowIndex, colIndex);
                        }
                      }}
                      onKeyDown={(e) => {
                        if (e.key === 'F2' && !isEditing && !getIsReadOnly(rowIndex)) {
                          startEditing(rowIndex, colIndex);
                        } else if (e.key === 'Enter' && isEditing) {
                          confirmEdit();
                        } else if (e.key === 'Escape' && isEditing) {
                          cancelEdit();
                        }
                      }}
                      tabIndex={0}
                    >
                      {isEditing && isActive ? (
                        <input
                          type="text"
                          value={editValue}
                          onChange={(e) => handleEditValueChange(e.target.value)}
                          onKeyDown={(e) => {
                            e.stopPropagation();
                            if (e.key === 'Enter') {
                              confirmEdit();
                            } else if (e.key === 'Escape') {
                              cancelEdit();
                            }
                          }}
                          autoFocus
                          className="w-full h-full p-1 border-none outline-none"
                        />
                      ) : (
                        <div className="select-none overflow-hidden text-ellipsis whitespace-nowrap">
                          {cell.getValue()?.toString() || ''}
                        </div>
                      )}
                    </td>
                  );
                })}
              </tr>
            );
          })}

          {/* 下部パディング行 */}
          {paddingBottom > 0 && (
            <tr>
              <td
                style={{ height: `${paddingBottom}px` }}
                colSpan={table.getAllColumns().length}
              />
            </tr>
          )}

          {/* データが空の場合のメッセージ */}
          {rows.length === 0 && (
            <tr>
              <td colSpan={table.getAllColumns().length}>
                <EmptyDataMessage />
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}) as (<TRow extends ReactHookForm.FieldValues, >(props: EditableGridProps<TRow> & { ref: React.ForwardedRef<EditableGridRef<TRow>> }) => React.ReactNode);

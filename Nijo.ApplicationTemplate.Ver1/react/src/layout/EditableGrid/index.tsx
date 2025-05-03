import * as React from "react";
import { useRef, useState, useCallback, useEffect, useImperativeHandle, useMemo } from "react";
import { EditableGridProps, EditableGridRef } from "./index.d";
import {
  createColumnHelper,
  getCoreRowModel,
  useReactTable,
  type Row,
  type Cell,
} from '@tanstack/react-table';
import {
  useVirtualizer,
  type VirtualItem
} from '@tanstack/react-virtual';
import { useCellTypes } from "../cellType/useFieldArrayEx";
import type * as ReactHookForm from 'react-hook-form';
import { getValueByPath } from "./EditableGrid.utils";

// コンポーネントのインポート
import { HeaderCell } from "./EditableGrid.HeaderCell";
import { CheckboxHeaderCell } from "./EditableGrid.CheckboxHeaderCell";
import { RowCheckboxCell } from "./EditableGrid.RowCheckboxCell";
import { DataCell } from "./EditableGrid.DataCell";
import { EmptyDataMessage } from "./EditableGrid.EmptyDataMessage";

// カスタムフックのインポート
import { useSelection } from "./EditableGrid.hooks/useSelection";
import { useEditing } from "./EditableGrid.hooks/useEditing";
import { useGridKeyboard } from "./EditableGrid.hooks/useGridKeyboard";
import { useDragSelection } from "./EditableGrid.hooks/useDragSelection";

// --- デバッグログ用追加 ---
let renderCount = 0;
// --- デバッグログ用追加 ---

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
  const tableBodyRef = useRef<HTMLDivElement>(null);

  // 列定義の取得
  const cellType = useCellTypes<TRow>();
  const columnDefs = getColumnDefs(cellType);

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
      header: () => (
        <div className="w-10 h-10 flex justify-center items-center">
          {showCheckBox && (
            <input
              type="checkbox"
              checked={allRowsSelected}
              onChange={(e) => handleToggleAllRows(e.target.checked)}
              aria-label="全行選択"
            />
          )}
        </div>
      ),
      cell: ({ row }: { row: Row<TRow> }) => {
        const rowIndex = row.index;
        return (
          <div className="w-10 h-8 flex justify-center items-center">
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
          header: () => colDef.header,
          cell: ({ row, column, getValue }: { row: Row<TRow>; column: any; getValue: () => any }) => {
            const rowIndex = row.index;
            const isActive = activeCell?.rowIndex === rowIndex && activeCell?.colIndex === colIndex;
            const isInRange = Boolean(selectedRange &&
              rowIndex >= Math.min(selectedRange.startRow, selectedRange.endRow) &&
              rowIndex <= Math.max(selectedRange.startRow, selectedRange.endRow) &&
              colIndex >= Math.min(selectedRange.startCol, selectedRange.endCol) &&
              colIndex <= Math.max(selectedRange.startCol, selectedRange.endCol));

            return (
              <div
                className={`p-1 h-8 w-full overflow-hidden ${isActive ? 'bg-blue-100' : ''} ${isInRange ? 'bg-blue-50' : ''}`}
                onClick={() => handleCellClick(rowIndex, colIndex)}
                onDoubleClick={() => {
                  if (!getIsReadOnly(rowIndex)) {
                    startEditing(rowIndex, colIndex);
                  }
                }}
              >
                {getValue()?.toString() || ''}
              </div>
            );
          }
        }
      )
    )
  ], [columnDefs, showCheckBox, allRowsSelected, handleToggleAllRows, getShouldShowCheckBox, selectedRows, handleToggleRow, activeCell, selectedRange, handleCellClick, getIsReadOnly, startEditing, columnHelper]);

  const table = useReactTable({
    data: rows,
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  // --- デバッグログ用追加 ---
  useEffect(() => {
    console.log(`[EditableGrid] props.rows changed (length: ${rows.length})`, rows);
  }, [rows]);
  // --- デバッグログ用追加 ---

  // 仮想化設定
  const { rows: tableRows } = table.getRowModel();

  // --- デバッグログ用追加 ---
  useEffect(() => {
    console.log('[EditableGrid] tableBodyRef.current:', tableBodyRef.current);
  }, []); // 初回レンダリング後のみ実行
  // --- デバッグログ用追加 ---

  useEffect(() => {
    console.log(`[EditableGrid] tableRows changed (length: ${tableRows.length})`, tableRows);
  }, [tableRows]);
  // --- デバッグログ用追加 ---

  const rowVirtualizer = useVirtualizer({
    count: tableRows.length,
    getScrollElement: () => {
      // --- デバッグログ用追加 ---
      console.log('[EditableGrid] rowVirtualizer getScrollElement called. tableBodyRef.current:', tableBodyRef.current);
      // --- デバッグログ用追加 ---
      return tableBodyRef.current;
    },
    estimateSize: () => 35, // 行の高さの推定値
    overscan: 5,
  });

  // --- デバッグログ用追加 ---
  const virtualItems = rowVirtualizer.getVirtualItems();
  useEffect(() => {
    console.log(`[EditableGrid] virtualItems changed (count: ${virtualItems.length})`, virtualItems);
  }, [virtualItems]);
  renderCount++;
  console.log(`[EditableGrid] Render #${renderCount}`);
  // --- デバッグログ用追加 ---

  const columnVirtualizer = useVirtualizer({
    count: table.getAllColumns().length,
    getScrollElement: () => tableContainerRef.current,
    estimateSize: () => 150, // 列の幅の推定値
    horizontal: true,
    overscan: 2,
  });

  // ref用の公開メソッド
  useImperativeHandle(ref, () => ({
    getSelectedRows: () => {
      return Array.from(selectedRows).map(rowIndex => ({
        row: rows[rowIndex],
        rowIndex
      }));
    },
    selectRow: selectRows
  }), [rows, selectedRows, selectRows]);

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
      className={`overflow-auto border border-gray-300 ${className || ''}`}
      ref={tableContainerRef}
      style={{ height: '100%', width: '100%' }}
      role="grid"
      aria-rowcount={rows.length + 1} // +1 for header row
      aria-colcount={columnDefs.length + 1} // +1 for row header
    >
      {rows.length === 0 ? (
        <EmptyDataMessage />
      ) : (
        <div
          className="relative"
          style={{ width: columnVirtualizer.getTotalSize() + 'px' }}
        >
          {/* ヘッダー行 */}
          <div
            className="sticky top-0 bg-gray-100 z-10"
            role="rowgroup"
          >
            {table.getHeaderGroups().map(headerGroup => (
              <div
                key={headerGroup.id}
                className="flex"
                role="row"
                aria-rowindex={1}
              >
                {columnVirtualizer.getVirtualItems().map(virtualColumn => {
                  const header = headerGroup.headers[virtualColumn.index];
                  if (!header) return null;

                  return virtualColumn.index === 0 ? (
                    <CheckboxHeaderCell
                      key={header.id}
                      allRowsSelected={allRowsSelected}
                      onToggleAllRows={handleToggleAllRows}
                      showCheckBox={showCheckBox}
                      virtualColumn={virtualColumn}
                    />
                  ) : (
                    <HeaderCell
                      key={header.id}
                      header={header}
                      virtualColumn={virtualColumn}
                    />
                  );
                })}
              </div>
            ))}
          </div>

          {/* テーブル本体 */}
          <div
            ref={tableBodyRef}
            className="overflow-auto"
            style={{
              height: '200px',
              position: 'relative',
            }}
          >
            <div // 仮想アイテム配置用コンテナ
              className="relative overflow-auto"
              style={{
                height: `calc(100% - 40px)`,
                width: '100%'
              }}
              role="rowgroup"
            >
              {rowVirtualizer.getVirtualItems().map(virtualRow => {
                const row = tableRows[virtualRow.index];
                if (!row) return null;

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
                    role="row"
                    aria-rowindex={virtualRow.index + 2} // +2 for header row and 0-indexing
                  >
                    {columnVirtualizer.getVirtualItems().map(virtualColumn => {
                      const rowIndex = virtualRow.index;

                      // 行ヘッダー（チェックボックス列）
                      if (virtualColumn.index === 0) {
                        return (
                          <RowCheckboxCell
                            key={`${row.id}-checkbox`}
                            rowIndex={rowIndex}
                            isSelected={selectedRows.has(rowIndex)}
                            showCheckBox={getShouldShowCheckBox(rowIndex)}
                            onToggleRow={handleToggleRow}
                            virtualColumn={virtualColumn}
                          />
                        );
                      }

                      // データセル
                      const cell = row.getVisibleCells()[virtualColumn.index];
                      if (!cell) return null;

                      const colIndex = virtualColumn.index - 1; // -1 for row header adjustment
                      const isActive = activeCell?.rowIndex === rowIndex && activeCell?.colIndex === colIndex;
                      const isInRange = Boolean(selectedRange &&
                        rowIndex >= Math.min(selectedRange.startRow, selectedRange.endRow) &&
                        rowIndex <= Math.max(selectedRange.startRow, selectedRange.endRow) &&
                        colIndex >= Math.min(selectedRange.startCol, selectedRange.endCol) &&
                        colIndex <= Math.max(selectedRange.startCol, selectedRange.endCol));

                      return (
                        <DataCell
                          key={cell.id}
                          cell={cell}
                          rowIndex={rowIndex}
                          colIndex={colIndex}
                          isActive={isActive}
                          isInRange={isInRange}
                          isReadOnly={getIsReadOnly(rowIndex)}
                          isEditing={isEditing}
                          editValue={editValue}
                          onEditValueChange={handleEditValueChange}
                          onConfirmEdit={() => confirmEdit(rowIndex, colIndex)}
                          onCancelEdit={cancelEdit}
                          onStartEditing={() => startEditing(rowIndex, colIndex)}
                          onClickCell={() => handleCellClick(rowIndex, colIndex)}
                          onMouseDown={() => handleMouseDown(rowIndex, colIndex)}
                          onMouseMove={() => handleMouseMove(rowIndex, colIndex)}
                          virtualColumn={virtualColumn}
                        />
                      );
                    })}
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}) as (<TRow extends ReactHookForm.FieldValues, >(props: EditableGridProps<TRow> & { ref: React.ForwardedRef<EditableGridRef<TRow>> }) => React.ReactNode);

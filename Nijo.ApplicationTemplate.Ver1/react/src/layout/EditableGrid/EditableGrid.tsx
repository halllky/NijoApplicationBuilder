import * as React from "react";
import { useRef, useState, useCallback, useEffect, useImperativeHandle, useMemo } from "react";
import { EditableGridProps, EditableGridRef, EditableGridColumnDef, EditableGridColumnDefRenderCell } from "./types";
import {
  createColumnHelper,
  getCoreRowModel,
  useReactTable,
  flexRender,
  ColumnSizingState
} from '@tanstack/react-table';
import {
  useVirtualizer,
  notUndefined
} from '@tanstack/react-virtual';
import { useCellTypes } from "./useCellTypes";
import type * as ReactHookForm from 'react-hook-form';
import { getValueByPath } from "./EditableGrid.utils";

// コンポーネントのインポート
import { EmptyDataMessage } from "./EditableGrid.EmptyDataMessage";

// カスタムフックのインポート
import { useSelection } from "./EditableGrid.useSelection";
import { useEditing } from "./EditableGrid.useEditing";
import { useGridKeyboard } from "./EditableGrid.useGridKeyboard";
import { useDragSelection } from "./EditableGrid.useDragSelection";

// CSS
import "./EditableGrid.css";

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
    showCheckBox,
    isReadOnly,
    className
  } = props;

  // テーブルの参照
  const tableContainerRef = useRef<HTMLDivElement>(null);
  const tableBodyRef = useRef<HTMLTableSectionElement>(null);

  // 列定義の取得
  const cellType = useCellTypes<TRow>()
  const columnDefs = React.useMemo(() => {
    return getColumnDefs(cellType)
  }, [getColumnDefs, cellType])

  // 列状態 (サイズ変更用)
  const [columnSizing, setColumnSizing] = useState<ColumnSizingState>(() => ({
    'rowHeader': ROW_HEADER_WIDTH
  }));

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

  // 選択状態
  const {
    activeCell,
    selectedRange,
    checkedRows,
    allRowsChecked,
    setActiveCell,
    setSelectedRange,
    handleCellClick,
    handleToggleAllRows,
    handleToggleRow,
    selectRows
  } = useSelection(rows.length)

  // 編集機能
  const {
    isEditing,
    editValue,
    startEditing,
    startEditingWithCharacter,
    confirmEdit,
    cancelEdit,
    handleEditValueChange
  } = useEditing<TRow>(props, columnDefs, isReadOnly)

  // ドラッグ選択機能
  const {
    isDragging,
    handleMouseDown,
    handleMouseMove
  } = useDragSelection(setActiveCell, setSelectedRange)

  // キーボード操作
  const rowVirtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => tableContainerRef.current,
    estimateSize: () => ESTIMATED_ROW_HEIGHT,
    overscan: 5,
  });

  useGridKeyboard({
    activeCell,
    selectedRange,
    isEditing,
    rowCount: rows.length,
    colCount: columnDefs.length,
    setActiveCell,
    setSelectedRange,
    startEditing,
    startEditingWithCharacter,
    getIsReadOnly,
    rowVirtualizer,
    tableContainerRef,
  });

  // テーブル定義
  const columnHelper = createColumnHelper<TRow>();
  const columns = useMemo(() => [
    // 行ヘッダー（チェックボックス列）
    columnHelper.display({
      id: 'rowHeader',
      enableResizing: false,
      header: () => {
        const handleClick = (e: React.MouseEvent) => {
          e.stopPropagation(); // イベント伝播を停止

          // アクティブセルを左上ボディセルに設定
          if (rows.length > 0 && columnDefs.length > 0) {
            setActiveCell({ rowIndex: 0, colIndex: 0 });
          }
        };

        return (
          <div
            className="flex justify-center items-center border-b border-r border-gray-300"
            onClick={handleClick}
            style={{ height: ESTIMATED_ROW_HEIGHT }}
          >
            {showCheckBox && (
              <input
                type="checkbox"
                checked={allRowsChecked}
                onChange={(e) => handleToggleAllRows(e.target.checked)}
                aria-label="全行選択"
              />
            )}
          </div>
        );
      },
      meta: {
        isRowHeader: true,
        originalColDef: undefined,
      } satisfies ColumnMetadataInternal<TRow>,
    }),
    // 列ヘッダーとデータ列
    ...columnDefs
      .map((colDef: EditableGridColumnDef<TRow>, colIndex: number) => {
        const accessor = (row: TRow) => colDef.fieldPath
          ? getValueByPath(row, colDef.fieldPath)
          : undefined
        const tableColumnDef = columnHelper.accessor(accessor, {
          id: `col-${colIndex}`, // 元のインデックスをIDに使用
          size: colDef.defaultWidth ?? DEFAULT_COLUMN_WIDTH,
          enableResizing: colDef.enableResizing ?? true,
          header: ({ header }) => (
            <div
              className="flex pl-1 border-b border-r border-gray-300 text-gray-700 font-normal select-none"
              style={{ width: header.getSize() }}
            >
              <span className="truncate">
                {colDef.header}
              </span>
            </div>
          ),
          meta: {
            originalColDef: colDef,
            isRowHeader: false,
          } satisfies ColumnMetadataInternal<TRow>,
        });
        return tableColumnDef;
      })
  ], [columnDefs, showCheckBox, allRowsChecked, handleToggleAllRows, columnHelper, rows.length, setActiveCell]); // 依存配列を適切に設定

  const table = useReactTable({
    data: rows,
    columns,
    columnResizeMode: 'onChange',
    state: {
      columnSizing,
      columnVisibility: {
        'rowHeader': showCheckBox !== undefined && showCheckBox !== false,
        ...Object.fromEntries(columnDefs.map((colDef, i) => [`col-${i}`, !colDef.invisible])),
      },
    },
    onColumnSizingChange: setColumnSizing,
    getCoreRowModel: getCoreRowModel(),
    enableColumnResizing: true,
    defaultColumn: {
      size: DEFAULT_COLUMN_WIDTH,
      minSize: 8,
      maxSize: 500,
    },
  });

  // テーブル全体の合計幅を取得 (最初のヘッダーグループから)
  const tableTotalWidth = table.getTotalSize();

  // 仮想化設定
  const { rows: tableRows } = table.getRowModel();

  const virtualItems = rowVirtualizer.getVirtualItems();

  // 実際に表示されていない行の余白部分の高さを計算
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
    // チェックボックスで選択されている行
    getCheckedRows: () => Array.from(checkedRows).map(rowIndex => ({
      row: rows[rowIndex],
      rowIndex,
    })),
    // セルの範囲選択に含まれる行
    getSelectedRows: () => {
      if (!selectedRange) return []
      return Array
        .from({ length: selectedRange.endRow - selectedRange.startRow + 1 }, (_, i) => selectedRange.startRow + i)
        .map(rowIndex => ({ row: rows[rowIndex], rowIndex }))
    },
    selectRow: selectRows,
    getActiveCell: () => activeCell ?? undefined,
    getSelectedRange: () => selectedRange ?? undefined,
  }), [checkedRows, rows, selectRows, activeCell, selectedRange]);

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
      className={`overflow-auto bg-gray-200 relative ${className ?? ''}`}
      onMouseMove={(e) => {
        if (isDragging && tableBodyRef.current) {
          // マウス位置からテーブル内の行と列のインデックスを計算
          const rect = tableBodyRef.current.getBoundingClientRect();
          const x = e.clientX - rect.left;
          const y = e.clientY - rect.top;

          // 行インデックスの計算（仮想化を考慮）
          const rowHeight = ESTIMATED_ROW_HEIGHT; // 推定行高さ
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
        className={`table-fixed border-collapse border-spacing-0`}
        style={{ minWidth: tableTotalWidth }}
      >
        {/* 列幅を設定するためのcolgroup要素。table.getAllLeafColumns() を使うことで非表示の列を除外 */}
        <colgroup>
          {table.getAllLeafColumns().map(column => (
            <col key={column.id} style={{ width: column.getSize() }} />
          ))}
        </colgroup>

        {/* 列ヘッダ */}
        <thead className="sticky top-0 z-10 grid-header-group">
          {table.getHeaderGroups().map(headerGroup => (
            <tr key={headerGroup.id}>
              {headerGroup.headers.map(header => {
                const headerMeta = header.column.columnDef.meta as ColumnMetadataInternal<TRow> | undefined
                const isFixedColumn = !!headerMeta?.originalColDef?.isFixed;
                const isRowHeader = !!headerMeta?.isRowHeader;

                let className = 'bg-gray-200 relative text-left select-none'
                if (isRowHeader) className += ' sticky left-0 z-20'
                else if (isFixedColumn) className += ' sticky z-10'

                return (
                  <th
                    key={header.id}
                    className={className}
                    style={{
                      width: header.getSize(),
                      left: isFixedColumn ? `${header.getStart()}px` : undefined,
                    }}
                  >
                    {!header.isPlaceholder && flexRender(
                      header.column.columnDef.header,
                      header.getContext()
                    )}

                    {/* 列幅を変更できる場合はサイズ変更ハンドラを設定 */}
                    {header.column.getCanResize() && (
                      <div
                        onMouseDown={header.getResizeHandler()}
                        onTouchStart={header.getResizeHandler()}
                        className={`absolute top-0 right-0 h-full w-1.5 cursor-col-resize select-none touch-none ${header.column.getIsResizing() ? 'bg-blue-500 opacity-50' : 'hover:bg-gray-400'}`}
                      >
                      </div>
                    )}
                  </th>
                );
              })}
            </tr>
          ))}
        </thead>
        <tbody ref={tableBodyRef} className="grid-body">
          {/* 仮想化のための上部パディング行 */}
          {paddingTop > 0 && (
            <tr>
              <td
                style={{ height: `${paddingTop}px` }}
                colSpan={table.getAllColumns().length}
              />
            </tr>
          )}

          {/* 画面のスクロール範囲内に表示されている行のみレンダリングされる */}
          {virtualItems.map(virtualRow => {
            const row = tableRows[virtualRow.index];
            if (!row) return null;

            return (
              <tr key={row.id} style={{ height: `${virtualRow.size}px` }}>
                {row.getVisibleCells().map(cell => {
                  const rowIndex = row.index;
                  // 行ヘッダー列を除いた可視列の配列を取得し、その中でのインデックスを colIndex とする
                  const cellMeta = cell.column.columnDef.meta as ColumnMetadataInternal<TRow> | undefined;
                  const visibleDataColumns = table.getAllLeafColumns().filter(c => !(c.columnDef.meta as ColumnMetadataInternal<TRow>)?.isRowHeader);
                  const colIndex = cellMeta?.isRowHeader
                    ? -1 // 行ヘッダーの場合
                    : visibleDataColumns.findIndex(c => c.id === cell.column.id);

                  const isActive = activeCell?.rowIndex === rowIndex && activeCell?.colIndex === colIndex;
                  const isInRange = Boolean(selectedRange &&
                    rowIndex >= selectedRange.startRow && rowIndex <= selectedRange.endRow &&
                    colIndex >= selectedRange.startCol && colIndex <= selectedRange.endCol);

                  // 行選択チェックボックス列
                  if (cellMeta?.isRowHeader) {
                    return (
                      <td
                        key={cell.id}
                        className="bg-gray-200 align-middle text-center sticky left-0"
                        style={{ width: cell.column.getSize() }}
                      >
                        <div
                          className="h-full flex justify-center items-center border-r border-gray-300"
                          style={{ width: cell.column.getSize(), height: ESTIMATED_ROW_HEIGHT }}
                        >
                          {getShouldShowCheckBox(rowIndex) && (
                            <input
                              type="checkbox"
                              checked={checkedRows.has(rowIndex)}
                              onChange={(e) => handleToggleRow(rowIndex, e.target.checked)}
                              aria-label={`行${rowIndex + 1}を選択`}
                            />
                          )}
                        </div>
                      </td>
                    );
                  }

                  // データ列
                  let dataColumnClassName = 'outline-none align-middle'

                  if (isActive) {
                    dataColumnClassName += ' bg-blue-200' // アクティブセル
                  } else if (isInRange) {
                    dataColumnClassName += ' bg-blue-100' // 選択範囲
                  } else if (activeCell &&
                    (rowIndex === activeCell.rowIndex || colIndex === activeCell.colIndex) &&
                    !(rowIndex === activeCell.rowIndex && colIndex === activeCell.colIndex)
                  ) {
                    dataColumnClassName += ' editable-grid-active-cell-cross-direction' // アクティブセルの十字方向
                  } else {
                    dataColumnClassName += ' bg-gray-100' // その他
                  }

                  if (cellMeta?.originalColDef?.isFixed) dataColumnClassName += ` sticky` // z-indexをつけるとボディ列が列ヘッダより手前にきてしまうので設定しない

                  // 画面側でレンダリング処理が決められている場合はそれを使用、決まっていないなら単にtoString
                  const renderCell = (cell.column.columnDef.meta as ColumnMetadataInternal<TRow>)?.originalColDef?.renderCell

                  return (
                    <td
                      key={cell.id}
                      className={dataColumnClassName}
                      style={{
                        width: cell.column.getSize(),
                        left: cellMeta?.originalColDef?.isFixed ? `${cell.column.getStart()}px` : undefined,
                      }}
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
                      {/* セル編集中の場合はinput要素をレンダリング */}
                      {isEditing && isActive && (
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
                          className="w-full pl-1 outline outline-blue-500"
                        />
                      )}

                      {/* セル編集中でない場合はdiv要素をレンダリング */}
                      {(!isEditing || !isActive) && (
                        <div
                          className="flex pl-1 border-r border-gray-200 select-none truncate"
                          style={{ width: cell.column.getSize() }}
                        >
                          {renderCell?.(cell.getContext()) ?? cell.getValue()?.toString()}
                          &nbsp;
                        </div>
                      )}
                    </td>
                  );
                })}
              </tr>
            );
          })}

          {/* 仮想化のための下部パディング行 */}
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
}) as (<TRow extends ReactHookForm.FieldValues, >(props: EditableGridProps<TRow> & { ref?: React.ForwardedRef<EditableGridRef<TRow>> }) => React.ReactNode);

// ------------------------------------

/** このファイル内部でのみ使用 */
type ColumnMetadataInternal<TRow extends ReactHookForm.FieldValues> = {
  isRowHeader: boolean | undefined
  originalColDef: EditableGridColumnDef<TRow> | undefined
}

/** 推定行高さ */
const ESTIMATED_ROW_HEIGHT = 24
/** 行ヘッダー列の幅 */
const ROW_HEADER_WIDTH = 32
/** デフォルトの列幅。8rem をピクセル換算。環境依存可能性あり */
const DEFAULT_COLUMN_WIDTH = 128

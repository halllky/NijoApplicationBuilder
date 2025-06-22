import * as React from "react";
import { useRef, useState, useCallback, useEffect, useImperativeHandle, useMemo } from "react";
import { EditableGridProps, EditableGridRef, EditableGridColumnDef, EditableGridColumnDefRenderCell, CellPosition, EditableGridAutoSaveStoragedValueInternal } from "./types";
import {
  createColumnHelper,
  getCoreRowModel,
  useReactTable,
  flexRender,
  ColumnSizingState,
  OnChangeFn,
  Cell,
  Table
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
import { useGridKeyboard } from "./EditableGrid.useGridKeyboard";
import { useDragSelection } from "./EditableGrid.useDragSelection";
import { useCopyPaste } from "./EditableGrid.useCopyPaste";

// CSS
import "./EditableGrid.css";
import { CellEditor, CellEditorRef, DefaultEditor, useGetPixel } from "./EditableGrid.CellEditor";
import { ActiveCell } from "./EditableGrid.ActiveCell";

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
    onChangeRow,
    onActiveCellChanged: propsOnActiveCellChanged,
    className
  } = props;

  // 保存された状態の読み込み。コンポーネント初期化時のみ読み込む。
  React.useEffect(() => {
    if (props.storage) {
      try {
        const json = props.storage.loadState()
        if (json) {
          const obj: EditableGridAutoSaveStoragedValueInternal = JSON.parse(json)
          if (typeof obj === 'object' && (obj["column-sizing"] === undefined || typeof obj["column-sizing"] === 'object')) {
            setColumnSizing(obj["column-sizing"] ?? { [ROW_HEADER_COLUMN_ID]: ROW_HEADER_WIDTH })
          }
        }
      } catch {
        // 無視
      }
    }
  }, [])

  // テーブルの参照
  const tableContainerRef = useRef<HTMLDivElement>(null);
  const tableBodyRef = useRef<HTMLTableSectionElement>(null);
  const cellEditorRef = useRef<CellEditorRef<TRow>>(null);

  // 列定義の取得
  const cellType = useCellTypes<TRow>(props.onChangeRow)
  const columnDefs = React.useMemo(() => {
    return getColumnDefs(cellType)
  }, [getColumnDefs, cellType])

  // table インスタンスへの参照を保持 (コールバック内で最新の table を参照するため)
  const tableRef = useRef<ReturnType<typeof useReactTable<TRow>> | null>(null);

  // 列状態 (サイズ変更用)
  const [columnSizing, setColumnSizing] = useState<ColumnSizingState>(() => ({
    [ROW_HEADER_COLUMN_ID]: ROW_HEADER_WIDTH
  }));
  React.useEffect(() => {
    props.storage?.saveState(JSON.stringify({ 'column-sizing': columnSizing }))
  }, [columnSizing])

  // チェックボックス表示判定
  const getShouldShowCheckBox = useCallback((rowIndex: number): boolean => {
    if (!showCheckBox) return false;
    if (showCheckBox === true) return true;
    if (typeof showCheckBox === 'function' && rowIndex >= 0 && rowIndex < rows.length) {
      const row = tableRef.current?.getRow(rowIndex.toString())?.original
      if (row) return showCheckBox(row, rowIndex);
    }
    return false;
  }, [showCheckBox, tableRef]);

  // 編集可否の判定
  const getIsReadOnly = useCallback((rowIndex: number): boolean => {
    if (isReadOnly === true) return true;
    if (typeof isReadOnly === 'function' && rowIndex >= 0 && rowIndex < rows.length) {
      const row = tableRef.current?.getRow(rowIndex.toString())?.original
      if (row) return isReadOnly(row, rowIndex);
    }
    return false;
  }, [isReadOnly, tableRef]);

  // 編集状態管理
  const [isEditing, setIsEditing] = useState(false);
  const handleChangeEditing = useCallback((editing: boolean) => {
    setIsEditing(editing);
  }, []);

  // キーボードで文字入力したとき即座に編集を開始できるようにするため
  // アクティブセルが変更されるたびにセルエディタにフォーカスを当てる
  const onActiveCellChanged = useCallback((cell: CellPosition | null) => {

    // 編集中の場合は、エディタの値を変更しないようにする
    if (cell && cellEditorRef.current && tableRef.current && !isEditing) {
      const row = rows[cell.rowIndex]
      const visibleDataColumns = tableRef.current.getVisibleLeafColumns()
      const targetColumn = visibleDataColumns[cell.colIndex];
      if (targetColumn) {
        const meta = targetColumn.columnDef.meta as ColumnMetadataInternal<TRow> | undefined;
        const colDef = meta?.originalColDef;
        if (row && colDef && colDef.onStartEditing) {
          colDef.onStartEditing({
            rowIndex: cell.rowIndex,
            row: row,
            setEditorInitialValue: (value: string) => {
              if (cellEditorRef.current?.textarea) {
                cellEditorRef.current.setEditorInitialValue(value)
                window.setTimeout(() => {
                  cellEditorRef.current?.textarea?.focus()
                  cellEditorRef.current?.textarea?.select()
                }, 10)
              }
            },
          })
        }
      }
    }

    // セルが選択されたあとに発火されるコールバック
    propsOnActiveCellChanged?.(cell)
  }, [cellEditorRef, rows, columnDefs, tableRef, propsOnActiveCellChanged, isEditing])

  // 選択状態
  const {
    activeCell,
    selectedRange,
    checkedRows,
    allRowsChecked,
    anchorCellRef,
    setActiveCell,
    setSelectedRange,
    handleCellClick,
    handleToggleAllRows,
    handleToggleRow,
    selectRows
  } = useSelection(
    rows.length,
    columnDefs.filter(cd => !cd.invisible).length,
    onActiveCellChanged
  )

  // フォーカス状態の管理
  const [isFocused, setIsFocused] = useState(false);

  // コピー＆ペースト機能
  const { handleCopy, handlePaste, setStringValuesToSelectedRange } = useCopyPaste({
    tableRef,
    activeCell,
    selectedRange,
    setSelectedRange,
    isEditing,
    getIsReadOnly,
    props
  });

  // ドラッグ選択機能
  const {
    isDragging,
    handleMouseDown,
    handleMouseMove
  } = useDragSelection(setActiveCell, setSelectedRange, anchorCellRef)

  // 仮想化設定
  const rowVirtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => tableContainerRef.current,
    estimateSize: () => ESTIMATED_ROW_HEIGHT,
    measureElement: element => element?.getBoundingClientRect().height,
    overscan: 5,
  });

  // テーブル定義
  const columnHelper = createColumnHelper<TRow>();
  const columns = useMemo(() => {
    // 行ヘッダー（チェックボックス列）
    const rowHeaderColumn = columnHelper.display({
      id: ROW_HEADER_COLUMN_ID,
      enableResizing: false,
      meta: {
        isRowHeader: true,
        originalColDef: undefined,
      } satisfies ColumnMetadataInternal<TRow>,
    });

    // 列ヘッダーとデータ列
    const dataColumns = columnDefs
      .map((colDef: EditableGridColumnDef<TRow>, colIndex: number) => {
        const accessor = (row: TRow) => colDef.fieldPath
          ? getValueByPath(row, colDef.fieldPath)
          : undefined
        const tableColumnDef = columnHelper.accessor(accessor, {
          id: colDef.columnId ?? `col-${colIndex}`,
          size: colDef.defaultWidth ?? DEFAULT_COLUMN_WIDTH,
          enableResizing: colDef.enableResizing ?? true,
          meta: {
            originalColDef: colDef,
            isRowHeader: false,
          } satisfies ColumnMetadataInternal<TRow>,
        });
        return tableColumnDef;
      });

    const generatedColumns = showCheckBox ? [rowHeaderColumn, ...dataColumns] : dataColumns;
    return generatedColumns;
  }, [columnDefs, showCheckBox, allRowsChecked, handleToggleAllRows, columnHelper, cellType]);

  const table = useReactTable({
    data: rows,
    columns,
    columnResizeMode: 'onChange',
    state: {
      columnSizing,
      columnVisibility: {
        [ROW_HEADER_COLUMN_ID]: showCheckBox !== undefined && showCheckBox !== false,
        ...Object.fromEntries(
          columnDefs.map((colDef, i) => [
            colDef.columnId ?? `col-${i}`,
            !colDef.invisible
          ])
        ),
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
  tableRef.current = table

  // 横幅情報
  const tableTotalWidth = table.getTotalSize();

  // 仮想化設定
  const { rows: tableRows } = table.getRowModel();

  const virtualItems = rowVirtualizer.getVirtualItems();

  // ピクセル数取得関数
  const getPixel = useGetPixel(tableRef, rowVirtualizer, ESTIMATED_ROW_HEIGHT, columnSizing)

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
    getActiveCell: () => {
      if (!activeCell) return undefined;
      return {
        rowIndex: activeCell.rowIndex,
        colIndex: activeCell.colIndex,
        getRow: () => rows[activeCell.rowIndex],
        getColumnDef: () => columnDefs[activeCell.colIndex],
      }
    },
    getSelectedRange: () => selectedRange ?? undefined,
  }), [checkedRows, rows, columnDefs, selectRows, activeCell, selectedRange]);

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

  // キーボード操作のセットアップ
  const handleKeyDown = useGridKeyboard({
    propsKeyDown: props.onKeyDown,
    showCheckBox: showCheckBox !== undefined,
    activeCell,
    selectedRange,
    isEditing,
    rowCount: rows.length,
    colCount: table.getVisibleLeafColumns().length,
    setActiveCell,
    setSelectedRange,
    anchorCellRef,
    startEditing: (rowIndex, colIndex) => {
      const visibleDataColumns = table.getVisibleLeafColumns()
      const targetColumn = visibleDataColumns[colIndex];
      if (targetColumn) {
        const targetCell = table.getRow(rowIndex.toString()).getAllCells().find(c => c.column.id === targetColumn.id);
        if (targetCell) {
          cellEditorRef.current?.startEditing(targetCell);
        }
      }
    },
    getIsReadOnly,
    rowVirtualizer,
    tableContainerRef,
    setStringValuesToSelectedRange,
    table,
    getPixel,
  });

  // フォーカス制御のハンドラ
  const handleFocus = useCallback(() => {
    setIsFocused(true);
  }, []);

  const handleBlur = useCallback(() => {
    setIsFocused(false);
  }, []);

  return (
    <div
      ref={tableContainerRef}
      className={`overflow-auto bg-gray-200 relative outline-none ${className ?? ''}`}
      tabIndex={0} // 1行も無い場合であってもキーボード操作を受け付けるようにするため
      onKeyDown={handleKeyDown}
      onCopy={handleCopy}
      onPaste={handlePaste}
      onFocus={handleFocus}
      onBlur={handleBlur}
    >
      <table
        className={`grid border-collapse border-spacing-0`}
        style={{ minWidth: tableTotalWidth }}
      >

        {/* 列ヘッダ */}
        <thead className="grid sticky top-0 z-10 grid-header-group">
          {table.getHeaderGroups().map(headerGroup => (
            <tr key={headerGroup.id} className="flex w-full">
              {headerGroup.headers.map(header => {
                const headerMeta = header.column.columnDef.meta as ColumnMetadataInternal<TRow> | undefined
                const isFixedColumn = !!headerMeta?.originalColDef?.isFixed;
                const isRowHeader = !!headerMeta?.isRowHeader;

                let className = 'flex bg-gray-100 relative text-left select-none'
                if (isRowHeader) className += ' sticky z-20'
                else if (isFixedColumn) className += ' sticky z-10'

                return (
                  <th
                    key={header.id}
                    className={className}
                    style={{
                      width: header.getSize(),
                      left: (isRowHeader || isFixedColumn) ? `${header.getStart()}px` : undefined,
                    }}
                  >
                    {isRowHeader ? (
                      <div
                        className="flex justify-center items-center border-b border-r border-gray-200 sticky"
                        onClick={e => e.stopPropagation()}
                        style={{
                          width: header.getSize(),
                          height: ESTIMATED_ROW_HEIGHT,
                        }}
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
                    ) : (
                      <div
                        className="flex pl-1 border-b border-r border-gray-200 text-gray-700 font-normal select-none"
                        style={{ width: header.getSize() }}
                      >
                        <span className="truncate">
                          {headerMeta?.originalColDef?.header === '' ? '\u00A0' : headerMeta?.originalColDef?.header}
                        </span>
                      </div>
                    )}

                    {/* 列幅を変更できる場合はサイズ変更ハンドラを設定 */}
                    {header.column.getCanResize() && (
                      <div
                        onMouseDown={header.getResizeHandler()}
                        onTouchStart={header.getResizeHandler()}
                        className={`absolute top-0 right-0 h-full w-1.5 cursor-col-resize select-none touch-none ${header.column.getIsResizing() ? 'bg-sky-500 opacity-50' : 'hover:bg-gray-400'}`}
                      >
                      </div>
                    )}
                  </th>
                );
              })}
            </tr>
          ))}
        </thead>
        <tbody
          ref={tableBodyRef}
          className="grid relative"
          style={{
            height: `${rowVirtualizer.getTotalSize()}px`, // スクロールバーにテーブルのサイズを伝える
          }}
        >
          {/* 画面のスクロール範囲内に表示されている行のみレンダリングされる */}
          {virtualItems.map(virtualRow => {
            const row = tableRows[virtualRow.index];
            if (!row) return null;

            const className = `flex absolute w-full ${props.getRowClassName?.(row.original) ?? ''}`

            return (
              <tr
                key={row.id}
                data-index={virtualRow.index} // 動的行高さ測定に必要
                ref={node => {
                  if (node) {
                    rowVirtualizer.measureElement(node); // 動的行高さを測定
                  }
                }}
                style={{
                  transform: `translateY(${virtualRow.start}px)`, // スクロールでの変更のため常にstyleとして設定
                }}
                className={className}
              >
                {row.getVisibleCells().map(cell => (
                  <MemorizedBodyCell<TRow>
                    key={cell.id}
                    cell={cell}
                    rowIndex={row.index}
                    tableRef={tableRef}
                    onChangeRow={props.onChangeRow}
                    getShouldShowCheckBox={getShouldShowCheckBox}
                    checkedRows={checkedRows}
                    handleToggleRow={handleToggleRow}
                    handleCellClick={handleCellClick}
                    getIsReadOnly={getIsReadOnly}
                    isDragging={isDragging}
                    handleMouseDown={handleMouseDown}
                    handleMouseMove={handleMouseMove}
                    cellEditorRef={cellEditorRef}
                    showHorizontalBorder={props.showHorizontalBorder}
                    columnSizing={columnSizing?.[cell.column.id]}
                  />
                ))}
              </tr>
            );
          })}

          {/* データが空の場合のメッセージ */}
          {rows.length === 0 && (
            <tr className="flex absolute w-full">
              <td colSpan={table.getAllColumns().length} className="flex w-full">
                <EmptyDataMessage />
              </td>
            </tr>
          )}
        </tbody>
      </table>

      <ActiveCell
        anchorCellRef={anchorCellRef}
        selectedRange={selectedRange}
        getPixel={getPixel}
        isFocused={isFocused}
      />

      <CellEditor
        ref={cellEditorRef}
        editorComponent={props.editorComponent ?? DefaultEditor}
        api={table}
        caretCell={activeCell ?? undefined}
        getPixel={getPixel}
        onChangeEditing={handleChangeEditing}
        onChangeRow={props.onChangeRow}
        isFocused={isFocused}
      />

    </div>
  );
}) as (<TRow extends ReactHookForm.FieldValues, >(props: EditableGridProps<TRow> & { ref?: React.ForwardedRef<EditableGridRef<TRow>> }) => React.ReactNode);

// ------------------------------------

type MemorizedBodyCellProps<TRow extends ReactHookForm.FieldValues> = {
  cell: Cell<TRow, unknown>,
  rowIndex: number,
  tableRef: React.RefObject<Table<TRow> | null>,
  getShouldShowCheckBox: (rowIndex: number) => boolean,
  checkedRows: Set<number>,
  handleToggleRow: (rowIndex: number, checked: boolean) => void,
  handleCellClick: (e: React.MouseEvent<HTMLTableCellElement>, rowIndex: number, colIndex: number) => void,
  getIsReadOnly: (rowIndex: number) => boolean,
  isDragging: boolean,
  handleMouseDown: (e: React.MouseEvent<HTMLTableCellElement>, rowIndex: number, colIndex: number) => void,
  handleMouseMove: (rowIndex: number, colIndex: number) => void,
  cellEditorRef: React.RefObject<CellEditorRef<TRow> | null>,
  showHorizontalBorder: boolean | undefined,
  columnSizing: number | null | undefined,
  onChangeRow: unknown,
}

/** メモ化されたtdセル */
const MemorizedBodyCell = React.memo(<TRow extends ReactHookForm.FieldValues>({
  cell,
  rowIndex,
  tableRef,
  getShouldShowCheckBox,
  checkedRows,
  handleToggleRow,
  handleCellClick,
  getIsReadOnly,
  isDragging,
  handleMouseDown,
  handleMouseMove,
  cellEditorRef,
  showHorizontalBorder,
  ...props
}: MemorizedBodyCellProps<TRow>) => {

  // 行ヘッダー列を除いた可視列の配列を取得し、その中でのインデックスを colIndex とする
  const cellMeta = cell.column.columnDef.meta as ColumnMetadataInternal<TRow> | undefined;

  // 行選択チェックボックス列
  if (cellMeta?.isRowHeader) {
    return (
      <td
        key={cell.id}
        className="flex bg-gray-100 align-middle text-center sticky left-0"
        style={{ width: cell.column.getSize() }}
      >
        <div
          className="h-full flex justify-center items-center border-r border-gray-200"
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
  let dataColumnClassName = 'flex outline-none align-middle'

  if (props.onChangeRow !== undefined
    && (cellMeta?.originalColDef?.isReadOnly === undefined
      || cellMeta?.originalColDef?.isReadOnly === false
      || typeof cellMeta?.originalColDef?.isReadOnly === 'function'
      && cellMeta.originalColDef.isReadOnly(cell.row.original, cell.row.index) === false)) {
    dataColumnClassName += ` bg-white`
  }

  // z-indexをつけるとボディ列が列ヘッダより手前にきてしまうので設定しない
  if (cellMeta?.originalColDef?.isFixed) dataColumnClassName += ` sticky`

  // 画面側でレンダリング処理が決められている場合はそれを使用、決まっていないなら単にtoString
  const renderCell = cellMeta?.originalColDef?.renderCell

  return (
    <td
      key={cell.id}
      className={dataColumnClassName}
      style={{
        width: cell.column.getSize(),
        left: cellMeta?.originalColDef?.isFixed ? `${cell.column.getStart()}px` : undefined,
      }}
      onClick={(e) => {
        const visibleDataColumns = tableRef.current?.getVisibleLeafColumns() ?? []
        const colIndex = visibleDataColumns.findIndex(c => c.id === cell.column.id);
        if (cell.column.id !== ROW_HEADER_COLUMN_ID && colIndex !== -1) {
          handleCellClick(e, rowIndex, colIndex);
        }
      }}
      onDoubleClick={() => {
        if (!getIsReadOnly(rowIndex) && cellEditorRef.current) {
          cellEditorRef.current.startEditing(cell);
        }
      }}
      onMouseDown={(e) => {
        const visibleDataColumns = tableRef.current?.getVisibleLeafColumns() ?? []
        const colIndex = visibleDataColumns.findIndex(c => c.id === cell.column.id);
        if (cell.column.id !== ROW_HEADER_COLUMN_ID && colIndex !== -1) {
          handleMouseDown(e, rowIndex, colIndex);
        }
      }}
      onMouseEnter={() => {
        // ドラッグ中のときのみ範囲選択を更新
        if (isDragging) {
          const visibleDataColumns = tableRef.current?.getVisibleLeafColumns() ?? []
          const colIndex = visibleDataColumns.findIndex(c => c.id === cell.column.id);
          if (cell.column.id !== ROW_HEADER_COLUMN_ID && colIndex !== -1) {
            handleMouseMove(rowIndex, colIndex);
          }
        }
      }}
      tabIndex={0}
    >
      <div
        className={`flex select-none truncate border-gray-200 ${showHorizontalBorder ? 'border-b' : ''} ${cellMeta?.originalColDef?.isFixed ? 'border-r' : ''}`}
        style={{
          width: cell.column.getSize(),
          minHeight: ESTIMATED_ROW_HEIGHT, // 動的行高さ対応: heightの代わりにminHeightを使用
        }}
      >
        {renderCell?.(cell.getContext()) ?? cell.getValue()?.toString()}
      </div>
    </td>
  );
}, (prevProps, nextProps) => {
  // cellは毎回新しいインスタンスに生まれ変わるので、それ以外のpropsが変わったときのみ再レンダリングする
  const { cell, ...prevRest } = prevProps
  const { cell: _, ...nextRest } = nextProps
  for (const key in prevRest) {
    if (!Object.is(prevRest[key as keyof typeof prevRest], nextRest[key as keyof typeof nextRest])) return false
  }
  return true
}) as (<TRow extends ReactHookForm.FieldValues>(props: MemorizedBodyCellProps<TRow>) => React.ReactNode)

// ------------------------------------

/** このフォルダ内部でのみ使用。外部から使われる想定はない */
export type ColumnMetadataInternal<TRow extends ReactHookForm.FieldValues> = {
  isRowHeader: boolean | undefined
  originalColDef: EditableGridColumnDef<TRow> | undefined
}

/** 行ヘッダー列のID */
export const ROW_HEADER_COLUMN_ID = 'rowHeader'

/** 推定行高さ */
const ESTIMATED_ROW_HEIGHT = 24
/** 行ヘッダー列の幅 */
const ROW_HEADER_WIDTH = 32
/** デフォルトの列幅。8rem をピクセル換算。環境依存可能性あり */
export const DEFAULT_COLUMN_WIDTH = 128

import React, { useEffect, useCallback, useRef } from "react";
import { CellPosition, CellSelectionRange, EditableGridKeyboardEventHandler, EditableGridColumnDef } from ".";
import { ColumnMetadataInternal } from "./EditableGrid";
import type { Virtualizer } from '@tanstack/react-virtual';
import type * as RT from '@tanstack/react-table';
import type * as ReactHookForm from 'react-hook-form';
import * as Util from "../../util";
import useEvent from "react-use-event-hook";
import { GetPixelFunction } from "./EditableGrid.CellEditor";

export interface UseGridKeyboardProps<TRow extends ReactHookForm.FieldValues> {
  /** EditableGridの外側で定義されるキーボードイベントハンドラ */
  propsKeyDown: EditableGridKeyboardEventHandler | undefined
  showCheckBox: boolean;
  activeCell: CellPosition | null;
  selectedRange: CellSelectionRange | null;
  isEditing: boolean;
  rowCount: number;
  colCount: number;
  setActiveCell: (cell: CellPosition | null) => void;
  setSelectedRange: (range: CellSelectionRange | null) => void;
  startEditing: (rowIndex: number, colIndex: number) => void;
  getIsReadOnly: (rowIndex: number) => boolean;
  rowVirtualizer: Virtualizer<HTMLDivElement, Element>;
  tableContainerRef: React.RefObject<HTMLDivElement | null>;
  setStringValuesToSelectedRange: (values: string[][]) => void;
  /** テーブルインスタンス */
  table: RT.Table<TRow>;
  getPixel: GetPixelFunction
  /** 範囲選択のアンカーセル */
  anchorCellRef: React.RefObject<CellPosition | null>;
}

export function useGridKeyboard<TRow extends ReactHookForm.FieldValues>({
  propsKeyDown,
  showCheckBox,
  activeCell,
  selectedRange,
  isEditing,
  rowCount,
  colCount,
  setActiveCell,
  setSelectedRange,
  startEditing,
  getIsReadOnly,
  rowVirtualizer,
  tableContainerRef,
  setStringValuesToSelectedRange,
  table,
  getPixel,
  anchorCellRef,
}: UseGridKeyboardProps<TRow>) {
  // キーボードイベントハンドラ
  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useEvent(e => {
    // 画面側で何らかの制御が指定されている場合はそちらが優先
    if (propsKeyDown) {
      const { handled } = propsKeyDown(e, isEditing);
      if (handled) {
        e.preventDefault();
        e.stopPropagation();
        return;
      }
    }

    // 編集中はCellEditorに任せる
    if (isEditing) return;

    if (!activeCell) return;

    const { rowIndex, colIndex } = activeCell;
    let newRowIndex = rowIndex;
    let newColIndex = colIndex;

    // 列方向のスクロール
    const scrollHorizontal = (cIndex: number) => {
      const tableElement = tableContainerRef.current
      if (!tableElement) return

      // 固定列の右端の列番号を取得
      const maxFixedColumnIndex = Math.max(...table
        .getVisibleLeafColumns()
        .filter(c => (c.columnDef.meta as ColumnMetadataInternal<TRow> | undefined)?.originalColDef?.isFixed)
        .map(c => c.getIndex()))

      // スクロール先が固定列ならばテーブル自体の水平方向のスクロール位置を0にする
      if (cIndex <= maxFixedColumnIndex) {
        tableElement.scrollTo({ left: 0, behavior: 'instant' })
        return
      }

      // スクロール先
      const columnLeft = getPixel({ position: 'left', colIndex: cIndex })
      const columnRight = getPixel({ position: 'right', colIndex: cIndex })

      // 左端の列が固定されており、スクロール先が固定されていない列の場合、
      // スクロール先のセルが固定セルの下に潜りこまないようにする。
      // （つまりスクロール先のセルの左端が固定セルの右端よりも左になるようにする）
      const fixedColumnWidth = maxFixedColumnIndex >= 0
        ? getPixel({ position: 'right', colIndex: maxFixedColumnIndex })
        : 0
      if (maxFixedColumnIndex >= 0) {
        if (columnLeft < fixedColumnWidth) {
          tableElement.scrollTo({ left: fixedColumnWidth, behavior: 'instant' })
        } else if (columnRight > fixedColumnWidth) {
          tableElement.scrollTo({ left: columnRight - tableElement.clientWidth, behavior: 'instant' })
        }
        return
      }

      const tableWidth = tableElement.clientWidth
      const currentLeft = tableElement.scrollLeft
      const currentRight = currentLeft + tableWidth
      if (currentLeft > columnLeft) {
        tableElement.scrollTo({ left: columnLeft, behavior: 'instant' })
      } else if (currentRight < columnRight) {
        tableElement.scrollTo({ left: columnRight - tableWidth, behavior: 'instant' })
      }
    }

    // Shiftキーが押されていない場合、または anchorCell が未設定の場合にアンカーを更新
    if (!e.shiftKey || !anchorCellRef.current) {
      anchorCellRef.current = activeCell;
    }

    // アンカーセルが存在しない場合は処理しない（Shiftキーが押された最初のイベントより前）
    const anchorCell = anchorCellRef.current;
    if (e.shiftKey && !anchorCell) return;

    switch (e.key) {
      case 'ArrowUp':
        e.preventDefault();

        // Alt + ArrowUp の場合、getOptions が定義されている列で編集を開始
        if (e.altKey) {
          const visibleDataColumns = table.getVisibleLeafColumns()
          const targetColumn = visibleDataColumns[colIndex];
          if (targetColumn) {
            const meta = targetColumn.columnDef.meta as ColumnMetadataInternal<TRow> | undefined;
            const columnDef = meta?.originalColDef;
            if (columnDef?.getOptions && !getIsReadOnly(rowIndex)) {
              startEditing(rowIndex, colIndex);
              return;
            }
          }
        }

        if (rowIndex > 0) {
          newRowIndex = rowIndex - 1;
          setActiveCell({ rowIndex: newRowIndex, colIndex });
          rowVirtualizer.scrollToIndex(newRowIndex, { align: 'center' });

          if (e.shiftKey && anchorCell) { // anchorCell の存在を確認
            // 範囲選択（Shift + 矢印キー）
            setSelectedRange({
              startRow: Math.min(anchorCell.rowIndex, newRowIndex),
              startCol: Math.min(anchorCell.colIndex, colIndex),
              endRow: Math.max(anchorCell.rowIndex, newRowIndex),
              endCol: Math.max(anchorCell.colIndex, colIndex)
            });
          } else {
            // 単一選択 (アンカーもリセット)
            anchorCellRef.current = { rowIndex: newRowIndex, colIndex };
            setSelectedRange({
              startRow: newRowIndex,
              startCol: colIndex,
              endRow: newRowIndex,
              endCol: colIndex
            });
          }
        }
        break;
      case 'ArrowDown':
        e.preventDefault();

        // Alt + ArrowDown の場合、getOptions が定義されている列で編集を開始
        if (e.altKey) {
          const visibleDataColumns = table.getVisibleLeafColumns()
          const targetColumn = visibleDataColumns[colIndex];
          if (targetColumn) {
            const meta = targetColumn.columnDef.meta as ColumnMetadataInternal<TRow> | undefined;
            const columnDef = meta?.originalColDef;
            if (columnDef?.getOptions && !getIsReadOnly(rowIndex)) {
              startEditing(rowIndex, colIndex);
              return;
            }
          }
        }

        if (rowIndex < rowCount - 1) {
          newRowIndex = rowIndex + 1;
          setActiveCell({ rowIndex: newRowIndex, colIndex });
          rowVirtualizer.scrollToIndex(newRowIndex, { align: 'center' });

          if (e.shiftKey && anchorCell) { // anchorCell の存在を確認
            // 範囲選択（Shift + 矢印キー）
            setSelectedRange({
              startRow: Math.min(anchorCell.rowIndex, newRowIndex),
              startCol: Math.min(anchorCell.colIndex, colIndex),
              endRow: Math.max(anchorCell.rowIndex, newRowIndex),
              endCol: Math.max(anchorCell.colIndex, colIndex)
            });
          } else {
            // 単一選択 (アンカーもリセット)
            anchorCellRef.current = { rowIndex: newRowIndex, colIndex };
            setSelectedRange({
              startRow: newRowIndex,
              startCol: colIndex,
              endRow: newRowIndex,
              endCol: colIndex
            });
          }
        }
        break;
      case 'ArrowLeft':
        e.preventDefault();
        const min = showCheckBox ? 1 : 0
        if (colIndex > min) {
          newColIndex = colIndex - 1;
          setActiveCell({ rowIndex, colIndex: newColIndex });
          scrollHorizontal(newColIndex);

          if (e.shiftKey && anchorCell) { // anchorCell の存在を確認
            // 範囲選択（Shift + 矢印キー）
            setSelectedRange({
              startRow: Math.min(anchorCell.rowIndex, rowIndex),
              startCol: Math.min(anchorCell.colIndex, newColIndex),
              endRow: Math.max(anchorCell.rowIndex, rowIndex),
              endCol: Math.max(anchorCell.colIndex, newColIndex)
            });
          } else {
            // 単一選択 (アンカーもリセット)
            anchorCellRef.current = { rowIndex, colIndex: newColIndex };
            setSelectedRange({
              startRow: rowIndex,
              startCol: newColIndex,
              endRow: rowIndex,
              endCol: newColIndex
            });
          }
        }
        break;
      case 'ArrowRight':
        e.preventDefault();
        if (colIndex < colCount - 1) {
          newColIndex = colIndex + 1;
          setActiveCell({ rowIndex, colIndex: newColIndex });
          scrollHorizontal(newColIndex);

          if (e.shiftKey && anchorCell) { // anchorCell の存在を確認
            // 範囲選択（Shift + 矢印キー）
            setSelectedRange({
              startRow: Math.min(anchorCell.rowIndex, rowIndex),
              startCol: Math.min(anchorCell.colIndex, newColIndex),
              endRow: Math.max(anchorCell.rowIndex, rowIndex),
              endCol: Math.max(anchorCell.colIndex, newColIndex)
            });
          } else {
            // 単一選択 (アンカーもリセット)
            anchorCellRef.current = { rowIndex, colIndex: newColIndex };
            setSelectedRange({
              startRow: rowIndex,
              startCol: newColIndex,
              endRow: rowIndex,
              endCol: newColIndex
            });
          }
        }
        break;
      case 'F2':
        e.preventDefault();
        if (!getIsReadOnly(rowIndex)) {
          startEditing(rowIndex, colIndex);
        }
        break;
      case 'Tab':
        e.preventDefault();
        // 次のセルに移動
        if (e.shiftKey) {
          // 前のセル
          if (colIndex > 0) {
            setActiveCell({ rowIndex, colIndex: colIndex - 1 });
            scrollHorizontal(colIndex - 1);
            setSelectedRange({
              startRow: rowIndex,
              startCol: colIndex - 1,
              endRow: rowIndex,
              endCol: colIndex - 1
            });
          } else if (rowIndex > 0) {
            // 前の行の最後のセル
            setActiveCell({ rowIndex: rowIndex - 1, colIndex: colCount - 1 });
            rowVirtualizer.scrollToIndex(rowIndex - 1, { align: 'center' });
            setSelectedRange({
              startRow: rowIndex - 1,
              startCol: colCount - 1,
              endRow: rowIndex - 1,
              endCol: colCount - 1
            });
          }
        } else {
          // 次のセル
          if (colIndex < colCount - 1) {
            setActiveCell({ rowIndex, colIndex: colIndex + 1 });
            scrollHorizontal(colIndex + 1);
            setSelectedRange({
              startRow: rowIndex,
              startCol: colIndex + 1,
              endRow: rowIndex,
              endCol: colIndex + 1
            });
          } else if (rowIndex < rowCount - 1) {
            // 次の行の最初のセル
            setActiveCell({ rowIndex: rowIndex + 1, colIndex: 0 });
            rowVirtualizer.scrollToIndex(rowIndex + 1, { align: 'center' });
            setSelectedRange({
              startRow: rowIndex + 1,
              startCol: 0,
              endRow: rowIndex + 1,
              endCol: 0
            });
          }
        }
        break;
      case 'Delete':
        e.preventDefault();
        // 選択範囲のセルの値をクリア。空文字をペーストしたのと同じ
        setStringValuesToSelectedRange([]);
        break;
      case 'Process':
        // IMEが開いている場合は編集開始
        startEditing(rowIndex, colIndex);
        break;
      default:
        // 文字、数字だけをひっかけたいのでlengthで判定
        if (e.key.length === 1 && !e.ctrlKey && !e.metaKey) {
          startEditing(rowIndex, colIndex);
        }
        break;
    }
  });

  return handleKeyDown
}

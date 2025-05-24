import { useEffect, useCallback, useRef } from "react";
import { CellPosition, CellSelectionRange } from ".";
import type { Virtualizer } from '@tanstack/react-virtual';
import * as Util from "../../util";
import useEvent from "react-use-event-hook";

export interface UseGridKeyboardProps {
  activeCell: CellPosition | null;
  selectedRange: CellSelectionRange | null;
  isEditing: boolean;
  rowCount: number;
  colCount: number;
  setActiveCell: (cell: CellPosition | null) => void;
  setSelectedRange: (range: CellSelectionRange | null) => void;
  startEditing: (rowIndex: number, colIndex: number) => void;
  startEditingWithCharacter: (rowIndex: number, colIndex: number, char: string) => void;
  getIsReadOnly: (rowIndex: number) => boolean;
  rowVirtualizer: Virtualizer<HTMLDivElement, Element>;
  tableContainerRef: React.RefObject<HTMLDivElement | null>;
  setStringValuesToSelectedRange: (values: string[][]) => void;
}

export function useGridKeyboard({
  activeCell,
  selectedRange,
  isEditing,
  rowCount,
  colCount,
  setActiveCell,
  setSelectedRange,
  startEditing,
  startEditingWithCharacter,
  getIsReadOnly,
  rowVirtualizer,
  tableContainerRef,
  setStringValuesToSelectedRange,
}: UseGridKeyboardProps) {
  const anchorCellRef = useRef<CellPosition | null>(null);
  const { isComposing } = Util.useIME();

  // クリップボードへのコピー処理
  const handleCopy = useEvent((e: ClipboardEvent) => {
    if (!selectedRange || !e.clipboardData) return;

    const startRow = Math.min(selectedRange.startRow, selectedRange.endRow);
    const endRow = Math.max(selectedRange.startRow, selectedRange.endRow);
    const startCol = Math.min(selectedRange.startCol, selectedRange.endCol);
    const endCol = Math.max(selectedRange.startCol, selectedRange.endCol);

    // このイベントハンドラではクリップボードデータを直接操作できないため、
    // 呼び出し元から行データとカラム定義を渡す必要があります。
    // このフックでは基本的なキーボードナビゲーションのみ扱います。
  });

  // キー入力イベントハンドラ
  const handleKeyInput = useEvent((e: KeyboardEvent) => {
    if (isEditing || !activeCell || isComposing) return;

    // ナビゲーションに使うキーは無視（別のハンドラで処理）
    const navigationKeys = [
      'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
      'Tab', 'Enter', 'Escape', 'Home', 'End', 'PageUp', 'PageDown',
      'F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'F7', 'F8', 'F9', 'F10', 'F11', 'F12',
      'Control', 'Alt', 'Shift', 'Meta', 'Delete',
    ];

    // 修飾キーが押されている場合は編集開始しない
    if (e.ctrlKey || e.altKey || e.metaKey) return;

    if (e.key && e.key.length === 1 && !navigationKeys.includes(e.key)) {
      const { rowIndex, colIndex } = activeCell;

      if (!getIsReadOnly(rowIndex)) {
        e.preventDefault();
        startEditingWithCharacter(rowIndex, colIndex, e.key);
      }
    }
  });

  // キーボードイベントハンドラ
  const handleKeyDown = useEvent((e: KeyboardEvent) => {
    if (!activeCell || isEditing) return;

    const { rowIndex, colIndex } = activeCell;
    let newRowIndex = rowIndex;
    let newColIndex = colIndex;

    // スクロール処理を関数化
    const scrollToCell = (rIndex: number, cIndex: number, isVertical: boolean) => {
      if (isVertical) {
        rowVirtualizer.scrollToIndex(rIndex, { align: 'auto' });
      } else {
        // 列方向のスクロール
        const tableElement = tableContainerRef.current;
        if (tableElement) {
          // ヘッダーから対象列の要素を探す (より堅牢な方法は列IDを使うことだが、ここでは簡略化)
          // 注意: この方法は表示されている列ヘッダーのみを対象とし、colIndexが可視列のインデックスであることを前提とします。
          //       実際には、React TableのAPIや列定義と照らし合わせてDOM要素を特定する必要があるかもしれません。
          const thSelector = `thead th:nth-child(${cIndex + 2})`; // +1 for 1-based index, +1 for rowHeader column
          const thElement = tableElement.querySelector(thSelector) as HTMLElement | null;
          if (thElement) {
            thElement.scrollIntoView({ inline: 'nearest', block: 'nearest' });
          }
        }
      }
    };

    // Shiftキーが押されていない場合、または anchorCell が未設定の場合にアンカーを更新
    if (!e.shiftKey || !anchorCellRef.current) {
      anchorCellRef.current = activeCell;
    }

    // 編集モード中は矢印キーを処理しない
    if (isEditing) return;

    // アンカーセルが存在しない場合は処理しない（Shiftキーが押された最初のイベントより前）
    const anchorCell = anchorCellRef.current;
    if (e.shiftKey && !anchorCell) return;

    switch (e.key) {
      case 'ArrowUp':
        e.preventDefault();
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
        if (colIndex > 0) {
          newColIndex = colIndex - 1;
          setActiveCell({ rowIndex, colIndex: newColIndex });
          scrollToCell(rowIndex, newColIndex, false);

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
          scrollToCell(rowIndex, newColIndex, false);

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
            scrollToCell(rowIndex, colIndex - 1, false);
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
            scrollToCell(rowIndex, colIndex + 1, false);
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
    }
  });

  // イベントリスナーの設定
  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('keypress', handleKeyInput);
    document.addEventListener('copy', handleCopy);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('keypress', handleKeyInput);
      document.removeEventListener('copy', handleCopy);
    };
  }, [handleKeyDown, handleKeyInput, handleCopy]);
}

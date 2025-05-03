import { useEffect, useCallback } from "react";
import { CellPosition, CellSelectionRange } from "../index.d";

export interface UseGridKeyboardProps {
  activeCell: CellPosition | null;
  selectedRange: CellSelectionRange | null;
  isEditing: boolean;
  rowCount: number;
  colCount: number;
  setActiveCell: (cell: CellPosition | null) => void;
  setSelectedRange: (range: CellSelectionRange | null) => void;
  startEditing: (rowIndex: number, colIndex: number) => void;
  getIsReadOnly: (rowIndex: number) => boolean;
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
  getIsReadOnly
}: UseGridKeyboardProps) {
  // クリップボードへのコピー処理
  const handleCopy = useCallback((e: ClipboardEvent) => {
    if (!selectedRange || !e.clipboardData) return;

    const startRow = Math.min(selectedRange.startRow, selectedRange.endRow);
    const endRow = Math.max(selectedRange.startRow, selectedRange.endRow);
    const startCol = Math.min(selectedRange.startCol, selectedRange.endCol);
    const endCol = Math.max(selectedRange.startCol, selectedRange.endCol);

    // このイベントハンドラではクリップボードデータを直接操作できないため、
    // 呼び出し元から行データとカラム定義を渡す必要があります。
    // このフックでは基本的なキーボードナビゲーションのみ扱います。
  }, [selectedRange]);

  // キーボードイベントハンドラ
  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (!activeCell) return;

    const { rowIndex, colIndex } = activeCell;

    // 編集モード中は矢印キーを処理しない
    if (isEditing) return;

    // Shiftキーを押した状態でのキー操作時、範囲選択の開始位置を保持
    const rangeStart = selectedRange ?
      { rowIndex: selectedRange.startRow, colIndex: selectedRange.startCol } :
      { rowIndex, colIndex };

    switch (e.key) {
      case 'ArrowUp':
        e.preventDefault();
        if (rowIndex > 0) {
          const newRowIndex = rowIndex - 1;
          setActiveCell({ rowIndex: newRowIndex, colIndex });

          if (e.shiftKey) {
            // 範囲選択（Shift + 矢印キー）
            setSelectedRange({
              startRow: rangeStart.rowIndex,
              startCol: rangeStart.colIndex,
              endRow: newRowIndex,
              endCol: colIndex
            });
          } else {
            // 単一選択
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
          const newRowIndex = rowIndex + 1;
          setActiveCell({ rowIndex: newRowIndex, colIndex });

          if (e.shiftKey) {
            // 範囲選択（Shift + 矢印キー）
            setSelectedRange({
              startRow: rangeStart.rowIndex,
              startCol: rangeStart.colIndex,
              endRow: newRowIndex,
              endCol: colIndex
            });
          } else {
            // 単一選択
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
          const newColIndex = colIndex - 1;
          setActiveCell({ rowIndex, colIndex: newColIndex });

          if (e.shiftKey) {
            // 範囲選択（Shift + 矢印キー）
            setSelectedRange({
              startRow: rangeStart.rowIndex,
              startCol: rangeStart.colIndex,
              endRow: rowIndex,
              endCol: newColIndex
            });
          } else {
            // 単一選択
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
          const newColIndex = colIndex + 1;
          setActiveCell({ rowIndex, colIndex: newColIndex });

          if (e.shiftKey) {
            // 範囲選択（Shift + 矢印キー）
            setSelectedRange({
              startRow: rangeStart.rowIndex,
              startCol: rangeStart.colIndex,
              endRow: rowIndex,
              endCol: newColIndex
            });
          } else {
            // 単一選択
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
            setSelectedRange({
              startRow: rowIndex,
              startCol: colIndex - 1,
              endRow: rowIndex,
              endCol: colIndex - 1
            });
          } else if (rowIndex > 0) {
            // 前の行の最後のセル
            setActiveCell({ rowIndex: rowIndex - 1, colIndex: colCount - 1 });
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
            setSelectedRange({
              startRow: rowIndex,
              startCol: colIndex + 1,
              endRow: rowIndex,
              endCol: colIndex + 1
            });
          } else if (rowIndex < rowCount - 1) {
            // 次の行の最初のセル
            setActiveCell({ rowIndex: rowIndex + 1, colIndex: 0 });
            setSelectedRange({
              startRow: rowIndex + 1,
              startCol: 0,
              endRow: rowIndex + 1,
              endCol: 0
            });
          }
        }
        break;
    }
  }, [activeCell, selectedRange, rowCount, colCount, isEditing, getIsReadOnly, setActiveCell, setSelectedRange, startEditing]);

  // イベントリスナーの設定
  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('copy', handleCopy);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('copy', handleCopy);
    };
  }, [handleKeyDown, handleCopy]);
}

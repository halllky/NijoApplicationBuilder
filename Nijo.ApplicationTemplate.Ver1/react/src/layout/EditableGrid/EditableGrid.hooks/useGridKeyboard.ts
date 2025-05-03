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

    switch (e.key) {
      case 'ArrowUp':
        e.preventDefault();
        if (rowIndex > 0) {
          setActiveCell({ rowIndex: rowIndex - 1, colIndex });
          if (!e.shiftKey) {
            setSelectedRange({
              startRow: rowIndex - 1,
              startCol: colIndex,
              endRow: rowIndex - 1,
              endCol: colIndex
            });
          } else if (selectedRange) {
            setSelectedRange({
              ...selectedRange,
              endRow: rowIndex - 1
            });
          }
        }
        break;
      case 'ArrowDown':
        e.preventDefault();
        if (rowIndex < rowCount - 1) {
          setActiveCell({ rowIndex: rowIndex + 1, colIndex });
          if (!e.shiftKey) {
            setSelectedRange({
              startRow: rowIndex + 1,
              startCol: colIndex,
              endRow: rowIndex + 1,
              endCol: colIndex
            });
          } else if (selectedRange) {
            setSelectedRange({
              ...selectedRange,
              endRow: rowIndex + 1
            });
          }
        }
        break;
      case 'ArrowLeft':
        e.preventDefault();
        if (colIndex > 0) {
          setActiveCell({ rowIndex, colIndex: colIndex - 1 });
          if (!e.shiftKey) {
            setSelectedRange({
              startRow: rowIndex,
              startCol: colIndex - 1,
              endRow: rowIndex,
              endCol: colIndex - 1
            });
          } else if (selectedRange) {
            setSelectedRange({
              ...selectedRange,
              endCol: colIndex - 1
            });
          }
        }
        break;
      case 'ArrowRight':
        e.preventDefault();
        if (colIndex < colCount - 1) {
          setActiveCell({ rowIndex, colIndex: colIndex + 1 });
          if (!e.shiftKey) {
            setSelectedRange({
              startRow: rowIndex,
              startCol: colIndex + 1,
              endRow: rowIndex,
              endCol: colIndex + 1
            });
          } else if (selectedRange) {
            setSelectedRange({
              ...selectedRange,
              endCol: colIndex + 1
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

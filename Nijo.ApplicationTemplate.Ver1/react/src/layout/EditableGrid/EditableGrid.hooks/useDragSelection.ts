import { useState, useCallback, useEffect } from "react";
import { CellPosition, CellSelectionRange } from "../index.d";

export interface UseDragSelectionReturn {
  isDragging: boolean;
  handleMouseDown: (rowIndex: number, colIndex: number) => void;
  handleMouseMove: (rowIndex: number, colIndex: number) => void;
}

export function useDragSelection(
  setActiveCell: (cell: CellPosition | null) => void,
  setSelectedRange: (range: CellSelectionRange | null) => void
): UseDragSelectionReturn {
  const [isDragging, setIsDragging] = useState(false);
  const [dragStartCell, setDragStartCell] = useState<CellPosition | null>(null);

  // マウスダウンハンドラ
  const handleMouseDown = useCallback((rowIndex: number, colIndex: number) => {
    console.log('[useDragSelection] mouseDown', rowIndex, colIndex);
    setActiveCell({ rowIndex, colIndex });
    setSelectedRange({
      startRow: rowIndex,
      startCol: colIndex,
      endRow: rowIndex,
      endCol: colIndex
    });
    setDragStartCell({ rowIndex, colIndex });
    setIsDragging(true);
  }, [setActiveCell, setSelectedRange]);

  // マウス移動ハンドラ
  const handleMouseMove = useCallback((rowIndex: number, colIndex: number) => {
    if (isDragging && dragStartCell) {
      console.log('[useDragSelection] mouseMove', rowIndex, colIndex);

      // アクティブセルを更新
      setActiveCell({ rowIndex, colIndex });

      // 選択範囲を更新
      setSelectedRange({
        startRow: dragStartCell.rowIndex,
        startCol: dragStartCell.colIndex,
        endRow: rowIndex,
        endCol: colIndex
      });
    }
  }, [isDragging, dragStartCell, setActiveCell, setSelectedRange]);

  // マウスアップハンドラ
  const handleMouseUp = useCallback(() => {
    console.log('[useDragSelection] mouseUp');
    setIsDragging(false);
  }, []);

  // イベントリスナーの設定
  useEffect(() => {
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [handleMouseUp]);

  return {
    isDragging,
    handleMouseDown,
    handleMouseMove
  };
}

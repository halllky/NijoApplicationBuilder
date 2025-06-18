import { useState, useCallback, useEffect } from "react";
import { CellPosition, CellSelectionRange } from ".";

export interface UseDragSelectionReturn {
  isDragging: boolean;
  handleMouseDown: (event: React.MouseEvent, rowIndex: number, colIndex: number) => void;
  handleMouseMove: (rowIndex: number, colIndex: number) => void;
}

export function useDragSelection(
  setActiveCell: (cell: CellPosition | null) => void,
  setSelectedRange: (range: CellSelectionRange | null) => void,
  anchorCellRef: React.RefObject<CellPosition | null>
): UseDragSelectionReturn {
  const [isDragging, setIsDragging] = useState(false);

  // マウスダウンハンドラ
  const handleMouseDown = useCallback((event: React.MouseEvent, rowIndex: number, colIndex: number) => {
    const newCell: CellPosition = { rowIndex, colIndex };
    setActiveCell(newCell);
    setSelectedRange({
      startRow: Math.min(anchorCellRef.current?.rowIndex ?? rowIndex, rowIndex),
      startCol: Math.min(anchorCellRef.current?.colIndex ?? colIndex, colIndex),
      endRow: Math.max(anchorCellRef.current?.rowIndex ?? rowIndex, rowIndex),
      endCol: Math.max(anchorCellRef.current?.colIndex ?? colIndex, colIndex)
    });
    // アンカーセルを設定
    if (!event.shiftKey) {
      anchorCellRef.current = newCell;
    }
    setIsDragging(true);
  }, [setActiveCell, setSelectedRange, anchorCellRef]);

  // マウス移動ハンドラ
  const handleMouseMove = useCallback((rowIndex: number, colIndex: number) => {
    if (isDragging && anchorCellRef.current) {
      // アクティブセルを更新
      setActiveCell({ rowIndex, colIndex });

      // 選択範囲を更新
      setSelectedRange({
        startRow: Math.min(anchorCellRef.current.rowIndex, rowIndex),
        startCol: Math.min(anchorCellRef.current.colIndex, colIndex),
        endRow: Math.max(anchorCellRef.current.rowIndex, rowIndex),
        endCol: Math.max(anchorCellRef.current.colIndex, colIndex)
      });
    }
  }, [isDragging, anchorCellRef, setActiveCell, setSelectedRange]);

  // マウスアップハンドラ
  const handleMouseUp = useCallback(() => {
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

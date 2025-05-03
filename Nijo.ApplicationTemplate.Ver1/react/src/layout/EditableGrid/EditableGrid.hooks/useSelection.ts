import { useState, useCallback } from "react";
import { CellPosition, CellSelectionRange } from "../index.d";

export interface UseSelectionReturn {
  activeCell: CellPosition | null;
  selectedRange: CellSelectionRange | null;
  selectedRows: Set<number>;
  allRowsSelected: boolean;
  setActiveCell: (cell: CellPosition | null) => void;
  setSelectedRange: (range: CellSelectionRange | null) => void;
  handleCellClick: (rowIndex: number, colIndex: number) => void;
  handleToggleAllRows: (checked: boolean) => void;
  handleToggleRow: (rowIndex: number, checked: boolean) => void;
  selectRows: (startRowIndex: number, endRowIndex: number) => void;
}

export function useSelection(totalRows: number): UseSelectionReturn {
  // 選択状態の管理
  const [activeCell, setActiveCell] = useState<CellPosition | null>(null);
  const [selectedRange, setSelectedRange] = useState<CellSelectionRange | null>(null);
  const [selectedRows, setSelectedRows] = useState<Set<number>>(new Set());
  const [allRowsSelected, setAllRowsSelected] = useState(false);

  // 全行選択トグルのハンドラ
  const handleToggleAllRows = useCallback((checked: boolean) => {
    if (checked) {
      const allIndices = new Set(Array.from({ length: totalRows }, (_, i) => i));
      setSelectedRows(allIndices);
      setAllRowsSelected(true);
    } else {
      setSelectedRows(new Set());
      setAllRowsSelected(false);
    }
  }, [totalRows]);

  // 行選択トグルのハンドラ
  const handleToggleRow = useCallback((rowIndex: number, checked: boolean) => {
    setSelectedRows(prev => {
      const newSelectedRows = new Set(prev);
      if (checked) {
        newSelectedRows.add(rowIndex);
      } else {
        newSelectedRows.delete(rowIndex);
      }

      setAllRowsSelected(newSelectedRows.size === totalRows);
      return newSelectedRows;
    });
  }, [totalRows]);

  // セルクリックハンドラ
  const handleCellClick = useCallback((rowIndex: number, colIndex: number) => {
    setActiveCell({ rowIndex, colIndex });
    setSelectedRange({
      startRow: rowIndex,
      startCol: colIndex,
      endRow: rowIndex,
      endCol: colIndex
    });
  }, []);

  // 行範囲選択
  const selectRows = useCallback((startRowIndex: number, endRowIndex: number) => {
    const newSelectedRows = new Set<number>();
    for (let i = Math.min(startRowIndex, endRowIndex); i <= Math.max(startRowIndex, endRowIndex); i++) {
      if (i >= 0 && i < totalRows) {
        newSelectedRows.add(i);
      }
    }
    setSelectedRows(newSelectedRows);
    setAllRowsSelected(newSelectedRows.size === totalRows);
  }, [totalRows]);

  return {
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
  };
}

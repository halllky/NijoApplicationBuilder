import { useState, useCallback, useRef, useEffect } from "react";
import { CellPosition, CellSelectionRange } from ".";

export interface UseSelectionReturn {
  activeCell: CellPosition | null;
  selectedRange: CellSelectionRange | null;
  /** 行頭のチェックボックスで選択されている行のインデックスの集合 */
  checkedRows: Set<number>;
  /** 全ての行がチェックボックスで選択されているかどうか */
  allRowsChecked: boolean;
  /** 範囲選択のアンカーセル */
  anchorCellRef: React.RefObject<CellPosition | null>;
  setActiveCell: (cell: CellPosition | null) => void;
  setSelectedRange: (range: CellSelectionRange | null) => void;
  handleCellClick: (event: React.MouseEvent, rowIndex: number, colIndex: number) => void;
  handleToggleAllRows: (checked: boolean) => void;
  handleToggleRow: (rowIndex: number, checked: boolean) => void;
  selectRows: (startRowIndex: number, endRowIndex: number) => void;
}

export function useSelection(
  totalRows: number,
  totalColumns: number,
  onActiveCellChanged: (cell: CellPosition | null) => void,
): UseSelectionReturn {
  // 選択状態の管理
  const [activeCell, setActiveCell_useState] = useState<CellPosition | null>(null);
  const [selectedRange, setSelectedRange] = useState<CellSelectionRange | null>(null);
  const [selectedRows, setSelectedRows] = useState<Set<number>>(new Set());
  const [allRowsSelected, setAllRowsSelected] = useState(false);

  const activeCellRef = useRef<CellPosition | null>(null);
  const anchorCellRef = useRef<CellPosition | null>(null);

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
  const handleCellClick = useCallback((event: React.MouseEvent, rowIndex: number, colIndex: number) => {
    const currentCell = { rowIndex, colIndex };

    if (event.shiftKey && anchorCellRef.current) {
      setActiveCell(currentCell);
      setSelectedRange({
        startRow: Math.min(anchorCellRef.current.rowIndex, currentCell.rowIndex),
        startCol: Math.min(anchorCellRef.current.colIndex, currentCell.colIndex),
        endRow: Math.max(anchorCellRef.current.rowIndex, currentCell.rowIndex),
        endCol: Math.max(anchorCellRef.current.colIndex, currentCell.colIndex)
      });
    } else {
      setActiveCell(currentCell);
      setSelectedRange({
        startRow: currentCell.rowIndex,
        startCol: currentCell.colIndex,
        endRow: currentCell.rowIndex,
        endCol: currentCell.colIndex
      });
    }
    if (!event.shiftKey) {
      anchorCellRef.current = currentCell;
    }
  }, [anchorCellRef]);

  // 行範囲選択
  const selectRows = useCallback((startRowIndex: number, endRowIndex: number) => {
    const newSelectedRows = new Set<number>();
    const min = Math.max(0, Math.min(startRowIndex, endRowIndex));
    const max = Math.min(totalRows - 1, Math.max(startRowIndex, endRowIndex));
    for (let i = min; i <= max; i++) {
      newSelectedRows.add(i);
    }
    setActiveCell({ rowIndex: min, colIndex: 0 });
    setSelectedRows(newSelectedRows);
    setSelectedRange({ startCol: 0, endCol: totalColumns - 1, startRow: min, endRow: max });
    setAllRowsSelected(newSelectedRows.size === totalRows);
  }, [totalRows, totalColumns]);

  // 初期選択状態の設定（オプション）
  useEffect(() => {
    if (activeCellRef.current) {
      setActiveCell_useState(activeCellRef.current);
      anchorCellRef.current = activeCellRef.current;
    }
  }, []);

  const setActiveCell = useCallback((cell: CellPosition | null) => {
    activeCellRef.current = cell;
    setActiveCell_useState(cell);
    onActiveCellChanged(cell);
  }, [setActiveCell_useState, onActiveCellChanged]);

  return {
    activeCell,
    selectedRange,
    checkedRows: selectedRows,
    allRowsChecked: allRowsSelected,
    anchorCellRef,
    setActiveCell,
    setSelectedRange,
    handleCellClick,
    handleToggleAllRows,
    handleToggleRow,
    selectRows
  };
}

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
    anchorCellRef.current = { rowIndex: min, colIndex: 0 };
  }, [totalRows, totalColumns, anchorCellRef]);

  // 初期選択状態の設定（オプション）
  useEffect(() => {
    if (activeCellRef.current) {
      setActiveCell_useState(activeCellRef.current);
      anchorCellRef.current = activeCellRef.current;
    }
  }, []);

  // データ範囲の変更に応じて選択状態を調整
  useEffect(() => {
    const maxRowIndex = Math.max(0, totalRows - 1);
    const maxColIndex = Math.max(0, totalColumns - 1);

    // データが空になった場合は選択状態をクリア
    if (totalRows === 0 || totalColumns === 0) {
      setActiveCell_useState(null);
      setSelectedRange(null);
      setSelectedRows(new Set());
      setAllRowsSelected(false);
      activeCellRef.current = null;
      anchorCellRef.current = null;
      return;
    }

    // アクティブセルが範囲外の場合は調整
    if (activeCellRef.current) {
      const currentActive = activeCellRef.current;
      if (currentActive.rowIndex > maxRowIndex || currentActive.colIndex > maxColIndex) {
        const adjustedActiveCell = {
          rowIndex: Math.min(currentActive.rowIndex, maxRowIndex),
          colIndex: Math.min(currentActive.colIndex, maxColIndex)
        };
        activeCellRef.current = adjustedActiveCell;
        setActiveCell_useState(adjustedActiveCell);
        onActiveCellChanged(adjustedActiveCell);
      }
    }

    // アンカーセルが範囲外の場合は調整
    if (anchorCellRef.current) {
      const currentAnchor = anchorCellRef.current;
      if (currentAnchor.rowIndex > maxRowIndex || currentAnchor.colIndex > maxColIndex) {
        anchorCellRef.current = {
          rowIndex: Math.min(currentAnchor.rowIndex, maxRowIndex),
          colIndex: Math.min(currentAnchor.colIndex, maxColIndex)
        };
      }
    }

    // 選択範囲が範囲外の場合は調整
    setSelectedRange(prevRange => {
      if (!prevRange) return null;

      const adjustedRange = {
        startRow: Math.min(prevRange.startRow, maxRowIndex),
        startCol: Math.min(prevRange.startCol, maxColIndex),
        endRow: Math.min(prevRange.endRow, maxRowIndex),
        endCol: Math.min(prevRange.endCol, maxColIndex)
      };

      if (adjustedRange.startRow !== prevRange.startRow ||
        adjustedRange.startCol !== prevRange.startCol ||
        adjustedRange.endRow !== prevRange.endRow ||
        adjustedRange.endCol !== prevRange.endCol) {
        return adjustedRange;
      }
      return prevRange;
    });

    // チェック済み行が範囲外の場合は調整
    setSelectedRows(prevSelected => {
      const filtered = new Set(Array.from(prevSelected).filter(rowIndex => rowIndex < totalRows));
      if (filtered.size !== prevSelected.size) {
        setAllRowsSelected(filtered.size === totalRows);
        return filtered;
      }
      return prevSelected;
    });
  }, [totalRows, totalColumns, onActiveCellChanged]);

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

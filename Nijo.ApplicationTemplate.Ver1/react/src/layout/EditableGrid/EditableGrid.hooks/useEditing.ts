import { useState, useCallback } from "react";
import { getValueByPath, setValueByPath } from "../EditableGrid.utils";
import type * as ReactHookForm from 'react-hook-form';
import { EditableGridColumnDef } from "../index.d";

export interface UseEditingReturn<TRow extends ReactHookForm.FieldValues> {
  isEditing: boolean;
  editValue: string;
  startEditing: (rowIndex: number, colIndex: number) => void;
  confirmEdit: () => void;
  cancelEdit: () => void;
  handleEditValueChange: (value: string) => void;
}

export function useEditing<TRow extends ReactHookForm.FieldValues>(
  rows: TRow[],
  columnDefs: EditableGridColumnDef<TRow>[],
  onChangeRow?: (row: TRow, rowIndex: number) => void,
  isGridReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean),
): UseEditingReturn<TRow> {
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState<string>("");
  const [editingCell, setEditingCell] = useState<{ rowIndex: number; colIndex: number } | null>(null);

  // 行の編集可否判定
  const getIsReadOnly = useCallback((rowIndex: number): boolean => {
    if (isGridReadOnly === true) return true;
    if (typeof isGridReadOnly === 'function' && rowIndex >= 0 && rowIndex < rows.length) {
      return isGridReadOnly(rows[rowIndex], rowIndex);
    }
    return false;
  }, [isGridReadOnly, rows]);

  // 編集開始
  const startEditing = useCallback((rowIndex: number, colIndex: number) => {
    if (getIsReadOnly(rowIndex)) return;

    const fieldPath = columnDefs[colIndex]?.fieldPath;
    if (!fieldPath) return;

    const row = rows[rowIndex];
    if (!row) return;

    let value = getValueByPath(row, fieldPath);
    setEditValue(value?.toString() || '');
    setIsEditing(true);
    setEditingCell({ rowIndex, colIndex });
  }, [getIsReadOnly, rows, columnDefs]);

  // 編集確定
  const confirmEdit = useCallback(() => {
    if (!editingCell) {
      setIsEditing(false);
      return;
    }

    const { rowIndex, colIndex } = editingCell;

    if (!onChangeRow || getIsReadOnly(rowIndex)) {
      setIsEditing(false);
      setEditingCell(null);
      return;
    }

    const fieldPath = columnDefs[colIndex]?.fieldPath;
    if (!fieldPath) {
      setIsEditing(false);
      setEditingCell(null);
      return;
    }

    const targetRow = rows[rowIndex];
    const colDef = columnDefs[colIndex];

    console.log('[confirmEdit] rowIndex:', rowIndex, 'colIndex:', colIndex);
    console.log('[confirmEdit] colDef:', colDef);
    console.log('[confirmEdit] editValue:', editValue);

    if (!targetRow || !colDef || !colDef.fieldPath) {
      setIsEditing(false);
      setEditingCell(null);
      return;
    }

    const originalValue = getValueByPath(targetRow, colDef.fieldPath);
    let newValue: any = editValue;

    try {
      if (typeof originalValue === 'number') {
        newValue = Number(editValue);
      } else if (typeof originalValue === 'boolean') {
        newValue = editValue.toLowerCase() === 'true';
      }
    } catch (e) {
      console.error("型変換エラー", e);
      newValue = originalValue;
    }

    console.log('[confirmEdit] originalValue:', originalValue, 'typeof:', typeof originalValue);
    console.log('[confirmEdit] newValue:', newValue, 'typeof:', typeof newValue);

    setValueByPath(targetRow, colDef.fieldPath, newValue);

    onChangeRow(targetRow, rowIndex);
    setIsEditing(false);
    setEditingCell(null);
  }, [onChangeRow, getIsReadOnly, columnDefs, rows, editValue, editingCell]);

  // 編集値の変更ハンドラ
  const handleEditValueChange = useCallback((value: string) => {
    setEditValue(value);
  }, []);

  // 編集キャンセルハンドラ
  const cancelEdit = useCallback(() => {
    setIsEditing(false);
    setEditingCell(null);
  }, []);

  return {
    isEditing,
    editValue,
    startEditing,
    confirmEdit,
    cancelEdit,
    handleEditValueChange
  };
}

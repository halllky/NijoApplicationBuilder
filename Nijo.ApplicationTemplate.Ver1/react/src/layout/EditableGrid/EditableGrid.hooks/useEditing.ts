import { useState, useCallback } from "react";
import { getValueByPath } from "../EditableGrid.utils";
import type * as ReactHookForm from 'react-hook-form';
import { ColumnDef } from "../../cellType";

export interface UseEditingReturn {
  isEditing: boolean;
  editValue: string;
  startEditing: (rowIndex: number, colIndex: number) => void;
  confirmEdit: (rowIndex: number, colIndex: number) => void;
  cancelEdit: () => void;
  handleEditValueChange: (value: string) => void;
}

export function useEditing<TRow extends ReactHookForm.FieldValues>(
  rows: TRow[],
  columnDefs: ColumnDef<TRow>[],
  onChangeRow?: (newRow: TRow, rowIndex: number) => void,
  isReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean),
): UseEditingReturn {
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState<string>("");

  // 行の編集可否判定
  const getIsReadOnly = useCallback((rowIndex: number): boolean => {
    if (isReadOnly === true) return true;
    if (typeof isReadOnly === 'function' && rowIndex >= 0 && rowIndex < rows.length) {
      return isReadOnly(rows[rowIndex], rowIndex);
    }
    return false;
  }, [isReadOnly, rows]);

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
  }, [getIsReadOnly, rows, columnDefs]);

  // 編集確定
  const confirmEdit = useCallback((rowIndex: number, colIndex: number) => {
    if (!onChangeRow || getIsReadOnly(rowIndex)) {
      setIsEditing(false);
      return;
    }

    const fieldPath = columnDefs[colIndex]?.fieldPath;
    if (!fieldPath) {
      setIsEditing(false);
      return;
    }

    try {
      // 変更を適用した新しい行オブジェクトを作成
      const newRow = { ...rows[rowIndex] };
      const paths = fieldPath.split('.');
      let current: any = newRow;

      // ネストしたオブジェクトの場合、最後のプロパティ以外のパスを辿る
      for (let i = 0; i < paths.length - 1; i++) {
        if (!current[paths[i]]) {
          current[paths[i]] = {};
        }
        current = current[paths[i]];
      }

      // 値の型に応じて変換
      const lastPath = paths[paths.length - 1];
      const originalValue = getValueByPath(rows[rowIndex], fieldPath);

      if (typeof originalValue === 'number') {
        current[lastPath] = Number(editValue);
      } else if (typeof originalValue === 'boolean') {
        current[lastPath] = editValue.toLowerCase() === 'true';
      } else {
        current[lastPath] = editValue;
      }

      // 変更を親コンポーネントに通知
      onChangeRow(newRow as TRow, rowIndex);
    } catch (e) {
      console.error('Failed to update cell value', e);
    }

    setIsEditing(false);
  }, [onChangeRow, getIsReadOnly, columnDefs, rows, editValue]);

  // 編集値の変更ハンドラ
  const handleEditValueChange = useCallback((value: string) => {
    setEditValue(value);
  }, []);

  // 編集キャンセルハンドラ
  const cancelEdit = useCallback(() => {
    setIsEditing(false);
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

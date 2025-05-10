import { useState, useCallback } from "react";
import { getValueByPath, setValueByPath } from "./EditableGrid.utils";
import type * as ReactHookForm from 'react-hook-form';
import { EditableGridColumnDef, EditableGridProps } from ".";
import useEvent from "react-use-event-hook";

export interface UseEditingReturn<TRow extends ReactHookForm.FieldValues> {
  isEditing: boolean;
  editValue: string;
  startEditing: (rowIndex: number, colIndex: number) => void;
  startEditingWithCharacter: (rowIndex: number, colIndex: number, char: string) => void;
  confirmEdit: () => void;
  cancelEdit: () => void;
  handleEditValueChange: (value: string) => void;
}

export function useEditing<TRow extends ReactHookForm.FieldValues>(
  props: EditableGridProps<TRow>,
  columnDefs: EditableGridColumnDef<TRow>[],
  isGridReadOnly?: boolean | ((row: TRow, rowIndex: number) => boolean),
): UseEditingReturn<TRow> {

  const {
    rows,
    onCellEdited,
    cloneRow,
  } = props;

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

  // 現在のセルの値をベースに編集を開始する
  const startEditingByCurrentValue = useCallback((rowIndex: number, colIndex: number) => {
    if (getIsReadOnly(rowIndex)) return;

    const colDef = columnDefs[colIndex];

    // まず `onStartEditing` が指定されているかどうかを確認
    let editorValue: string | undefined = undefined;
    if (colDef?.onStartEditing) {
      colDef.onStartEditing({
        rowIndex,
        row: rows[rowIndex],
        setEditorValue: value => editorValue = value,
      });
    }
    // `onStartEditing` による編集開始処理が指定されていない場合は `fieldPath` の参照を試みる
    if (editorValue === undefined) {
      const fieldPath = colDef?.fieldPath;
      if (fieldPath) {
        const row = rows[rowIndex];
        editorValue = getValueByPath(row, fieldPath)?.toString() ?? ''
      }
    }
    // 上記いずれかで編集用の値が設定されていれば編集開始
    if (editorValue !== undefined) {
      setEditValue(editorValue);
      setIsEditing(true);
      setEditingCell({ rowIndex, colIndex });
    }
  }, [getIsReadOnly, rows, columnDefs]);

  // キーボードで入力された文字をベースにして編集開始
  const startEditingWithCharacter = useCallback((rowIndex: number, colIndex: number, char: string) => {
    if (getIsReadOnly(rowIndex)) return;

    // 初期値として入力された文字をセット
    setEditValue(char);
    setIsEditing(true);
    setEditingCell({ rowIndex, colIndex });
  }, [getIsReadOnly, rows, columnDefs]);

  // 編集確定
  const confirmEdit = useEvent(() => {
    if (!editingCell) {
      setIsEditing(false);
      return;
    }

    const { rowIndex, colIndex } = editingCell;

    if (!onCellEdited || getIsReadOnly(rowIndex)) {
      setIsEditing(false);
      setEditingCell(null);
      return;
    }

    const targetRow = rows[rowIndex];
    const fieldPath = columnDefs[colIndex]?.fieldPath;
    if (!fieldPath) {
      setIsEditing(false);
      setEditingCell(null);
      return;
    }

    // onCellEdited イベントを呼ぶ。
    // 状態の変更は画面側に任せる。
    const newRow = cloneRow ? cloneRow(targetRow) : structuredClone(targetRow);
    setValueByPath(newRow, fieldPath, editValue);
    onCellEdited({ rowIndex, oldRow: targetRow, newRow });

    setIsEditing(false);
    setEditingCell(null);
  });

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
    startEditing: startEditingByCurrentValue,
    startEditingWithCharacter,
    confirmEdit,
    cancelEdit,
    handleEditValueChange
  };
}

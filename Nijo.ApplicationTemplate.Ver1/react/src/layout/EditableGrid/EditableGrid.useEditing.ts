import { useState, useCallback } from "react";
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
    onChangeRow,
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

    // `onStartEditing` で編集開始時の値を決定する。
    // 決定できなければ編集を開始しない。
    let editorValue: string | undefined = undefined;
    if (!colDef?.onStartEditing) return
    colDef.onStartEditing({
      rowIndex,
      row: rows[rowIndex],
      setEditorInitialValue: value => editorValue = value,
    });
    if (editorValue === undefined) return

    setEditValue(editorValue);
    setIsEditing(true);
    setEditingCell({ rowIndex, colIndex });
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
    const colDef = columnDefs[colIndex];

    // 変更に必要な関数が指定されていない場合は処理中断
    if (!onChangeRow || !colDef.onEndEditing || getIsReadOnly(rowIndex)) {
      setIsEditing(false);
      setEditingCell(null);
      return;
    }

    // 列定義の onEndEditing と onChangeRow を呼ぶ。
    // 具体的に変更をオブジェクトに反映させるロジックはここでは定義しない。
    const oldRow = rows[rowIndex];
    let newRow: TRow | undefined = undefined;
    colDef.onEndEditing({
      rowIndex,
      row: oldRow,
      value: editValue,
      setEditedRow: row => newRow = row,
    })
    if (newRow) {
      onChangeRow({ changedRows: [{ rowIndex, oldRow, newRow }] });
    }

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

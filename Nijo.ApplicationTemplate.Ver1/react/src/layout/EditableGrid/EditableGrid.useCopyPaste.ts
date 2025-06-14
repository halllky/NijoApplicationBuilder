import React, { useCallback } from "react";
import useEvent from "react-use-event-hook";
import type * as ReactHookForm from 'react-hook-form';
import { getValueByPath } from "./EditableGrid.utils";
import { EditableGridProps, EditableGridColumnDef } from "./types";
import { toTsvString, fromTsvString } from "../../util/tsv-util";

interface UseCopyPasteParams<TRow extends ReactHookForm.FieldValues> {
  rows: TRow[];
  columnDefs: EditableGridColumnDef<TRow>[];
  activeCell: { rowIndex: number; colIndex: number } | null;
  selectedRange: {
    startRow: number;
    startCol: number;
    endRow: number;
    endCol: number;
  } | null;
  isEditing: boolean;
  getIsReadOnly: (rowIndex: number) => boolean;
  props: EditableGridProps<TRow>;
}

export const useCopyPaste = <TRow extends ReactHookForm.FieldValues,>({
  rows,
  columnDefs,
  activeCell,
  selectedRange,
  isEditing,
  getIsReadOnly,
  props
}: UseCopyPasteParams<TRow>) => {

  // クリップボードへのコピー処理
  const handleCopy: React.ClipboardEventHandler = useEvent(e => {
    if (isEditing || !selectedRange || !rows.length) return;

    // 選択範囲内のセルの値を取得
    const dataArray: string[][] = [];
    for (let r = selectedRange.startRow; r <= selectedRange.endRow; r++) {
      const rowData: string[] = [];
      for (let c = selectedRange.startCol; c <= selectedRange.endCol; c++) {
        // セルの値を文字列に変換
        const colDef = columnDefs[c];
        let cellValue = '';

        if (colDef && r < rows.length) {
          if (colDef.onStartEditing) {
            // onStartEditingを使って値を取得
            colDef.onStartEditing({
              rowIndex: r,
              row: rows[r],
              setEditorInitialValue: (value: string) => {
                cellValue = value;
              }
            });
          } else if (colDef.fieldPath) {
            // fieldPathから値を取得
            const value = getValueByPath(rows[r], colDef.fieldPath);
            cellValue = value !== undefined ? String(value) : '';
          }
        }

        rowData.push(cellValue);
      }
      dataArray.push(rowData);
    }

    // TSV形式に変換してクリップボードにセット
    const tsvData = toTsvString(dataArray);
    if (e.clipboardData) {
      e.clipboardData.setData('text/plain', tsvData);
      e.preventDefault(); // デフォルトのコピー動作を防止
    }
  });

  // stringの2次元配列を選択範囲にセットする
  const setStringValuesToSelectedRange = useEvent((values: string[][]) => {
    if (!activeCell || !rows.length || getIsReadOnly(activeCell.rowIndex)) return;

    // 変更イベントが未定義の場合は処理しても意味がないのでスキップ
    if (!props.onChangeRow) return;

    // セルの値をまとめてクリアしたい場合があるので、
    // ペーストデータのうち長さ0の配列部分は長さ1の配列と読み替える
    if (values.length === 0) {
      values = [['']];
    } else {
      for (let i = 0; i < values.length; i++) {
        const row = values[i];
        if (row.length === 0) {
          values[i] = ['']
        }
      }
    }

    // ペースト対象の範囲計算
    const startRow = selectedRange ? selectedRange.startRow : activeCell.rowIndex;
    const startCol = selectedRange ? selectedRange.startCol : activeCell.colIndex;

    // 選択範囲の行数に関わらず、ペーストデータの行数に基づいて範囲を拡張する
    // これにより複数行のデータがペーストされる
    let endRow = selectedRange ? selectedRange.endRow : activeCell.rowIndex;
    const calculatedEndRow = startRow + values.length - 1;

    // ペーストデータの行数が選択範囲より多い場合、範囲を拡張
    if (calculatedEndRow > endRow) {
      endRow = Math.min(calculatedEndRow, rows.length - 1);
    }

    const endCol = selectedRange ? selectedRange.endCol : activeCell.colIndex;

    const rowCount = endRow - startRow + 1;
    const colCount = endCol - startCol + 1;

    const changedRows: {
      rowIndex: number;
      oldRow: TRow;
      newRow: TRow;
    }[] = [];

    // 各行ごとにデータを適用
    for (let r = 0; r < rowCount; r++) {
      const targetRowIndex = startRow + r;
      if (targetRowIndex >= rows.length) break;

      // ペーストデータの行インデックス（ループさせる）
      const pasteRowIdx = r % values.length;
      const rowData = values[pasteRowIdx];
      if (!rowData.length) continue;

      let rowChanged = false;
      const originalRow = rows[targetRowIndex];

      // 編集中の行。onEndEditingのたびに新しいインスタンスに洗い替えられる。
      // 一度に複数列ペーストしたときは、列ごとに少しずつプロパティが変わっていく。
      let editingRow = originalRow;

      // 各列ごとにデータを適用
      for (let c = 0; c < colCount; c++) {
        const targetColIndex = startCol + c;
        if (targetColIndex >= columnDefs.length) break;

        // ペーストデータの列インデックス（ループさせる）
        const pasteColIdx = c % rowData.length;
        const colDef = columnDefs[targetColIndex];

        // 必要な関数が定義されていないならスキップ
        if (!colDef) continue;
        if (!colDef.onEndEditing) continue;

        // 読み取り専用ならスキップ
        if (colDef.isReadOnly === true) continue;
        if (typeof colDef.isReadOnly === 'function' && colDef.isReadOnly(editingRow, targetRowIndex)) continue;

        const pasteValue = rowData[pasteColIdx];

        // セルの値を設定
        colDef.onEndEditing({
          rowIndex: targetRowIndex,
          row: editingRow,
          value: pasteValue,
          setEditedRow: (editedRow) => {
            editingRow = editedRow;
            rowChanged = true; // 変更があったことをフラグで記録
          }
        });
      }

      // この行に変更があった場合のみchangedRowsに追加する
      if (rowChanged) {
        changedRows.push({
          rowIndex: targetRowIndex,
          oldRow: originalRow,
          newRow: editingRow
        });
      }
    }

    // 変更があった場合、onChangeRowコールバックを呼び出す
    if (changedRows.length > 0) {
      props.onChangeRow({ changedRows });
    }
  });

  // クリップボードからのペースト処理
  const handlePaste: React.ClipboardEventHandler = useEvent(e => {
    if (isEditing || !activeCell || getIsReadOnly(activeCell.rowIndex)) return;

    try {
      // クリップボードからテキストを取得。
      // 空文字をペーストしたいケースがありうるのでスキップはしない
      const clipboardText = e.clipboardData?.getData('text/plain') || '';

      // TSV形式のテキストを2次元配列に変換
      const pastedData = fromTsvString(clipboardText);

      setStringValuesToSelectedRange(pastedData);

      e.preventDefault(); // デフォルトのペースト動作を防止

    } catch (err) {
      console.error('クリップボードからのペーストに失敗しました:', err);
    }
  });

  return {
    handleCopy,
    handlePaste,
    /** stringの2次元配列を選択範囲にセットする */
    setStringValuesToSelectedRange,
  };
};

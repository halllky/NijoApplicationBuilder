import * as ReactHookForm from "react-hook-form"
import { EditableGridColumnDef, EditableGridColumnDefOnEndEditing, EditableGridColumnDefOptions } from "./types"

/** 列定義ヘルパー関数の一覧を返します。 */
export const useCellTypes = <TRow extends ReactHookForm.FieldValues>(): ColumnDefFactories<TRow> => {
  return {
    /** 既定の文字列型セル */
    text: (fieldPath, header, options) => {
      return {
        header,
        fieldPath,
        onEndEditing: getDefaultOnEndEditing(fieldPath),
        ...options,
      };
    },
    /** 既定の数値型セル */
    number: (fieldPath, header, options) => {
      return {
        header,
        fieldPath,
        onEndEditing: getDefaultOnEndEditing(fieldPath),
        ...options,
      };
    },
    /** 既定の日付型セル */
    date: (fieldPath, header, options) => {
      return {
        header,
        fieldPath,
        onEndEditing: getDefaultOnEndEditing(fieldPath),
        ...options,
      };
    },
    /** 既定の真偽値型セル */
    boolean: (fieldPath, header, options) => {
      return {
        header,
        fieldPath,
        onEndEditing: getDefaultOnEndEditing(fieldPath),
        ...options,
      };
    },
    /** その他の型のセル */
    other: (header, options) => {
      return {
        header,
        ...options,
      };
    },
  };
}

// -----------------------------------------
/** fieldPath でバインドされる列の既定の行への変更反映ロジック */
const getDefaultOnEndEditing = <TRow extends ReactHookForm.FieldValues>(fieldPath: ReactHookForm.FieldPath<TRow>): EditableGridColumnDefOnEndEditing<TRow> => {
  return e => {
    // structuredClone でオブジェクトを複製し、
    // React hook form の set でネストされたオブジェクトにも安全に値を設定する。
    const clone = window.structuredClone(e.row)
    ReactHookForm.set(clone, fieldPath, e.value)
    e.setEditedRow(clone)
  }
}

// -----------------------------------------

/** このアプリケーションで定義可能な、グリッドの列定義の種類の一覧 */
export type ColumnDefFactories<TRow extends ReactHookForm.FieldValues> = {
  /** 文字列型の列を定義します。 */
  text: BoundColumnDefFactory<TRow, string | undefined>
  /** 数値型の列を定義します。 */
  number: BoundColumnDefFactory<TRow, number | undefined>
  /** 日付型の列を定義します。 */
  date: BoundColumnDefFactory<TRow, string | undefined>
  /** 真偽値型の列を定義します。 */
  boolean: BoundColumnDefFactory<TRow, boolean>
  /** その他の型の列を定義します。 */
  other: UnboundColumnDefFactory<TRow>
}

/** フィールドの特定のプロパティと紐づいた列定義を生成する関数。 */
export type BoundColumnDefFactory<TRow extends ReactHookForm.FieldValues, TCellValueType> = (
  /** この列と紐づけるフィールド名 */
  fieldName: ReactHookForm.FieldPathByValue<TRow, TCellValueType>,
  /** ヘッダーに表示する文字列 */
  header: string,
  /** オプション */
  options?: EditableGridColumnDefOptions<TRow>
) => EditableGridColumnDef<TRow>

/** ボタンなど、特定のプロパティと紐づかない列定義を生成する関数。 */
export type UnboundColumnDefFactory<TRow extends ReactHookForm.FieldValues> = (
  /** ヘッダーに表示する文字列 */
  header: string,
  /** オプション */
  options?: EditableGridColumnDefOptions<TRow>
) => EditableGridColumnDef<TRow>

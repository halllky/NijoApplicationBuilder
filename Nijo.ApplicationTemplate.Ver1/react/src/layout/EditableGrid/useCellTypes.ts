import * as ReactHookForm from "react-hook-form"
import { EditableGridColumnDef, EditableGridColumnDefOptions } from "./index.d"

/** 列定義ヘルパー関数の一覧を返します。 */
export const useCellTypes = <TRow extends ReactHookForm.FieldValues>(): ColumnDefFactories<TRow> => {
  const textCellFactory: BoundColumnDefFactory<TRow, string | undefined> = (fieldName, header, options) => {
    return {
      header,
      fieldPath: fieldName,
      ...options,
    };
  };

  const numberCellFactory: BoundColumnDefFactory<TRow, number | undefined> = (fieldName, header, options) => {
    return {
      header,
      fieldPath: fieldName,
      ...options,
    };
  };

  const dateCellFactory: BoundColumnDefFactory<TRow, string | undefined> = (fieldName, header, options) => {
    return {
      header,
      fieldPath: fieldName,
      ...options,
    };
  };

  const booleanCellFactory: BoundColumnDefFactory<TRow, boolean> = (fieldName, header, options) => {
    return {
      header,
      fieldPath: fieldName,
      ...options,
    };
  };

  return {
    text: textCellFactory,
    number: numberCellFactory,
    date: dateCellFactory,
    boolean: booleanCellFactory
  };
}

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
) => EditableGridColumnDef<TRow>

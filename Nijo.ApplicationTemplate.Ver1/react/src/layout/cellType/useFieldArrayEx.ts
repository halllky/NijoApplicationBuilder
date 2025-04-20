import * as ReactHookForm from "react-hook-form"

/** セル型定義ヘルパー関数を返します。 */
export const useFieldArrayEx = <
  TField extends ReactHookForm.FieldValues,
  TFieldArrayName extends ReactHookForm.FieldArrayPath<TField>
>(props: ReactHookForm.UseFieldArrayProps<TField, TFieldArrayName>): UseFieldArrayExReturn<TField, TFieldArrayName> => {
  throw new Error("Not implemented")
}

/** 列定義ヘルパー関数の一覧を返します。 */
export const useCellTypes = <TRow extends ReactHookForm.FieldValues>(): ColumnDefFactories<TRow> => {
  const textCellFactory: BoundColumnDefFactory<TRow, string | undefined> = (fieldName, header, options) => {
    return {
      header,
      fieldPath: fieldName as string
    };
  };

  const numberCellFactory: BoundColumnDefFactory<TRow, number | undefined> = (fieldName, header, options) => {
    return {
      header,
      fieldPath: fieldName as string
    };
  };

  const dateCellFactory: BoundColumnDefFactory<TRow, string | undefined> = (fieldName, header, options) => {
    return {
      header,
      fieldPath: fieldName as string
    };
  };

  const booleanCellFactory: BoundColumnDefFactory<TRow, boolean> = (fieldName, header, options) => {
    return {
      header,
      fieldPath: fieldName as string
    };
  };

  return {
    text: textCellFactory,
    number: numberCellFactory,
    date: dateCellFactory,
    boolean: booleanCellFactory
  };
}

/** フィールド配列。react-hook-formのuseFieldArrayを拡張したもの。 */
export type UseFieldArrayExReturn<
  TField extends ReactHookForm.FieldValues,
  TFieldArrayName extends ReactHookForm.FieldArrayPath<TField>
> = ReactHookForm.UseFieldArrayReturn<TField, TFieldArrayName> & {
  /** 列定義ヘルパー関数の一覧 */
  cellType: ColumnDefFactories<ReactHookForm.FieldArrayWithId<TField, TFieldArrayName>>
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
  options?: ColumnDefOptions
) => ColumnDef<TRow, TCellValueType>

/** ボタンなど、特定のプロパティと紐づかない列定義を生成する関数。 */
export type UnboundColumnDefFactory<TRow, TCellValueType> = (
) => ColumnDef<TRow, TCellValueType>

/** グリッドの列定義 */
export type ColumnDef<TRow, TCellValueType = unknown> = {
  /** ヘッダーに表示する文字列 */
  header: string;
  /** この列と紐づけるフィールドパス */
  fieldPath?: string;
}

export type ColumnDefOptions = {
  /** 列の幅 */
  defaultWidth?: number
}

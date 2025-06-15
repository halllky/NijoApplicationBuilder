import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import { EditableGridColumnDef, EditableGridColumnDefOnEndEditing, EditableGridColumnDefOnStartEditing, EditableGridColumnDefOptions, RowChangeEvent } from "./types"
import { getValueByPath, setValueByPath } from "./EditableGrid.utils";

/** 列定義ヘルパー関数の一覧を返します。 */
export const useCellTypes = <TRow extends ReactHookForm.FieldValues>(
  onChangeRow: RowChangeEvent<TRow> | undefined,
): ColumnDefFactories<TRow> => {

  return React.useMemo(() => ({
    /** 既定の文字列型セル */
    text: (fieldPath, header, options) => {
      return {
        header,
        fieldPath,
        onStartEditing: e => {
          const value = getValueByPath(e.row, fieldPath) as string | undefined
          e.setEditorInitialValue(value?.toString() ?? '')
        },
        onEndEditing: getDefaultOnEndEditing(fieldPath),
        ...options,
      };
    },
    /** 既定の数値型セル */
    number: (fieldPath, header, options) => {
      return {
        header,
        fieldPath,
        onStartEditing: e => {
          const value = getValueByPath(e.row, fieldPath) as number | undefined
          e.setEditorInitialValue(value?.toString() ?? '')
        },
        onEndEditing: getDefaultOnEndEditing(fieldPath),
        ...options,
      };
    },
    /** 既定の日付型セル */
    date: (fieldPath, header, options) => {
      return {
        header,
        fieldPath,
        onStartEditing: e => {
          const value = getValueByPath(e.row, fieldPath) as string | undefined
          e.setEditorInitialValue(value?.toString() ?? '')
        },
        onEndEditing: getDefaultOnEndEditing(fieldPath),
        ...options,
      };
    },
    /** 既定の真偽値型セル */
    boolean: (fieldPath, header, options) => {
      return {
        header,
        fieldPath,
        ...options,
        renderCell: options?.renderCell ?? (context => (
          <div className="w-full">
            {getValueByPath(context.row.original, fieldPath) ? '✔' : ''}
          </div>
        )),
        onStartEditing: options?.onStartEditing ?? (e => {
          const value = getValueByPath(e.row, fieldPath) as boolean | undefined
          e.setEditorInitialValue(value ? '✔' : '')
        }),
        onEndEditing: options?.onEndEditing ?? (e => {
          const clone = window.structuredClone(e.row)
          setValueByPath(clone, fieldPath, e.value === '✔')
          e.setEditedRow(clone)
        }),
        getOptions: options?.getOptions ?? (() => ([
          { label: '✔', value: '✔' },
          { label: '', value: '' },
        ]))
      };
    },
    /** その他の型のセル */
    other: (header, options) => {
      return {
        header,
        ...options,
      };
    },
  }), [onChangeRow])
}

// -----------------------------------------
/** fieldPath でバインドされる列の既定の行への変更反映ロジック */
const getDefaultOnEndEditing = <TRow extends ReactHookForm.FieldValues>(fieldPath: ReactHookForm.FieldPath<TRow>): EditableGridColumnDefOnEndEditing<TRow> => {
  return e => {
    const clone = window.structuredClone(e.row)
    setValueByPath(clone, fieldPath, e.value)
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
  boolean: BoundColumnDefFactory<TRow, boolean | undefined>
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

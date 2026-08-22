import React from "react"
import * as ReactHookForm from "react-hook-form"
import { EditableGrid2Column, EditableGrid2LeafColumn, EditableGrid2Props, EditableGrid2Ref, EditableGridCellEditor, EditableGridCellEditorProps, EditableGridCellEditorRef } from "@halllky/react-editable-grid"
import { createTextCellHelper } from "./GridCell.Text"
import { createCheckBoxCellHelper } from "./GridCell.CheckBox"
import { createButtonCellHelper } from "./GridCell.Button"
import { createDropdownCellHelper } from "./GridCell.Dropdown"
import { createComboBoxCellHelper } from "./GridCell.TypeComboBox"
import { createElementNameCellHelper } from "./GridCell.ElementName"

/**
 * EditableGrid2 を react-hook-form の useFieldArray と組み合わせて使用する際の
 * 定型的な処理をまとめたカスタムフック。
 *
 * このフックをバイパスして直接列定義を指定してもよいが、こちらを使うと楽。
 */
export function useFieldArrayForEditableGrid2<
  TField extends ReactHookForm.FieldValues,
  TArrayPath extends ReactHookForm.ArrayPath<TField>,
  TKeyName extends string = 'id'
>(
  formProps: ReactHookForm.UseFieldArrayProps<TField, TArrayPath, TKeyName> & {
    getValues: ReactHookForm.UseFormGetValues<TField>
    setValue: ReactHookForm.UseFormSetValue<TField>
    /** 子孫集約編集グリッドの場合、先頭の1行はルート集約なので、それをスキップする */
    skipFirstRow?: boolean
  },
  getColumnDef: GetColumnDefWithHelper<ReactHookForm.FieldArrayWithId<TField, TArrayPath, TKeyName>>,
  getColumnDefDependencies: React.DependencyList
) {
  type TRow = ReactHookForm.FieldArrayWithId<TField, TArrayPath, TKeyName>

  // react-hook-form
  const { getValues, setValue, skipFirstRow, ...fieldArrayProps } = formProps
  const fieldArrayReturn = ReactHookForm.useFieldArray<TField, TArrayPath, TKeyName>(fieldArrayProps)

  // 列定義
  const gridRef = React.useRef<EditableGrid2Ref<TRow>>(null)
  const helper = React.useMemo((): ColumnDefHelper<TRow> => {
    const get = getValues as ReactHookForm.UseFormGetValues<ReactHookForm.FieldValues>
    const set = setValue as ReactHookForm.UseFormSetValue<ReactHookForm.FieldValues>
    const ctl = formProps.control as ReactHookForm.Control<ReactHookForm.FieldValues>
    return {
      text: createTextCellHelper(get, set, ctl, fieldArrayProps.name, skipFirstRow),
      checkBox: createCheckBoxCellHelper(get, set, ctl, fieldArrayProps.name, skipFirstRow),
      button: createButtonCellHelper(get, ctl, fieldArrayProps.name, skipFirstRow, gridRef),
      dropdown: createDropdownCellHelper(get, set, ctl, fieldArrayProps.name, skipFirstRow),
      elementName: createElementNameCellHelper(get, set, ctl, fieldArrayProps.name, skipFirstRow),
      typeComboBox: createComboBoxCellHelper(get, set, ctl, fieldArrayProps.name, skipFirstRow),
    }
  }, [getValues, setValue, formProps.control, fieldArrayProps.name, skipFirstRow])

  const data = React.useMemo(() => {
    return skipFirstRow
      ? fieldArrayReturn.fields.slice(1)
      : fieldArrayReturn.fields
  }, [fieldArrayReturn.fields, skipFirstRow])

  // EditableGrid2 の props
  const editableGrid2Props: EditableGrid2Props<TRow> & { ref: React.RefObject<EditableGrid2Ref<TRow> | null> } = {
    ref: gridRef,
    data,
    columns: [
      () => getColumnDef(helper),
      [helper, ...getColumnDefDependencies]
    ],
    getRowId: row => (row as Record<string, string>)[fieldArrayProps.keyName ?? "id"],
    getLatestRowObject: index => {
      return skipFirstRow
        ? getValues(`${fieldArrayProps.name}.${index + 1}` as ReactHookForm.Path<TField>)
        : getValues(`${fieldArrayProps.name}.${index}` as ReactHookForm.Path<TField>)
    },
  }

  return {
    fieldArrayReturn,
    editableGrid2Props,
    gridRef,
  }
}

export type UseFieldArrayForEditableGrid2Return<
  TField extends ReactHookForm.FieldValues,
  TArrayPath extends ReactHookForm.ArrayPath<TField>,
  TKeyName extends string
> = {
  /** useFieldArray の返り値 */
  fieldArrayReturn: ReactHookForm.UseFieldArrayReturn<TField, TArrayPath, TKeyName>
  /** EditableGrid2 の引数。スプレッド構文でそのまま渡すこと */
  editableGrid2Props: EditableGrid2Props<ReactHookForm.FieldArrayWithId<TField, TArrayPath, TKeyName>>
  /** グリッドの参照オブジェクト。EditableGrid2Ref 型として使用可能 */
  gridRef: React.RefObject<EditableGrid2Ref<ReactHookForm.FieldArrayWithId<TField, TArrayPath, TKeyName>>>
}

//#region 列定義ヘルパー

/** EditableGrid2 標準の列定義処理にヘルパー関数を追加したもの */
export type GetColumnDefWithHelper<TRow> = (helper: ColumnDefHelper<TRow>) => EditableGrid2Column<TRow>[]

/** 列定義ヘルパー */
export type ColumnDefHelper<TRow> = {
  /** テキスト列 */
  text: ReturnType<typeof createTextCellHelper>
  /** チェックボックス列 */
  checkBox: ReturnType<typeof createCheckBoxCellHelper>
  /** ボタン列 */
  button: ReturnType<typeof createButtonCellHelper>
  /** ドロップダウン列 */
  dropdown: ReturnType<typeof createDropdownCellHelper>
  /** XML要素の名前列。インデントつきで表示される。 */
  elementName: ReturnType<typeof createElementNameCellHelper>
  /** 種類のコンボボックス列 */
  typeComboBox: ReturnType<typeof createComboBoxCellHelper>
}

//#endregion 列定義ヘルパー

import * as React from "react"
import * as ReactHookForm from "react-hook-form"

/** EditableGridのプロパティ */
export type EditableGridProps<
  TField extends ReactHookForm.FieldValues,
  TArrayName extends ReactHookForm.ArrayPath<TField>,
> = {
  /** react-hook-formのuseFieldArrayの返り値 */
  useFieldArrayReturn: ReactHookForm.UseFieldArrayReturn<TField, TArrayName>
}

/** EditableGridのref */
export type EditableGridRef<
  TField extends ReactHookForm.FieldValues,
  TArrayName extends ReactHookForm.ArrayPath<TField>,
> = {
  /** 選択された行を取得する */
  getSelectedRows: () => { row: ReactHookForm.FieldArrayWithId<TField, TArrayName>, rowIndex: number }[]
}


/**
 * 編集可能なグリッドを表示するコンポーネント
*/
export const EditableGrid = React.forwardRef(<
  TField extends ReactHookForm.FieldValues,
  TArrayName extends ReactHookForm.ArrayPath<TField>
>(
  props: EditableGridProps<TField, TArrayName>,
  ref: React.ForwardedRef<EditableGridRef<TField, TArrayName>>
) => {

  type TRow<
    TField extends ReactHookForm.FieldValues,
    TArrayName extends ReactHookForm.ArrayPath<TField>,
  > = ReactHookForm.FieldArrayWithId<TField, TArrayName>

  return (
    <div>
      {/* TODO: グリッドを作成 */}
    </div>
  )
})

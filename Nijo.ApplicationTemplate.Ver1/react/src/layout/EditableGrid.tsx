import * as React from "react"
import { EditableGridProps, EditableGridRef } from "./EditableGrid.d"

/**
 * 編集可能なグリッドを表示するコンポーネント
*/
export const EditableGrid = React.forwardRef(<TRow,>(
  props: EditableGridProps<TRow>,
  ref: React.ForwardedRef<EditableGridRef<TRow>>
) => {

  return (
    <div>
      {/* TODO: グリッドを作成 */}
    </div>
  )
})

import * as ReactHookForm from "react-hook-form"
import { EditableGrid2LeafColumn, EditableGrid2Ref } from "@halllky/react-editable-grid"

export type CreateButtonCellFunction = <TRow>(
  text: (row: TRow, rowIndex: number) => React.ReactNode,
  onClick: (row: TRow, rowIndex: number) => void,
  options?: Partial<EditableGrid2LeafColumn<TRow>> & {
    disableIfReadOnly?: boolean
  }
) => EditableGrid2LeafColumn<TRow>

/**
 * EditableGrid2 のボタン列
 */
export function createButtonCellHelper(
  getValues: ReactHookForm.UseFormGetValues<ReactHookForm.FieldValues>,
  control: ReactHookForm.Control<ReactHookForm.FieldValues>,
  arrayName: string,
  skipFirstRow: boolean | undefined,
  gridRef: React.RefObject<EditableGrid2Ref<ReactHookForm.FieldValues> | null>,
): CreateButtonCellFunction {

  return (text, onClick, options) => ({
    renderHeader: () => null,
    renderBody: ({ context, isReadOnly }) => {
      const fieldRowIndex = skipFirstRow ? context.row.index + 1 : context.row.index
      const row = ReactHookForm.useWatch({ control, name: `${arrayName}.${fieldRowIndex}` })

      return (
        <button type="button"
          onClick={() => {
            const current = getValues(`${arrayName}.${fieldRowIndex}`)
            onClick(current, fieldRowIndex)
            gridRef.current?.forceUpdate()
          }}
          disabled={options?.disableIfReadOnly === true && isReadOnly}
          className="w-full h-full text-sm text-white bg-teal-700 border border-white"
        >
          {text(row, fieldRowIndex)}
        </button>
      )
    },
    disableResizing: true,
    ...options,
  })
}

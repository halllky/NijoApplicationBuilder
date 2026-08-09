import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { useFieldValidationError } from "../ProjectPage/useValidation"

export type CreateCheckBoxCellFunction = <TRow>(
  header: string,
  key: ReactHookForm.Path<TRow>,
  options?: Partial<EG2.EditableGrid2LeafColumn<TRow>> & {
    /** バリデーションエラーがあるときにセル背景色を変えるための設定 */
    validationErrorSettings?: [
      getXmlElementUniqueId: (row: TRow) => string | null | undefined,
      attributeName?: string
    ]
  }
) => EG2.EditableGrid2LeafColumn<TRow>

/**
 * EditableGrid2 のチェックボックス列
 */
export function createCheckBoxCellHelper(
  getValues: ReactHookForm.UseFormGetValues<ReactHookForm.FieldValues>,
  setValue: ReactHookForm.UseFormSetValue<ReactHookForm.FieldValues>,
  control: ReactHookForm.Control<ReactHookForm.FieldValues>,
  arrayName: string,
  skipFirstRow: boolean | undefined,
): CreateCheckBoxCellFunction {

  return (header, key, options) => ({
    renderHeader: () => (
      <div className="px-1 py-px truncate text-sm text-gray-700">
        {header}
      </div>
    ),
    renderBody: ({ context, isReadOnly }) => {
      const fieldRowIndex = skipFirstRow ? context.row.index + 1 : context.row.index
      const value = ReactHookForm.useWatch({ control, name: `${arrayName}.${fieldRowIndex}.${key}` })
      const { hasError, errorMessages } = useFieldValidationError(options?.validationErrorSettings?.[0]?.(context.row.original), options?.validationErrorSettings?.[1])

      return (
        <label
          title={errorMessages.join('\n')}
          className={`self-start block h-full w-full px-1 ${isReadOnly ? '' : 'cursor-pointer'} ${hasError ? 'bg-amber-300/50' : ''}`}
        >
          <input
            type="checkbox"
            checked={!!value}
            onChange={e => setValue(
              `${arrayName}.${fieldRowIndex}.${key}`,
              e.target.checked as ReactHookForm.PathValue<ReactHookForm.FieldValues, typeof key>,
              { shouldDirty: true }
            )}
            disabled={isReadOnly}
            className="block h-6"
          />
        </label>
      )
    },
    onCellKeyDown: ({ rowIndex, event }) => {
      if (event.key === ' ' || event.code === 'Space') {
        event.preventDefault()

        const fieldRowIndex = skipFirstRow ? rowIndex + 1 : rowIndex
        const current = getValues(`${arrayName}.${fieldRowIndex}.${key}`)
        setValue(
          `${arrayName}.${fieldRowIndex}.${key}`,
          (!current) as ReactHookForm.PathValue<ReactHookForm.FieldValues, typeof key>,
          { shouldDirty: true }
        )
      }
    },
    getValueForEditor: ({ rowIndex }) => {
      const fieldRowIndex = skipFirstRow ? rowIndex + 1 : rowIndex
      const val = getValues(`${arrayName}.${fieldRowIndex}.${key}`)
      return val ? 'true' : 'false'
    },
    setValueFromEditor: ({ rowIndex, value }) => {
      const fieldRowIndex = skipFirstRow ? rowIndex + 1 : rowIndex
      const blnValue = [true, 1, 'true', '1', 'yes'].includes(typeof value === 'string' ? value.toLowerCase() : value)
      setValue(
        `${arrayName}.${fieldRowIndex}.${key}`,
        blnValue as ReactHookForm.PathValue<ReactHookForm.FieldValues, typeof key>,
        { shouldDirty: true }
      )
    },
    ...options,
  })
}

import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { MentionableTextarea, MentionableTextareaReadOnly } from "./Mention"
import { useFieldValidationError } from "../ProjectPage/useValidation"
import { EditingProject } from "../backend"
import { useMentionSuggestions } from "./useMentionSuggestions"
import { JumpToElementContext } from "../ProjectPage/useJumpToElement"

export type CreateTextCellFunction = <TRow>(
  header: string,
  key: ReactHookForm.Path<TRow>,
  options?: Partial<EG2.EditableGrid2LeafColumn<TRow>> & {
    format?: (value: unknown) => string
    parse?: (value: string) => unknown
    /** メンション使用可能かどうか */
    mentionAvailable?: boolean
    /** バリデーションエラーがあるときにセル背景色を変えるための設定 */
    validationErrorSettings?: [
      getXmlElementUniqueId: (row: TRow) => string | null | undefined,
      attributeName?: string
    ]
  }
) => EG2.EditableGrid2LeafColumn<TRow>

/**
 * EditableGrid2 のテキスト列
 */
export function createTextCellHelper(
  getValues: ReactHookForm.UseFormGetValues<ReactHookForm.FieldValues>,
  setValue: ReactHookForm.UseFormSetValue<ReactHookForm.FieldValues>,
  control: ReactHookForm.Control<ReactHookForm.FieldValues>,
  arrayName: string,
  skipFirstRow: boolean | undefined,
): CreateTextCellFunction {

  return (header, key, options) => ({
    editor: options?.mentionAvailable
      ? MentionableCellEditor
      : TextCellEditor,
    renderHeader: () => (
      <div className="px-1 py-px truncate text-sm text-gray-700">
        {header}
      </div>
    ),
    renderBody: ({ context }) => {
      const fieldRowIndex = skipFirstRow ? context.row.index + 1 : context.row.index
      const value: string | null | undefined = ReactHookForm.useWatch({ name: `${arrayName}.${fieldRowIndex}.${key}`, control })
      const { hasError, errorMessages } = useFieldValidationError(options?.validationErrorSettings?.[0]?.(context.row.original), options?.validationErrorSettings?.[1])
      const jumpToElement = React.useContext(JumpToElementContext)

      return options?.mentionAvailable ? (
        <MentionableTextareaReadOnly
          title={errorMessages.join('\n')}
          onClickMention={part => jumpToElement?.(part.targetId)}
          className={`w-full px-1 ${options?.wrap ? 'whitespace-pre-wrap' : 'truncate'} ${hasError ? 'bg-amber-300/50' : ''}`}
        >
          {value ?? undefined}
        </MentionableTextareaReadOnly>
      ) : (
        <div
          title={errorMessages.join('\n')}
          className={`w-full px-1 ${options?.wrap ? 'whitespace-pre-wrap' : 'truncate'} ${hasError ? 'bg-amber-300/50' : ''}`}
        >
          {options?.format?.(value) ?? value}
        </div>
      )
    },
    getValueForEditor: ({ rowIndex }) => {
      const fieldRowIndex = skipFirstRow ? rowIndex + 1 : rowIndex
      const val = getValues(`${arrayName}.${fieldRowIndex}.${key}`)
      return options?.format?.(val) ?? val?.toString() ?? ''
    },
    setValueFromEditor: ({ rowIndex, value }) => {
      const fieldRowIndex = skipFirstRow ? rowIndex + 1 : rowIndex
      const val = options?.parse?.(value) ?? value
      setValue(
        `${arrayName}.${fieldRowIndex}.${key}`,
        val as ReactHookForm.PathValue<ReactHookForm.FieldValues, typeof key>,
        { shouldDirty: true }
      )
    },
    ...options,
  })
}

/**
 * 通常のテキスト列のセルエディタ
 */
export const TextCellEditor: EG2.EditableGridCellEditor = React.forwardRef(function DefaultEditor({ style, isEditing, requestCommit, requestCancel }, ref) {
  const [value, setValue] = React.useState<string>('')
  const refInput = React.useRef<HTMLInputElement>(null)

  const handleChange: React.ChangeEventHandler<HTMLInputElement> = e => {
    setValue(e.target.value)
  }

  const handleKeyDown: React.KeyboardEventHandler<HTMLInputElement> = e => {
    if (!isEditing) return
    if (e.nativeEvent.isComposing) return // 日本語入力中は無視

    if (e.key === 'Enter') {
      requestCommit(value)
      e.preventDefault()
    }
    else if (e.key === 'Escape') {
      requestCancel()
      e.preventDefault()
    }
  }

  React.useImperativeHandle(ref, () => ({
    blur: () => refInput.current?.blur(),
    getCurrentValue: () => refInput.current?.value ?? '',
    setValueAndSelectAll: (v, timing) => {
      setValue(v)
      if (timing === 'move-focus' || timing === 'edit-end') {
        setTimeout(() => refInput.current?.select(), 0)
      }
    },
    getDomElement: () => refInput.current,
  }), [])

  return (
    <label
      style={style}
      className="px-[3px] resize-none border border-black bg-white"
    >
      <input
        ref={refInput}
        value={value ?? ''}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        spellCheck={false}
        autoComplete="off"
        className="block mt-[-1px] w-full field-sizing-content outline-none"
      />
    </label>
  )
})

/**
 * メンション使用可能な列のセルエディタ
 */
export const MentionableCellEditor: EG2.EditableGridCellEditor = React.forwardRef(({
  requestCancel,
  requestCommit,
  style,
}, ref) => {

  const { getValues } = ReactHookForm.useFormContext<EditingProject>()
  const getMentionSuggestions = useMentionSuggestions(getValues)

  const textareaRef = React.useRef<HTMLTextAreaElement>(null);
  const [value, setValue] = React.useState('');

  React.useImperativeHandle(ref, () => ({
    getCurrentValue: () => {
      return value
    },
    setValueAndSelectAll: (value, timing) => {
      setValue(value)
      if (timing === 'move-focus') {
        window.setTimeout(() => textareaRef.current?.select(), 0)
      }
    },
    getDomElement: () => textareaRef.current,
  }))

  const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = e => {
    if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) {
      e.preventDefault()
      requestCommit(value)
    } else if (e.key === 'Escape') {
      e.preventDefault()
      requestCancel()
    }
  }

  return (
    <MentionableTextarea
      getSuggestions={getMentionSuggestions}
      ref={textareaRef}
      value={value ?? ''}
      onChange={setValue}
      onKeyDown={handleKeyDown}
      style={style}
      className="bg-white border border-gray-700 [&_textarea]:px-[3px] mt-[-1px]"
    />
  )
})

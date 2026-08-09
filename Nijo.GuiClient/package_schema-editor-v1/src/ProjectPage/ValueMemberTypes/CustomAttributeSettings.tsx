import React from "react"
import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import { ModalDialog } from "@nijo/ui-components/layout"
import * as Input from "@nijo/ui-components/input"
import { GeneratedProjectInGui, NijoXmlCustomAttribute, TYPE_DATA_MODEL, TYPE_QUERY_MODEL, TYPE_COMMAND_MODEL, TYPE_STRUCTURE_MODEL, TYPE_STATIC_ENUM_MODEL, TYPE_VALUE_OBJECT_MODEL, XmlElementAttributeName } from "../../types"
import { UUID } from "uuidjs"
import FormLayout from "@nijo/ui-components/layout/FormLayout"
import { useFieldValidationError, useValidationErrorMessages } from "../useValidation"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import * as UI from "../../UI"

type CustomAttributeSettingsProps = {
  formMethods: ReactHookForm.UseFormReturn<GeneratedProjectInGui>
  elementRef?: React.RefObject<HTMLDivElement | null>
}

type GridRow = NijoXmlCustomAttribute & { id: string }

const ATTR_TYPES: NijoXmlCustomAttribute['type'][] = [
  'String',
  'Boolean',
  'Enum',
  'Decimal',
]

const AVAILABLE_MODELS = [
  { id: TYPE_DATA_MODEL, label: 'Data Model' },
  { id: TYPE_QUERY_MODEL, label: 'Query Model' },
  { id: TYPE_COMMAND_MODEL, label: 'Command Model' },
  { id: TYPE_STRUCTURE_MODEL, label: 'Structure Model' },
  { id: TYPE_STATIC_ENUM_MODEL, label: 'Enum' },
  { id: TYPE_VALUE_OBJECT_MODEL, label: 'Value Object' },
]

/**
 * カスタム属性設定欄
 */
export const CustomAttributeSettings: React.FC<CustomAttributeSettingsProps> = ({
  formMethods,
  elementRef,
}) => {
  const { control, getValues, setValue } = formMethods

  const {
    gridRef,
    editableGrid2Props,
    fieldArrayReturn: { fields: customAttributes, append, remove },
  } = UI.useFieldArrayForEditableGrid2({
    name: `customAttributes`,
    control,
    getValues,
    setValue,
  }, helper => {

    const renderBodyWithValidation = (columnId: keyof GridRow): EG2.EditableGrid2BodyRenderer<GridRow> => ({ context }) => {
      const watchedRow = ReactHookForm.useWatch({ name: `customAttributes.${context.row.index}`, control })
      const { hasError } = useFieldValidationError(watchedRow.uniqueId)
      return (
        <div className={`flex-1 inline-flex text-left truncate px-1 ${hasError ? 'bg-amber-300/50' : ''}`}>
          <span className="flex-1 truncate">
            {ReactHookForm.get(watchedRow, columnId) as string}
          </span>
        </div>
      )
    }

    return [
      helper.text("物理名", "physicalName", { defaultWidth: 150, renderBody: renderBodyWithValidation("physicalName") }),
      helper.text("表示名", "displayName", { defaultWidth: 150, renderBody: renderBodyWithValidation("displayName") }),
      helper.dropdown("タイプ", "type", ATTR_TYPES.map(type => ({ value: type, text: type })), { defaultWidth: 100, renderBody: renderBodyWithValidation("type") }),
      helper.checkBox("バリデーション", "isValidation", { defaultWidth: 110 }),
      {
        renderHeader: () => "Enum値",
        defaultWidth: 200,
        isReadOnly: row => row.type !== 'Enum',
        renderBody: ({ context }) => {
          const watchedRow = ReactHookForm.useWatch({ name: `customAttributes.${context.row.index}`, control })
          const { hasError } = useFieldValidationError(watchedRow.uniqueId)
          if (watchedRow.type !== 'Enum') return null
          return (
            <div className={`flex items-center justify-between w-full h-full px-1 ${hasError ? 'bg-amber-300/50' : ''}`}>
              <span className="truncate">{watchedRow.enumValues?.join(", ")}</span>
              <Input.IconButton mini icon={Icon.PencilIcon} onClick={() => setEditingEnumValuesIndex(context.row.index)} />
            </div>
          )
        }
      },
      {
        defaultWidth: 200,
        renderHeader: () => "利用可能モデル",
        renderBody: ({ context }) => {
          const watchedRow = ReactHookForm.useWatch({ name: `customAttributes.${context.row.index}`, control })
          const { hasError } = useFieldValidationError(watchedRow.uniqueId)
          const models = watchedRow.availableModels
          const label = models.map(m => AVAILABLE_MODELS.find(am => am.id === m)?.label ?? m).join(", ")
          return (
            <div className={`flex items-center justify-between w-full h-full px-1 ${hasError ? 'bg-amber-300/50' : ''}`}>
              <span className="truncate">{label}</span>
              <Input.IconButton mini icon={Icon.PencilIcon} onClick={() => setEditingAvailableModelsIndex(context.row.index)} />
            </div>
          )
        }
      },
      helper.text("コメント", "comment", { wrap: true, defaultWidth: 200, renderBody: renderBodyWithValidation("comment") }),
    ]
  }, [])

  const handleAddRow = useEvent(() => {
    const newAttr: NijoXmlCustomAttribute = {
      uniqueId: "Custom-" + UUID.generate().toLowerCase() as XmlElementAttributeName,
      physicalName: '',
      displayName: '',
      type: 'String',
      isValidation: false,
      availableModels: [
        TYPE_DATA_MODEL,
        TYPE_QUERY_MODEL,
        TYPE_COMMAND_MODEL,
        TYPE_STRUCTURE_MODEL,
        TYPE_STATIC_ENUM_MODEL,
        TYPE_VALUE_OBJECT_MODEL,
      ],
      enumValues: [],
    }
    append(newAttr)
  })

  const handleDeleteRow = useEvent(() => {
    const selection = gridRef.current?.getSelectedRows()
    if (!selection || selection.length === 0) return
    remove(selection.map(x => x.rowIndex))
  })

  const [editingAvailableModelsIndex, setEditingAvailableModelsIndex] = React.useState<number | null>(null)
  const [editingEnumValuesIndex, setEditingEnumValuesIndex] = React.useState<number | null>(null)

  const validationResultList = useValidationErrorMessages()
  const errorMessages = React.useMemo(() => {
    const messages: string[] = []
    for (let i = 0; i < customAttributes.length; i++) {
      const attr = customAttributes[i]
      const validationResult = validationResultList.filter(v => v.xmlElementUniqueId === attr.uniqueId)
      messages.push(...validationResult.map(err => `${i + 1}行目: ${err.message}`))
    }
    return messages
  }, [customAttributes, validationResultList])

  return (
    <FormLayout.Field fullWidth label="カスタム属性" labelEnd={(
      <div ref={elementRef ?? undefined} className="flex gap-1">
        <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleAddRow}>追加</Input.IconButton>
        <Input.IconButton outline mini icon={Icon.TrashIcon} onClick={handleDeleteRow}>削除</Input.IconButton>
      </div>
    )}>
      <ul className="text-xs text-gray-600 mb-1 mx-2 list-disc list-inside">
        <li>物理名: 生成後のソースでの名前。"IsHelpText" などパスカルケースで指定</li>
        <li>表示名: このGUIツール上での表示名</li>
        <li>タイプ: {ATTR_TYPES.join(", ")} から選択</li>
      </ul>

      <EG2.EditableGrid2
        {...editableGrid2Props}
        className="flex-1 h-[240px] resize-y border border-gray-600"
      />

      {errorMessages.length > 0 && (
        <ul className="text-amber-600 text-sm">
          {errorMessages.map((msg, i) => (
            <li key={i}>{msg}</li>
          ))}
        </ul>
      )}

      {editingAvailableModelsIndex !== null && (
        <AvailableModelsDialog
          initialSelection={customAttributes[editingAvailableModelsIndex].availableModels}
          onSave={(models) => {
            setValue(`customAttributes.${editingAvailableModelsIndex}.availableModels`, models, { shouldDirty: true })
            setEditingAvailableModelsIndex(null)
          }}
          onClose={() => setEditingAvailableModelsIndex(null)}
        />
      )}

      {editingEnumValuesIndex !== null && (
        <EnumValuesDialog
          initialValues={customAttributes[editingEnumValuesIndex].enumValues}
          onSave={(values) => {
            setValue(`customAttributes.${editingEnumValuesIndex}.enumValues`, values, { shouldDirty: true })
            setEditingEnumValuesIndex(null)
          }}
          onClose={() => setEditingEnumValuesIndex(null)}
        />
      )}
    </FormLayout.Field>
  )
}

const AvailableModelsDialog = ({ initialSelection, onSave, onClose }: { initialSelection: string[], onSave: (models: string[]) => void, onClose: () => void }) => {
  const [selection, setSelection] = React.useState(new Set(initialSelection))

  const toggle = (id: string) => {
    const newSelection = new Set(selection)
    if (newSelection.has(id)) newSelection.delete(id)
    else newSelection.add(id)
    setSelection(newSelection)
  }

  return (
    <ModalDialog open className="w-[400px]">
      <div className="p-4 flex flex-col gap-2">
        {AVAILABLE_MODELS.map(model => (
          <label key={model.id} className="flex items-center gap-2">
            <input type="checkbox" checked={selection.has(model.id)} onChange={() => toggle(model.id)} />
            {model.label}
          </label>
        ))}
        <div className="flex justify-end gap-2 mt-4">
          <Input.IconButton onClick={onClose}>キャンセル</Input.IconButton>
          <Input.IconButton fill onClick={() => onSave(Array.from(selection))}>OK</Input.IconButton>
        </div>
      </div>
    </ModalDialog>
  )
}

const EnumValuesDialog = ({ initialValues, onSave, onClose }: { initialValues: string[], onSave: (values: string[]) => void, onClose: () => void }) => {
  type EnumRow = { id: string, value: string }
  const [rows, setRows] = React.useState<EnumRow[]>(() => initialValues.map((v, i) => ({ id: `row-${i}`, value: v })))
  const rowsRef = React.useRef(rows)
  rowsRef.current = rows

  const gridRef = React.useRef<EG2.EditableGrid2Ref<EnumRow>>(null)

  const handleAdd = () => {
    setRows([...rows, { id: UUID.generate(), value: '' }])
  }

  const handleDelete = () => {
    const selection = gridRef.current?.getSelectedRows()
    if (!selection) return
    const indexes = new Set(selection.map(x => x.rowIndex))
    setRows(rows.filter((_, i) => !indexes.has(i)))
  }

  return (
    <ModalDialog open className="w-[500px] h-[400px]">
      <div className="flex flex-col h-full gap-2 p-4">
        <div className="flex gap-2">
          <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleAdd}>追加</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.TrashIcon} onClick={handleDelete}>削除</Input.IconButton>
        </div>
        <EG2.EditableGrid2
          ref={gridRef}
          data={rows}
          className="flex-1"
          columns={[() => [{
            renderHeader: () => "Value",
            renderBody: ({ context }) => (
              <span className="px-1 py-px">
                {rowsRef.current[context.row.index].value}
              </span>
            ),
            editor: UI.TextCellEditor,
            getValueForEditor: ({ rowIndex }) => rowsRef.current[rowIndex].value,
            setValueFromEditor: ({ rowIndex, value }) => setRows(rows => {
              const newRows = [...rows]
              newRows[rowIndex] = { ...newRows[rowIndex], value }
              return newRows
            }),
          }], []]}
        />
        <div className="flex justify-end gap-2 mt-2">
          <Input.IconButton onClick={onClose}>キャンセル</Input.IconButton>
          <Input.IconButton fill onClick={() => onSave(rows.map(r => r.value))}>OK</Input.IconButton>
        </div>
      </div>
    </ModalDialog>
  )
}

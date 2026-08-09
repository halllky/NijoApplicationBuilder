import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { UUID } from "uuidjs"
import {
  EditingProject, EditingMember, isAttributeAvailable, ATTR_UNIQUE_CONSTRAINTS,
  NODE_TYPE_CHILD_AGGREGATE, NODE_TYPE_CHILDREN_AGGREGATE, NODE_TYPE_VALUE_MEMBER, NODE_TYPE_REF,
} from "../../../backend"
import * as UI from '../../../UI'
import { usePersonalSettings } from "../../../PersonalSettings"
import { useSchemaEditorRule } from "../../SchemaEditorRuleContext"
import { RootAggregateLocation } from "../../rootAggregateLocation"
import { UniqueConstraintsContext, useUniqueConstraintsColumns } from "./useUniqueConstraintColumns"

/** グリッドに新しい行を挿入するときの初期値 */
const newEmptyMember = (indent: number): EditingMember => ({
  uniqueId: UUID.generate(),
  indent,
  physicalName: '',
  type: { kind: 'unknown' },
  attributes: {},
  uniqueConstraints: [],
})

/**
 * 子孫集約編集グリッド
 */
function DecsendantsGrid(props: {
  rootLocation: RootAggregateLocation
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
  className?: string
}) {

  type GridRowType = ReactHookForm.FieldArrayWithId<EditingProject, `${RootAggregateLocation['list']}.${number}.members`, 'id'>

  const {
    rootLocation,
    formMethods: { control, getValues, setValue },
    className,
  } = props

  const membersPath = `${rootLocation.list}.${rootLocation.index}.members` as const

  const { attributeDefs } = useSchemaEditorRule()
  const customAttributes = ReactHookForm.useWatch({ name: "customAttributes", control }) ?? []
  const rootModelType = ReactHookForm.useWatch({ name: `${rootLocation.list}.${rootLocation.index}.model`, control })

  const { uniqueConstraintColumns, contextValue: uniqueConstraintsContextValue } = useUniqueConstraintsColumns(control, getValues, setValue, rootLocation)

  const {
    fieldArrayReturn: { insert, remove, move, update },
    editableGrid2Props,
    gridRef,
  } = UI.useFieldArrayForEditableGrid2({
    name: membersPath,
    control,
    getValues,
    setValue,
  }, helper => {
    const columns: EG2.EditableGrid2Column<GridRowType>[] = []

    // 名前
    columns.push(helper.elementName(''))

    // 種類
    columns.push(helper.typeComboBox('種類', {
      defaultWidth: 160,
    }))

    // コメント
    columns.push(helper.text('コメント', 'comment', {
      defaultWidth: 400,
      mentionAvailable: true,
      wrap: true,
    }))

    // モデルの既定の属性
    for (const attrDef of attributeDefs) {
      if (!rootModelType || !isAttributeAvailable(attrDef, rootModelType, [
        NODE_TYPE_CHILD_AGGREGATE,
        NODE_TYPE_CHILDREN_AGGREGATE,
        NODE_TYPE_VALUE_MEMBER,
        NODE_TYPE_REF,
      ])) continue;

      if (attrDef.attributeName === ATTR_UNIQUE_CONSTRAINTS) {
        columns.push(uniqueConstraintColumns)

      } else if (attrDef.type === 'EnumSelect') {
        const ddlOptions = (attrDef.typeEnumValues ?? [])
          .map(opt => ({ value: opt, text: opt }))
        columns.push(helper.dropdown(attrDef.displayName, `attributes.${attrDef.attributeName}`, ddlOptions, {
          defaultWidth: 120,
          validationErrorSettings: [row => row.uniqueId, attrDef.attributeName],
        }))
      } else if (attrDef.type === 'Boolean') {
        columns.push(helper.checkBox(attrDef.displayName, `attributes.${attrDef.attributeName}`, {
          defaultWidth: 120,
          validationErrorSettings: [row => row.uniqueId, attrDef.attributeName],
        }))
      } else {
        columns.push(helper.text(attrDef.displayName, `attributes.${attrDef.attributeName}`, {
          defaultWidth: 120,
          validationErrorSettings: [row => row.uniqueId, attrDef.attributeName],
        }))
      }
    }

    // カスタム属性
    for (const customAttr of customAttributes) {
      if (!rootModelType || !customAttr.availableModels.includes(rootModelType)) continue;

      if (customAttr.type === 'Enum') {
        const ddlOptions = customAttr.enumValues.map(opt => ({ value: opt, text: opt }))
        columns.push(helper.dropdown(customAttr.displayName ?? customAttr.physicalName ?? '', `attributes.${customAttr.uniqueId}`, ddlOptions, {
          defaultWidth: 120,
          validationErrorSettings: [row => row.uniqueId, customAttr.uniqueId ?? undefined],
        }))

      } else if (customAttr.type === 'Boolean') {
        columns.push(helper.checkBox(customAttr.displayName ?? customAttr.physicalName ?? '', `attributes.${customAttr.uniqueId}`, {
          defaultWidth: 120,
          validationErrorSettings: [row => row.uniqueId, customAttr.uniqueId ?? undefined],
        }))

      } else {
        columns.push(helper.text(customAttr.displayName ?? customAttr.physicalName ?? '', `attributes.${customAttr.uniqueId}`, {
          defaultWidth: 120,
          validationErrorSettings: [row => row.uniqueId, customAttr.uniqueId ?? undefined],
        }))
      }
    }

    return columns
  }, [attributeDefs, rootModelType, customAttributes, uniqueConstraintColumns])

  // Handlers
  const handleInsertRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) {
      insert(0, newEmptyMember(1))
    } else {
      const insertPosition = selectedRows[0].rowIndex
      const indent = getValues(`${membersPath}.${insertPosition}.indent`) ?? 1
      const count = selectedRows.length
      const insertRows = Array.from({ length: count }, () => newEmptyMember(indent))
      insert(insertPosition, insertRows)
    }

    // グリッド未選択なら行選択
    window.setTimeout(() => {
      const selectedRows = gridRef.current?.getSelectedRows()
      if (!selectedRows || selectedRows.length === 0) gridRef.current?.selectRow(0, 0)
    }, 10)
  }

  const handleInsertRowBelow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) {
      insert(0, newEmptyMember(1))
    } else {
      const insertPosition = selectedRows[selectedRows.length - 1].rowIndex + 1
      const indent = getValues(`${membersPath}.${insertPosition}.indent`) ?? 1
      const count = selectedRows.length
      const insertRows = Array.from({ length: count }, () => newEmptyMember(indent))
      insert(insertPosition, insertRows)
    }

    // グリッド未選択なら行選択
    window.setTimeout(() => {
      const selectedRows = gridRef.current?.getSelectedRows()
      if (!selectedRows || selectedRows.length === 0) gridRef.current?.selectRow(0, 0)
    }, 10)
  }

  const handleDeleteRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const removedIndexes = selectedRows.map(row => row.rowIndex)
    remove(removedIndexes)
  }

  const handleMoveUp = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const startRow = selectedRows[0].rowIndex
    const endRow = startRow + selectedRows.length - 1
    if (startRow <= 0) return

    move(startRow - 1, endRow)

    // Restore selection
    gridRef.current?.selectRow(selectedRows[0].rowIndex - 1, selectedRows[0].rowIndex + selectedRows.length - 2)
  }

  const handleMoveDown = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const startRow = selectedRows[0].rowIndex
    const endRow = startRow + selectedRows.length - 1
    if (endRow >= getValues(membersPath).length - 1) return

    move(endRow + 1, startRow)

    gridRef.current?.selectRow(selectedRows[0].rowIndex + 1, selectedRows[0].rowIndex + selectedRows.length)
  }

  const handleIndentDown = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows) return
    for (const x of selectedRows) {
      update(x.rowIndex, { ...x.row, indent: Math.max(1, x.row.indent - 1) })
    }
  }

  const handleIndentUp = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows) return
    for (const x of selectedRows) {
      update(x.rowIndex, { ...x.row, indent: x.row.indent + 1 })
    }
  }

  // Key handlers
  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = e => {
    if (gridRef.current?.isEditing) return;

    if (!e.ctrlKey && e.key === 'Enter' && !e.defaultPrevented) {
      handleInsertRow()
    } else if (e.ctrlKey && e.key === 'Enter' && !e.defaultPrevented) {
      handleInsertRowBelow()
    } else if (e.shiftKey && e.key === 'Delete') {
      handleDeleteRow()
    } else if (e.altKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown') && !e.defaultPrevented) {
      if (e.key === 'ArrowUp') handleMoveUp()
      else if (e.key === 'ArrowDown') handleMoveDown()
    } else if (e.shiftKey && e.key === 'Tab') {
      handleIndentDown()
    } else if (e.key === 'Tab') {
      handleIndentUp()
    } else {
      return;
    }
    e.preventDefault()
  }

  const { personalSettings: { hideGridButtons } } = usePersonalSettings()

  return (
    <div onKeyDown={handleKeyDown} className={`flex flex-col pt-2 gap-1 ${className ?? ''}`}>
      {!hideGridButtons && (
        <div className="flex flex-wrap gap-1 items-center">
          <UI.Button outline mini icon={Icon.PlusIcon} onClick={handleInsertRow}>行挿入(Enter)</UI.Button>
          <UI.Button outline mini icon={Icon.PlusIcon} onClick={handleInsertRowBelow}>下挿入(Ctrl + Enter)</UI.Button>
          <UI.Button outline mini icon={Icon.TrashIcon} onClick={handleDeleteRow}>行削除(Shift + Delete)</UI.Button>
          <div className="basis-2"></div>
          <UI.Button outline mini icon={Icon.ChevronDoubleLeftIcon} onClick={handleIndentDown}>インデント下げ(Shift + Tab)</UI.Button>
          <UI.Button outline mini icon={Icon.ChevronDoubleRightIcon} onClick={handleIndentUp}>インデント上げ(Tab)</UI.Button>
          <UI.Button outline mini icon={Icon.ChevronUpIcon} onClick={handleMoveUp}>上に移動(Alt + ↑)</UI.Button>
          <UI.Button outline mini icon={Icon.ChevronDownIcon} onClick={handleMoveDown}>下に移動(Alt + ↓)</UI.Button>
        </div>
      )}

      <UniqueConstraintsContext.Provider value={uniqueConstraintsContextValue}>
        <EG2.EditableGrid2
          {...editableGrid2Props}
          striped
          className="flex-1 w-full border-y border-r border-gray-300"
        />
      </UniqueConstraintsContext.Provider>
    </div>
  )
}

export default React.memo(DecsendantsGrid)

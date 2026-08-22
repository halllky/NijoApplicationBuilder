import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import * as EG2 from "@halllky/react-editable-grid"
import { UUID } from "uuidjs"
import {
  EditingProject,
  EditingMember,
  ATTR_DISPLAY_NAME,
  ATTR_CONSTANT_TYPE,
  ATTR_CONSTANT_VALUE,
  CONSTANT_TYPE_CHILD,
  CONSTANT_TYPE_STRING,
  CONSTANT_TYPE_INT,
  CONSTANT_TYPE_DECIMAL,
  CONSTANT_TYPE_TEMPLATE,
} from "../../backend"
import * as UI from '../../UI'

/** グリッドに新しい行を挿入するときの初期値。定数の要素にType属性は無いのでkindは常にunknown。 */
const newEmptyMember = (indent: number): EditingMember => ({
  uniqueId: UUID.generate(),
  indent,
  physicalName: '',
  type: { kind: 'unknown' },
  attributes: {},
  uniqueConstraints: [],
})

/**
 * 定数のルート集約1個分のエディタ
 */
export function SingleConstantEditor({ index, formMethods }: {
  index: number
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
}) {
  const { control, getValues, setValue, register } = formMethods

  // ルート要素（定数定義ブロック自体）の名前
  const rootNamePath = `constants.${index}.physicalName` as const
  const rootDisplayNamePath = `constants.${index}.attributes.${ATTR_DISPLAY_NAME}` as const
  const membersPath = `constants.${index}.members` as const

  // 削除処理
  const handleDeleteConstant = () => {
    if (!window.confirm("この定数定義ブロックを削除しますか？")) return
    const current = getValues("constants")
    const next = [...current]
    next.splice(index, 1)
    setValue("constants", next)
  }

  // グリッド設定
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
    const columns: EG2.EditableGrid2Column<any>[] = []

    // 定義名
    columns.push(helper.elementName(''))

    // 日本語名
    columns.push(helper.text('日本語名', `attributes.${ATTR_DISPLAY_NAME}`, {
      defaultWidth: 200,
    }))

    // 型
    columns.push(helper.dropdown('型', `attributes.${ATTR_CONSTANT_TYPE}`, [
      { value: CONSTANT_TYPE_CHILD, text: '入れ子' },
      { value: CONSTANT_TYPE_STRING, text: '単純文字列' },
      { value: CONSTANT_TYPE_TEMPLATE, text: 'テンプレート文字列' },
      { value: CONSTANT_TYPE_INT, text: '整数' },
      { value: CONSTANT_TYPE_DECIMAL, text: '実数' },
    ], {
      defaultWidth: 100,
    }))

    // 値
    columns.push(helper.text('値', `attributes.${ATTR_CONSTANT_VALUE}`, {
      defaultWidth: 300,
    }))

    // コメント
    columns.push(helper.text('コメント', 'comment', {
      defaultWidth: 400,
      mentionAvailable: true,
      wrap: true,
    }))

    return columns
  }, [index])

  // 行操作ハンドラ (StaticEnumGridから流用)
  const watchedFields = ReactHookForm.useWatch({ control, name: membersPath })

  const handleInsertRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) {
      insert(0, newEmptyMember(1))
    } else {
      const insertPosition = selectedRows[0].rowIndex
      const indent = watchedFields[insertPosition]?.indent ?? 1
      const count = selectedRows.length
      const insertRows = Array.from({ length: count }, () => newEmptyMember(indent))
      insert(insertPosition, insertRows)
    }
  }

  const handleInsertRowBelow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) {
      insert(0, newEmptyMember(1))
    } else {
      const insertPosition = selectedRows[selectedRows.length - 1].rowIndex + 1
      const indent = watchedFields[insertPosition]?.indent ?? 1
      const count = selectedRows.length
      const insertRows = Array.from({ length: count }, () => newEmptyMember(indent))
      insert(insertPosition, insertRows)
    }
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
    if (endRow >= watchedFields.length - 1) return

    move(endRow + 1, startRow)

    gridRef.current?.selectRow(selectedRows[0].rowIndex + 1, selectedRows[0].rowIndex + selectedRows.length)
  }

  const handleIndentDown = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows) return
    for (const row of selectedRows) {
      update(row.rowIndex, { ...row.row, indent: Math.max(1, row.row.indent - 1) })
    }
  }

  const handleIndentUp = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows) return
    for (const row of selectedRows) {
      update(row.rowIndex, { ...row.row, indent: row.row.indent + 1 })
    }
  }

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = e => {
    if (gridRef.current?.isEditing) return
    if (!e.ctrlKey && e.key === 'Enter') {
      e.preventDefault()
      handleInsertRow()
    } else if (e.ctrlKey && e.key === 'Enter') {
      e.preventDefault()
      handleInsertRowBelow()
    } else if (e.shiftKey && e.key === 'Delete') {
      e.preventDefault()
      handleDeleteRow()
    } else if (e.altKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      e.preventDefault()
      if (e.key === 'ArrowUp') handleMoveUp()
      else if (e.key === 'ArrowDown') handleMoveDown()
    } else if (e.shiftKey && e.key === 'Tab') {
      e.preventDefault()
      handleIndentDown()
    } else if (e.key === 'Tab') {
      e.preventDefault()
      handleIndentUp()
    }
  }

  return (
    <div className="p-4 flex flex-col gap-2">
      <div className="flex items-center gap-2">
        <UI.WordTextBox
          {...register(rootNamePath)}
          className="w-64 border px-1 font-bold"
          placeholder="定数グループ名 (MyConstants)"
        />
        <UI.WordTextBox
          {...register(rootDisplayNamePath)}
          className="w-64 border px-1"
          placeholder="日本語名 (定数)"
        />
        <div className="flex-1"></div>
        <UI.Button mini hideText icon={Icon.TrashIcon} onClick={handleDeleteConstant}>
          削除
        </UI.Button>
      </div>

      <div onKeyDown={handleKeyDown} className="flex flex-col gap-1">
        <div className="flex flex-wrap gap-1 items-center">
          <UI.Button mini outline icon={Icon.PlusIcon} onClick={handleInsertRow}>
            行挿入 (Enter)
          </UI.Button>
          <UI.Button mini outline icon={Icon.PlusIcon} onClick={handleInsertRowBelow}>
            下挿入 (Ctrl + Enter)
          </UI.Button>
          <UI.Button mini outline icon={Icon.TrashIcon} onClick={handleDeleteRow}>
            行削除 (Shift + Delete)
          </UI.Button>
          <div className="basis-2"></div>
          <UI.Button outline mini icon={Icon.ChevronDoubleLeftIcon} onClick={handleIndentDown}>インデント下げ (Shift + Tab)</UI.Button>
          <UI.Button outline mini icon={Icon.ChevronDoubleRightIcon} onClick={handleIndentUp}>インデント上げ (Tab)</UI.Button>
          <UI.Button outline mini icon={Icon.ChevronUpIcon} onClick={handleMoveUp}>上に移動 (Alt + ↑)</UI.Button>
          <UI.Button outline mini icon={Icon.ChevronDownIcon} onClick={handleMoveDown}>下に移動 (Alt + ↓)</UI.Button>
        </div>

        <EG2.EditableGrid2
          {...editableGrid2Props}
          striped
          className="w-full min-h-36 border border-gray-700"
        />
      </div>
    </div>
  )
}

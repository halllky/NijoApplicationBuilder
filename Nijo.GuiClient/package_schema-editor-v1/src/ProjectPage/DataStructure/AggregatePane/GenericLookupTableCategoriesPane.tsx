import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { UUID } from "uuidjs"
import {
  GeneratedProjectInGui,
  ATTR_DISPLAY_NAME,
  ATTR_IS_HARD_CODED_PRIMARY_KEY,
  GenericLookupTableCategory,
} from "../../../types"
import * as UI from "../../../UI"

/**
 * 汎用参照テーブルのカテゴリ定義編集ペイン
 * IsGenericLookupTable が True のルート集約の場合に表示される。
 */
function GenericLookupTableCategoriesPane(props: {
  selectedRootAggregateIndex: number
  formMethods: ReactHookForm.UseFormReturn<GeneratedProjectInGui>
  className?: string
}) {
  const { selectedRootAggregateIndex, formMethods: { control, getValues, setValue }, className } = props

  // このルート集約の UniqueId
  const rootUniqueId = ReactHookForm.useWatch({
    name: `xmlElementTrees.${selectedRootAggregateIndex}.xmlElements.0.uniqueId`,
    control,
  })

  // このルート集約の子要素（ValueMember等）のうち IsHardCodedPrimaryKey が True のもの
  const xmlElements = ReactHookForm.useWatch({
    name: `xmlElementTrees.${selectedRootAggregateIndex}.xmlElements`,
    control,
  }) ?? []

  const hardCodedKeyElements = React.useMemo(() => {
    return xmlElements.filter(el =>
      el.attributes?.[ATTR_IS_HARD_CODED_PRIMARY_KEY] === 'True'
    )
  }, [xmlElements])

  // genericLookupTableCategories のうちこのルート集約に対応するエントリのインデックスを特定する。
  // 存在しない場合は新規追加する。
  const categoriesListIndex = React.useMemo(() => {
    if (!rootUniqueId) return -1
    const list = getValues('genericLookupTableCategories') ?? []
    const idx = list.findIndex(entry => entry.for === rootUniqueId)
    if (idx !== -1) return idx
    // 存在しないので追加する
    const newList = [...list, { for: rootUniqueId, categories: [] }]
    setValue('genericLookupTableCategories', newList)
    return newList.length - 1
  }, [rootUniqueId, getValues, setValue])

  // useFieldArray: genericLookupTableCategories[categoriesListIndex].categories を管理する
  const fieldArrayName = `genericLookupTableCategories.${categoriesListIndex}.categories` as const

  const {
    fieldArrayReturn: { insert, remove, move },
    editableGrid2Props,
    gridRef,
  } = UI.useFieldArrayForEditableGrid2({
    name: fieldArrayName,
    control,
    getValues,
    setValue,
  }, helper => {
    const columns: EG2.EditableGrid2Column<ReactHookForm.FieldArrayWithId<GeneratedProjectInGui, typeof fieldArrayName, 'id'>>[] = []

    // 物理名（カテゴリのXML要素名）
    columns.push(helper.text('物理名', 'name' as ReactHookForm.Path<ReactHookForm.FieldArrayWithId<GeneratedProjectInGui, typeof fieldArrayName, 'id'>>, {
      defaultWidth: 180,
    }))

    // 表示名
    columns.push(helper.text('表示名', 'displayName' as ReactHookForm.Path<ReactHookForm.FieldArrayWithId<GeneratedProjectInGui, typeof fieldArrayName, 'id'>>, {
      defaultWidth: 200,
    }))

    // ハードコードされる主キーごとの列
    for (const keyEl of hardCodedKeyElements) {
      const keyUniqueId = keyEl.uniqueId
      const keyDisplayName = keyEl.attributes?.[ATTR_DISPLAY_NAME] || keyEl.localName || keyUniqueId
      const keyPath = `hardCodedKeyValues.${keyUniqueId}` as ReactHookForm.Path<ReactHookForm.FieldArrayWithId<GeneratedProjectInGui, typeof fieldArrayName, 'id'>>
      columns.push(helper.text(keyDisplayName, keyPath, {
        defaultWidth: 160,
      }))
    }

    return columns
  }, [hardCodedKeyElements, fieldArrayName])

  // ----- ハンドラ -----
  const handleInsertRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    const newRow: GenericLookupTableCategory = {
      name: '',
      displayName: '',
      hardCodedKeyValues: {},
    }
    if (!selectedRows || selectedRows.length === 0) {
      insert(0, newRow)
    } else {
      const insertPosition = selectedRows[0].rowIndex + 1
      insert(insertPosition, newRow)
    }
    window.setTimeout(() => {
      const rows = gridRef.current?.getSelectedRows()
      if (!rows || rows.length === 0) gridRef.current?.selectRow(0, 0)
    }, 10)
  }

  const handleDeleteRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const indexes = selectedRows.map(r => r.rowIndex)
    remove(indexes)
  }

  const handleMoveUp = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const startRow = selectedRows[0].rowIndex
    if (startRow <= 0) return
    const endRow = startRow + selectedRows.length - 1
    move(startRow - 1, endRow)
    gridRef.current?.selectRow(selectedRows[0].rowIndex - 1, selectedRows[0].rowIndex + selectedRows.length - 2)
  }

  const handleMoveDown = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const startRow = selectedRows[0].rowIndex
    const endRow = startRow + selectedRows.length - 1
    const total = (getValues(fieldArrayName) ?? []).length
    if (endRow >= total - 1) return
    move(endRow + 1, startRow)
    gridRef.current?.selectRow(selectedRows[0].rowIndex + 1, selectedRows[0].rowIndex + selectedRows.length)
  }

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = e => {
    if (gridRef.current?.isEditing) return
    if (!e.ctrlKey && e.key === 'Enter' && !e.defaultPrevented) {
      handleInsertRow()
    } else if (e.shiftKey && e.key === 'Delete') {
      handleDeleteRow()
    } else if (e.altKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown') && !e.defaultPrevented) {
      if (e.key === 'ArrowUp') handleMoveUp()
      else handleMoveDown()
    } else {
      return
    }
    e.preventDefault()
  }

  if (categoriesListIndex < 0) return null

  return (
    <div onKeyDown={handleKeyDown} className={`flex flex-col gap-1 ${className ?? ''}`}>
      <div className="flex items-center gap-1 px-1 pt-2">
        <span className="text-sm font-semibold text-gray-700">カテゴリ定義</span>
        {hardCodedKeyElements.length === 0 && (
          <span className="text-xs text-amber-600">
            （IsHardCodedPrimaryKey が指定された主キーがありません）
          </span>
        )}
      </div>
      <div className="flex flex-wrap gap-1 items-center px-1">
        <UI.Button outline mini icon={Icon.PlusIcon} onClick={handleInsertRow}>行挿入(Enter)</UI.Button>
        <UI.Button outline mini icon={Icon.TrashIcon} onClick={handleDeleteRow}>行削除(Shift + Delete)</UI.Button>
        <UI.Button outline mini icon={Icon.ChevronUpIcon} onClick={handleMoveUp}>上に移動(Alt + ↑)</UI.Button>
        <UI.Button outline mini icon={Icon.ChevronDownIcon} onClick={handleMoveDown}>下に移動(Alt + ↓)</UI.Button>
      </div>
      <EG2.EditableGrid2
        {...editableGrid2Props}
        striped
        className="flex-1 w-full border-y border-r border-gray-300"
      />
    </div>
  )
}

export default React.memo(GenericLookupTableCategoriesPane)

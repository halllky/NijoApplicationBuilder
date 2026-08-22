import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import * as EG2 from "@halllky/react-editable-grid"
import { UUID } from "uuidjs"
import {
  EditingProject,
  ATTR_DISPLAY_NAME,
  ATTR_IS_HARD_CODED_PRIMARY_KEY,
  EditingGenericLookupTableCategory,
} from "../../../backend"
import * as UI from "../../../UI"
import { RootAggregateLocation } from "../../rootAggregateLocation"

type GridRow = EditingGenericLookupTableCategory & { id: string }

/**
 * 汎用参照テーブルのカテゴリ定義編集ペイン
 * IsGenericLookupTable が True のルート集約の場合に表示される。
 */
function GenericLookupTableCategoriesPane(props: {
  rootLocation: RootAggregateLocation
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
  className?: string
}) {
  const { rootLocation, formMethods: { control, getValues, setValue }, className } = props
  const rootPath = `${rootLocation.list}.${rootLocation.index}` as const
  const categoriesPath = `${rootPath}.genericLookupTable.categories` as const

  // このルート集約の genericLookupTable
  const genericLookupTable = ReactHookForm.useWatch({ name: `${rootPath}.genericLookupTable`, control })

  // このルート集約の子要素（ValueMember等）のうち IsHardCodedPrimaryKey が True のもの
  const members = ReactHookForm.useWatch({ name: `${rootPath}.members`, control }) ?? []

  const hardCodedKeyElements = React.useMemo(() => {
    return members.filter(m => m.attributes?.[ATTR_IS_HARD_CODED_PRIMARY_KEY] === true)
  }, [members])

  // このペインが表示された時点で genericLookupTable が未設定なら空で初期化する。
  // このペイン自体、IsGenericLookupTable が True の場合にのみ表示される（AggregatePane側で制御）。
  React.useEffect(() => {
    if (getValues(`${rootPath}.genericLookupTable`) == null) {
      setValue(`${rootPath}.genericLookupTable`, { categories: [] }, { shouldDirty: false })
    }
  }, [rootPath, getValues, setValue])

  const {
    fieldArrayReturn: { insert, remove, move },
    editableGrid2Props,
    gridRef,
  } = UI.useFieldArrayForEditableGrid2({
    name: categoriesPath,
    control,
    getValues,
    setValue,
  }, helper => {
    const columns: EG2.EditableGrid2Column<GridRow>[] = []

    // 物理名（カテゴリのXML要素名）
    columns.push(helper.text('物理名', 'name', {
      defaultWidth: 180,
    }))

    // 表示名
    columns.push(helper.text('表示名', 'displayName', {
      defaultWidth: 200,
    }))

    // ハードコードされる主キーごとの列
    for (const keyEl of hardCodedKeyElements) {
      const keyUniqueId = keyEl.uniqueId
      const keyDisplayName = keyEl.attributes?.[ATTR_DISPLAY_NAME] || keyEl.physicalName || keyUniqueId
      const keyPath = `hardCodedKeyValues.${keyUniqueId}` as const
      columns.push(helper.text(String(keyDisplayName), keyPath, {
        defaultWidth: 160,
      }))
    }

    return columns
  }, [hardCodedKeyElements])

  // ----- ハンドラ -----
  const handleInsertRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    const newRow: EditingGenericLookupTableCategory = {
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
    const total = (getValues(categoriesPath) ?? []).length
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

  if (!genericLookupTable) return null

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

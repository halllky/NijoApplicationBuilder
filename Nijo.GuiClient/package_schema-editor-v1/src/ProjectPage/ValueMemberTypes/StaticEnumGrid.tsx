import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import * as EG2 from "@halllky/react-editable-grid"
import { UUID } from "uuidjs"
import { EditingProject, EditingRootAggregate, EditingMember, MODEL_STATIC_ENUM } from "../../backend"
import * as UI from '../../UI'

/** グリッドに新しい行を挿入するときの初期値。静的区分の値にType属性は無いのでkindは常にunknown。 */
const newEmptyMember = (indent: number): EditingMember => ({
  uniqueId: UUID.generate(),
  indent,
  physicalName: '',
  type: { kind: 'unknown' },
  attributes: {},
  uniqueConstraints: [],
})

/**
 * 静的区分定義グリッド
 *
 * 複数の静的区分定義をリスト形式で表示し、それぞれを編集可能にする。
 */
export default function StaticEnumGrid(props: {
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
}) {
  const { control, setValue, getValues } = props.formMethods
  const staticEnums = ReactHookForm.useWatch({
    control,
    name: "staticEnums"
  }) ?? []

  const handleAddEnum = () => {
    const newEnum: EditingRootAggregate = {
      uniqueId: UUID.generate(),
      physicalName: "",
      model: MODEL_STATIC_ENUM,
      attributes: {},
      uniqueConstraints: [],
      members: [],
    }

    // 末尾に追加
    const current = getValues("staticEnums") ?? []
    setValue("staticEnums", [...current, newEnum])
  }

  return (
    <div className="flex flex-col gap-2 py-2">
      {staticEnums.map((staticEnum, index) => (
        <div key={staticEnum.uniqueId ?? index} id={`enum-def-${staticEnum.uniqueId}`}>
          <SingleEnumEditor
            index={index}
            formMethods={props.formMethods}
          />
        </div>
      ))}
      <div>
        <UI.Button icon={Icon.PlusIcon} onClick={handleAddEnum}>
          新しい区分を追加
        </UI.Button>
      </div>
    </div>
  )
}

function SingleEnumEditor({ index, formMethods }: {
  index: number
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
}) {
  const { register, control, getValues, setValue } = formMethods

  // ルート要素（区分定義自体）の名前
  const rootNamePath = `staticEnums.${index}.physicalName` as const
  const membersPath = `staticEnums.${index}.members` as const

  // 削除処理
  const handleDeleteEnum = () => {
    if (!window.confirm("この区分定義を削除しますか？")) return
    const current = getValues("staticEnums")
    const next = [...current]
    next.splice(index, 1)
    setValue("staticEnums", next)
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

    // 名前
    columns.push(helper.text('値', 'physicalName', {
      defaultWidth: 220,
      isFixed: true,
      renderBody: ({ context }) => {
        const value = ReactHookForm.useWatch({
          control,
          name: `${membersPath}.${context.row.index}.physicalName`,
        })
        return (
          <div className="px-1 w-full truncate">
            {value}
          </div>
        )
      }
    }))

    // Key
    columns.push(helper.text('C#列挙体キー', `attributes.key`, {
      defaultWidth: 100,
    }))

    // コメント
    columns.push(helper.text('コメント', 'comment', {
      defaultWidth: 400,
      mentionAvailable: true,
      wrap: true,
    }))

    // その他の属性があればここに追加（Enum値に属性がある場合）

    return columns
  }, [index])

  // 行操作ハンドラ (DescendantsGridから流用)
  const watchedFields = ReactHookForm.useWatch({ control, name: membersPath })

  const handleInsertRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) {
      insert(0, newEmptyMember(1))
    } else {
      const insertPosition = selectedRows[0].rowIndex
      const indent = watchedFields[insertPosition]?.indent ?? 1
      insert(insertPosition, newEmptyMember(indent))
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

  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = e => {
    if (gridRef.current?.isEditing) return
    if (e.key === 'Enter') {
      e.preventDefault()
      handleInsertRow()
    } else if (e.shiftKey && e.key === 'Delete') {
      e.preventDefault()
      handleDeleteRow()
    } else if (e.altKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      e.preventDefault()
      if (e.key === 'ArrowUp') handleMoveUp()
      else if (e.key === 'ArrowDown') handleMoveDown()
    }
  }

  return (
    <div className="py-4 flex flex-col gap-1">
      <div className="flex justify-start items-center gap-1">
        <UI.WordTextBox
          {...register(rootNamePath)}
          className="basis-80 font-bold border px-1"
          placeholder="区分名を入力"
        />
        <UI.Button mini icon={Icon.TrashIcon} onClick={handleDeleteEnum}>
          削除
        </UI.Button>
      </div>

      <div onKeyDown={handleKeyDown} className="flex flex-col gap-1">
        <div className="flex gap-1">
          <UI.Button mini outline icon={Icon.PlusIcon} onClick={handleInsertRow}>
            行追加 (Enter)
          </UI.Button>
          <UI.Button mini outline icon={Icon.TrashIcon} onClick={handleDeleteRow}>
            行削除 (Shift+Delete)
          </UI.Button>
          <div className="basis-2"></div>
          <UI.Button outline mini icon={Icon.ChevronUpIcon} onClick={handleMoveUp}>上へ (Alt + ↑)</UI.Button>
          <UI.Button outline mini icon={Icon.ChevronDownIcon} onClick={handleMoveDown}>下へ (Alt + ↓)</UI.Button>
        </div>

        <EG2.EditableGrid2
          {...editableGrid2Props}
          className="w-full border border-gray-700"
        />
      </div>
    </div>
  )
}

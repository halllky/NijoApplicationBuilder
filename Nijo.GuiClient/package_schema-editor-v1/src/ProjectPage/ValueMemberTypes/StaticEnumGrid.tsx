import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { UUID } from "uuidjs"
import { GeneratedProjectInGui, ATTR_TYPE, TYPE_STATIC_ENUM_MODEL, XmlElementAttributeName, XmlElementItem } from "../../types"
import * as UI from '../../UI'

type FormType = GeneratedProjectInGui

/**
 * 静的区分定義グリッド
 *
 * 複数の静的区分定義をリスト形式で表示し、それぞれを編集可能にする。
 */
export default function StaticEnumGrid(props: {
  formMethods: ReactHookForm.UseFormReturn<FormType>
}) {
  const { control, setValue, getValues } = props.formMethods
  const xmlElementTrees = ReactHookForm.useWatch({
    control,
    name: "xmlElementTrees"
  }) ?? []

  // 静的区分のインデックスのみを抽出
  const enumIndexes = React.useMemo(() => {
    return xmlElementTrees
      .map((tree, index) => ({ tree, index }))
      .filter(({ tree }) => tree.xmlElements?.[0]?.attributes?.[ATTR_TYPE] === TYPE_STATIC_ENUM_MODEL)
      .map(({ index }) => index)
  }, [xmlElementTrees])

  const handleAddEnum = () => {
    const newEnum: XmlElementItem = {
      uniqueId: UUID.generate(),
      indent: 0,
      localName: "",
      value: undefined,
      attributes: { [ATTR_TYPE]: TYPE_STATIC_ENUM_MODEL },
      comment: undefined,
    }
    const newTree = { xmlElements: [newEnum] }

    // 末尾に追加
    const current = getValues("xmlElementTrees") ?? []
    setValue("xmlElementTrees", [...current, newTree])
  }

  return (
    <div className="flex flex-col gap-2 py-2">
      {enumIndexes.map(index => {
        const uniqueId = xmlElementTrees[index].xmlElements[0]?.uniqueId
        return (
          <div key={uniqueId ?? index} id={`enum-def-${uniqueId}`}>
            <SingleEnumEditor
              index={index}
              formMethods={props.formMethods}
            />
          </div>
        )
      })}
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
  formMethods: ReactHookForm.UseFormReturn<FormType>
}) {
  const { register, control, getValues, setValue } = formMethods

  // ルート要素（区分定義自体）の名前
  const rootNamePath = `xmlElementTrees.${index}.xmlElements.0.localName` as const

  // 削除処理
  const handleDeleteEnum = () => {
    if (!window.confirm("この区分定義を削除しますか？")) return
    const current = getValues("xmlElementTrees")
    // 指定インデックスを除外 (インデックスがずれるのでfilterを使う)
    // filterを使う場合、indexはmap時点のものなので、再計算が必要か、もしくはspliceを使う。
    // ここでは安全のため、現在のvaluesを取得してspliceする。
    const next = [...current]
    next.splice(index, 1) // 正しいインデックスか？ 親コンポーネントが渡すindexは初期描画時のものか？
    // 親のenumIndexesはxmlElementTreesに依存して再計算されるので、indexは常に正しいはず。
    setValue("xmlElementTrees", next)
  }

  // グリッド設定
  const {
    fieldArrayReturn: { insert, remove, move, update },
    editableGrid2Props,
    gridRef,
  } = UI.useFieldArrayForEditableGrid2({
    name: `xmlElementTrees.${index}.xmlElements`,
    skipFirstRow: true, // 先頭行はルート要素なのでグリッドには含めない
    control,
    getValues,
    setValue,
  }, helper => {
    const columns: EG2.EditableGrid2Column<ReactHookForm.FieldArrayWithId<GeneratedProjectInGui, `xmlElementTrees.${number}.xmlElements`>>[] = []

    // 名前
    columns.push(helper.text('値', 'localName', {
      defaultWidth: 220,
      isFixed: true,
      renderBody: ({ context }) => {
        const indent = context.row.original.indent
        return (
          <div className="px-1 flex-1 inline-flex text-left truncate">
            {/* Indent */}
            {Array.from({ length: Math.max(0, indent - 1) }).map((_, i) => (
              <div key={i} className="basis-[20px] min-w-[20px] relative leading-none" />
            ))}
            <HelperRHFTextCell
              control={control}
              name={`xmlElementTrees.${index}.xmlElements.${context.row.index + 1}.localName`}
            />
          </div>
        )
      }
    }))

    // Key
    columns.push(helper.text('C#列挙体キー', `attributes.${"key" as XmlElementAttributeName}`, {
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
  const watchedFields = ReactHookForm.useWatch({ control, name: `xmlElementTrees.${index}.xmlElements` })

  const handleInsertRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) {
      insert(1, { uniqueId: UUID.generate(), indent: 1, localName: '', attributes: {} })
    } else {
      const insertPosition = selectedRows[0].rowIndex + 1
      const indent = watchedFields[insertPosition]?.indent ?? 1
      insert(insertPosition, { uniqueId: UUID.generate(), indent, localName: '', attributes: {} })
    }
  }

  const handleDeleteRow = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const removedIndexes = selectedRows.map(row => row.rowIndex + 1)
    remove(removedIndexes)
  }

  const handleMoveUp = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const startRow = selectedRows[0].rowIndex + 1 // Field Index
    const endRow = startRow + selectedRows.length - 1
    if (startRow <= 1) return // Can't move above root (index 0)

    move(startRow - 1, endRow)

    // Restore selection
    gridRef.current?.selectRow(selectedRows[0].rowIndex - 1, selectedRows[0].rowIndex + selectedRows.length - 2)
  }

  const handleMoveDown = () => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows || selectedRows.length === 0) return
    const startRow = selectedRows[0].rowIndex + 1
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
    <div className="py-4 flex flex-col gap-2">
      <div className="flex items-center gap-2">
        <UI.WordTextBox
          {...register(rootNamePath)}
          className="basis-80 font-bold border px-1"
          placeholder="区分名を入力"
        />
        <UI.Button mini hideText icon={Icon.TrashIcon} onClick={handleDeleteEnum}>
          区分自体の削除
        </UI.Button>
        <div className="flex-1" />
      </div>

      <div onKeyDown={handleKeyDown} className="flex flex-col gap-1">
        <div className="flex gap-1">
          <UI.Button mini outline icon={Icon.PlusIcon} onClick={handleInsertRow}>
            値を追加 (Enter)
          </UI.Button>
          <UI.Button mini outline icon={Icon.TrashIcon} onClick={handleDeleteRow}>
            値を削除 (Shift+Delete)
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

const HelperRHFTextCell = ({ control, name }: { control: any, name: string }) => {
  const value = ReactHookForm.useWatch({ control, name })
  return <>{value}</>
}

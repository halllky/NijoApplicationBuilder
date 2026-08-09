import React from "react"
import * as ReactHookForm from "react-hook-form"
import { GeneratedProjectInGui, ATTR_UNIQUE_CONSTRAINTS, asTree, XmlElementItem } from "../../../types"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { TextCellEditor } from "../../../UI"

/**
 * ユニーク制約の列定義を提供するフック。
 */
export function useUniqueConstraintsColumns(
  control: ReactHookForm.Control<GeneratedProjectInGui>,
  getValues: ReactHookForm.UseFormGetValues<GeneratedProjectInGui>,
  setValue: ReactHookForm.UseFormSetValue<GeneratedProjectInGui>,
  selectedRootAggregateIndex: number,
  skipFirstRow: boolean,
) {

  // ルート、child, children すべて監視
  const elements = ReactHookForm.useWatch({ name: `xmlElementTrees.${selectedRootAggregateIndex}.xmlElements`, control }) ?? []

  const treeHelper = React.useMemo(() => asTree(elements, el => el.uniqueId), [elements])

  const uniqueConstraintsByParent = React.useMemo(() => {
    const map = new Map<string, string[][]>()
    for (const el of elements) {
      const raw = el.attributes?.[ATTR_UNIQUE_CONSTRAINTS] as string | undefined
      map.set(el.uniqueId, parseUniqueConstraints(raw))
    }
    return map
  }, [elements])

  const uniqueConstraintsMaxLength = React.useMemo(() => {
    let max = 0
    uniqueConstraintsByParent.forEach(constraints => {
      if (constraints.length > max) max = constraints.length
    })
    return max
  }, [uniqueConstraintsByParent])

  const contextValue = React.useMemo(() => ({ elements, treeHelper, uniqueConstraintsByParent }), [elements, treeHelper, uniqueConstraintsByParent])

  // 列定義。
  // この変数が変わるとグリッド全体の列定義が更新されてしまうため、
  // ユニーク制約の数が変わったとき以外は同じオブジェクトを返すようにする。
  type GridRow = ReactHookForm.FieldArrayWithId<GeneratedProjectInGui, `xmlElementTrees.${number}.xmlElements`, "id">
  const uniqueConstraintColumns = React.useMemo((): EG2.EditableGrid2Column<GridRow> => {
    const columns: EG2.EditableGrid2LeafColumn<GridRow>[] = []
    const constraintCount = uniqueConstraintsMaxLength + 1

    for (let i = 0; i < constraintCount; i++) {
      columns.push({
        columnId: `unique-constraint-${i}`,
        editor: TextCellEditor,
        renderHeader: () => (
          <div className="px-1 py-px truncate text-sm text-gray-700">
            {i + 1}
          </div>
        ),
        renderBody: ({ context }) => (
          <UniqueConstraintCell
            rowIndex={context.row.index}
            columnIndex={i}
            skipFirstRow={skipFirstRow}
          />
        ),
        getValueForEditor: ({ row }) => {
          const currentElements = getValues(`xmlElementTrees.${selectedRootAggregateIndex}.xmlElements`) ?? []
          if (!currentElements.length) return ''

          const rowIndex = currentElements.findIndex(el => el.uniqueId === row.uniqueId)
          if (rowIndex < 0) return ''

          const treeHelperForEditor = asTree(currentElements, el => el.uniqueId)
          const parentOrRoot = treeHelperForEditor.getParent(currentElements[rowIndex]) ?? currentElements[0]
          const parentConstraints = parseUniqueConstraints(parentOrRoot.attributes?.[ATTR_UNIQUE_CONSTRAINTS] as string | undefined)
          const indexInConstraint = parentConstraints[i]?.indexOf(row.uniqueId) ?? -1
          return indexInConstraint >= 0 ? String(indexInConstraint + 1) : ''
        },
        setValueFromEditor: ({ row, value }) => {
          const currentElements = getValues(`xmlElementTrees.${selectedRootAggregateIndex}.xmlElements`) ?? []
          if (!currentElements.length) return

          const rowIndex = currentElements.findIndex(el => el.uniqueId === row.uniqueId)
          if (rowIndex < 0) return

          const treeHelperForEditor = asTree(currentElements, el => el.uniqueId)
          const parentOrRoot = treeHelperForEditor.getParent(currentElements[rowIndex]) ?? currentElements[0]
          const parentIndex = Math.max(0, currentElements.indexOf(parentOrRoot))

          let numValue = parseInt(value, 10)
          if (value.trim() === '') {
            numValue = 0 // 空文字が入力されたらその行をユニーク制約から外す
          } else if (isNaN(numValue)) {
            numValue = Number.MAX_SAFE_INTEGER // 文字列が入力されたらとりあえず一番後ろに追加する挙動にする
          }

          const currentRaw = getValues(`xmlElementTrees.${selectedRootAggregateIndex}.xmlElements.${parentIndex}.attributes.${ATTR_UNIQUE_CONSTRAINTS}`)
          const currentConstraints = parseUniqueConstraints(currentRaw)

          if (!currentConstraints[i]) currentConstraints[i] = []

          currentConstraints[i] = currentConstraints[i].filter(id => id !== row.uniqueId)

          if (numValue > 0) {
            currentConstraints[i].splice(numValue - 1, 0, row.uniqueId)
          }

          while (currentConstraints.length > 0 && currentConstraints[currentConstraints.length - 1].length === 0) {
            currentConstraints.pop()
          }

          const newRaw = serializeUniqueConstraints(currentConstraints)
          setValue(
            `xmlElementTrees.${selectedRootAggregateIndex}.xmlElements.${parentIndex}.attributes.${ATTR_UNIQUE_CONSTRAINTS}` as ReactHookForm.FieldPath<GeneratedProjectInGui>,
            newRaw,
            { shouldDirty: true })
        },
        defaultWidth: i === uniqueConstraintsMaxLength ? 112 : 24,
      })
    }

    return {
      renderHeader: () => (
        <div className="px-1 py-px truncate text-sm text-gray-700">
          ユニーク制約
        </div>
      ),
      columns,
    } satisfies EG2.EditableGrid2GroupColumn<GridRow>
  }, [uniqueConstraintsMaxLength, getValues, setValue, selectedRootAggregateIndex, skipFirstRow])

  return {
    /**
     * ユニーク制約の列定義。
     * この関数は列定義の依存配列に入れて使用してください。
     * ユニーク制約の数が変わったときに列定義も更新されるようになります。
     */
    uniqueConstraintColumns,
    contextValue,
  }
}

type UniqueConstraintsContextValue = {
  elements: XmlElementItem[]
  treeHelper: ReturnType<typeof asTree<XmlElementItem, string>>
  uniqueConstraintsByParent: Map<string, string[][]>
}
/**
 * ユニーク制約の情報を提供するReact Context。
 * ユニーク制約はグリッド内の他の行の値に依存するため、
 * 適切に再レンダリングをかけるようにするためにContextを使用している。
 */
export const UniqueConstraintsContext = React.createContext<UniqueConstraintsContextValue>({
  elements: [],
  treeHelper: asTree<XmlElementItem, string>([], el => el.uniqueId),
  uniqueConstraintsByParent: new Map(),
})


/**
 * ユニーク制約のセルのレンダラー
 */
function UniqueConstraintCell({ rowIndex, columnIndex, skipFirstRow }: {
  rowIndex: number
  columnIndex: number
  skipFirstRow: boolean
}) {
  const { elements, treeHelper, uniqueConstraintsByParent } = React.useContext(UniqueConstraintsContext)
  const actualRowIndex = skipFirstRow ? rowIndex + 1 : rowIndex
  const row = elements[actualRowIndex]
  if (!row) return <div className="w-full px-1 truncate" />

  const parent = treeHelper.getParent(row) ?? elements[0]
  const parentConstraints = parent ? uniqueConstraintsByParent.get(parent.uniqueId) ?? [] : []
  const indexInConstraint = parentConstraints[columnIndex]?.indexOf(row.uniqueId) ?? -1
  const value = indexInConstraint >= 0 ? String(indexInConstraint + 1) : ''
  return (
    <div className="w-full px-1 truncate">
      {value}
    </div>
  )
}

function parseUniqueConstraints(raw: string | undefined): string[][] {
  if (!raw) return []
  const parts = raw.split(';').map(s => s.trim())
  if (parts.length > 0 && parts[parts.length - 1] === '') {
    parts.pop()
  }
  return parts.map(s => s.split(',').map(id => id.trim()).filter(id => id.length > 0))
}

function serializeUniqueConstraints(constraints: string[][]): string {
  if (constraints.length === 0) return ''
  return constraints.map(c => c.join(',')).join(';') + ';'
}

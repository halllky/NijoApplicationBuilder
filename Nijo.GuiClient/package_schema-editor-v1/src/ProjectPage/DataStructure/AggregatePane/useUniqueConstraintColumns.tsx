import React from "react"
import * as ReactHookForm from "react-hook-form"
import { EditingProject, EditingMember, EditingUniqueConstraint } from "../../../backend"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { TextCellEditor } from "../../../UI"
import { asTree, TreeHelper } from "../../../asTree"
import { RootAggregateLocation } from "../../rootAggregateLocation"

type GridRow = EditingMember & { id: string }

/**
 * ユニーク制約の列定義を提供するフック。
 * ユニーク制約は「ルート集約、または child/children メンバー」が、自身の直属メンバーの組み合わせに対して持つ。
 * セル1個は「この行が、対象コンテナ（直近の親、無ければルート自身）の何番目の制約グループの何番目のメンバーか」を表す。
 */
export function useUniqueConstraintsColumns(
  control: ReactHookForm.Control<EditingProject>,
  getValues: ReactHookForm.UseFormGetValues<EditingProject>,
  setValue: ReactHookForm.UseFormSetValue<EditingProject>,
  rootLocation: RootAggregateLocation,
) {
  const rootPath = `${rootLocation.list}.${rootLocation.index}` as const
  const membersPath = `${rootPath}.members` as const

  // ルート、メンバーすべて監視
  const root = ReactHookForm.useWatch({ name: rootPath, control })
  const members = ReactHookForm.useWatch({ name: membersPath, control }) ?? []

  const treeHelper = React.useMemo(() => asTree(members, m => m.uniqueId), [members])

  // 各コンテナ（ルート or メンバー）のUniqueId → uniqueConstraints のマップ
  const uniqueConstraintsByContainerId = React.useMemo(() => {
    const map = new Map<string, EditingUniqueConstraint[]>()
    if (root) map.set(root.uniqueId, root.uniqueConstraints ?? [])
    for (const m of members) map.set(m.uniqueId, m.uniqueConstraints ?? [])
    return map
  }, [root, members])

  const uniqueConstraintsMaxLength = React.useMemo(() => {
    let max = 0
    uniqueConstraintsByContainerId.forEach(constraints => {
      if (constraints.length > max) max = constraints.length
    })
    return max
  }, [uniqueConstraintsByContainerId])

  const contextValue = React.useMemo((): UniqueConstraintsContextValue => ({
    members,
    treeHelper,
    rootUniqueId: root?.uniqueId,
    uniqueConstraintsByContainerId,
  }), [members, treeHelper, root?.uniqueId, uniqueConstraintsByContainerId])

  // 列定義。
  // この変数が変わるとグリッド全体の列定義が更新されてしまうため、
  // ユニーク制約の数が変わったとき以外は同じオブジェクトを返すようにする。
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
          />
        ),
        getValueForEditor: ({ row }) => {
          const container = findContainer(getValues, rootPath, membersPath, row.uniqueId)
          if (!container) return ''
          const constraints = container.uniqueConstraints ?? []
          const indexInConstraint = constraints[i]?.memberUniqueIds.indexOf(row.uniqueId) ?? -1
          return indexInConstraint >= 0 ? String(indexInConstraint + 1) : ''
        },
        setValueFromEditor: ({ row, value }) => {
          const container = findContainer(getValues, rootPath, membersPath, row.uniqueId)
          if (!container) return

          let numValue = parseInt(value, 10)
          if (value.trim() === '') {
            numValue = 0 // 空文字が入力されたらその行をユニーク制約から外す
          } else if (isNaN(numValue)) {
            numValue = Number.MAX_SAFE_INTEGER // 文字列が入力されたらとりあえず一番後ろに追加する挙動にする
          }

          const currentConstraints = (container.uniqueConstraints ?? []).map(c => ({
            memberUniqueIds: c.memberUniqueIds.filter(id => id !== row.uniqueId),
          }))
          if (!currentConstraints[i]) currentConstraints[i] = { memberUniqueIds: [] }
          if (numValue > 0) currentConstraints[i].memberUniqueIds.splice(numValue - 1, 0, row.uniqueId)

          while (currentConstraints.length > 0 && currentConstraints[currentConstraints.length - 1].memberUniqueIds.length === 0) {
            currentConstraints.pop()
          }

          if (container.kind === 'root') {
            setValue(`${rootPath}.uniqueConstraints`, currentConstraints, { shouldDirty: true })
          } else {
            setValue(`${membersPath}.${container.memberIndex}.uniqueConstraints`, currentConstraints, { shouldDirty: true })
          }
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
  }, [uniqueConstraintsMaxLength, getValues, setValue, rootPath, membersPath])

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

/**
 * 指定した行（メンバー）が属するコンテナ（直近の親メンバー、無ければルート自身）を、
 * その時点の最新の値から解決する。
 */
function findContainer(
  getValues: ReactHookForm.UseFormGetValues<EditingProject>,
  rootPath: `${RootAggregateLocation['list']}.${number}`,
  membersPath: `${RootAggregateLocation['list']}.${number}.members`,
  memberUniqueId: string,
): { kind: 'root', uniqueConstraints: EditingUniqueConstraint[] } | { kind: 'member', memberIndex: number, uniqueConstraints: EditingUniqueConstraint[] } | undefined {
  const currentRoot = getValues(rootPath)
  const currentMembers = getValues(membersPath) ?? []
  if (!currentRoot) return undefined

  const rowIndex = currentMembers.findIndex(m => m.uniqueId === memberUniqueId)
  if (rowIndex < 0) return undefined

  const parent = asTree(currentMembers, m => m.uniqueId).getParent(currentMembers[rowIndex])
  if (!parent) return { kind: 'root', uniqueConstraints: currentRoot.uniqueConstraints ?? [] }

  const parentIndex = currentMembers.findIndex(m => m.uniqueId === parent.uniqueId)
  return { kind: 'member', memberIndex: parentIndex, uniqueConstraints: parent.uniqueConstraints ?? [] }
}

type UniqueConstraintsContextValue = {
  members: EditingMember[]
  treeHelper: TreeHelper<EditingMember, string>
  rootUniqueId: string | undefined
  uniqueConstraintsByContainerId: Map<string, EditingUniqueConstraint[]>
}
/**
 * ユニーク制約の情報を提供するReact Context。
 * ユニーク制約はグリッド内の他の行の値に依存するため、
 * 適切に再レンダリングをかけるようにするためにContextを使用している。
 */
export const UniqueConstraintsContext = React.createContext<UniqueConstraintsContextValue>({
  members: [],
  treeHelper: asTree<EditingMember, string>([], m => m.uniqueId),
  rootUniqueId: undefined,
  uniqueConstraintsByContainerId: new Map(),
})


/**
 * ユニーク制約のセルのレンダラー
 */
function UniqueConstraintCell({ rowIndex, columnIndex }: {
  rowIndex: number
  columnIndex: number
}) {
  const { members, treeHelper, rootUniqueId, uniqueConstraintsByContainerId } = React.useContext(UniqueConstraintsContext)
  const row = members[rowIndex]
  if (!row) return <div className="w-full px-1 truncate" />

  const parent = treeHelper.getParent(row)
  const containerId = parent?.uniqueId ?? rootUniqueId
  const containerConstraints = containerId ? uniqueConstraintsByContainerId.get(containerId) ?? [] : []
  const indexInConstraint = containerConstraints[columnIndex]?.memberUniqueIds.indexOf(row.uniqueId) ?? -1
  const value = indexInConstraint >= 0 ? String(indexInConstraint + 1) : ''
  return (
    <div className="w-full px-1 truncate">
      {value}
    </div>
  )
}

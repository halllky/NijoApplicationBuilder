import React from "react"
import useEvent from "react-use-event-hook";
import { AppSettingsForDisplay, AppSettingsForSave, Perspective } from "../型つきドキュメント/types";
import { UUID } from "uuidjs";
import * as Input from "../input"
import * as Layout from "../layout"
import * as Icon from "@heroicons/react/24/solid"
import { useDialogContext } from "../layout";
import { useForm, UseFormSetValue, useWatch } from "react-hook-form";

/** ドキュメント種類編集グリッドの行 */
type GridRow = { entityTypeId: string, entityTypeName: string | undefined }

/** アプリケーション設定編集ダイアログ */
export const useAppSettingsEditDialog = () => {
  const dialogContext = useDialogContext()

  return React.useCallback((
    defaultValues: AppSettingsForSave,
    entityTypeList: AppSettingsForDisplay['entityTypeList'],
    onSave: (
      values: AppSettingsForSave,
      newPerspectives: Perspective[],
      entityNames: Record<string, string>
    ) => Promise<void>
  ) => {
    dialogContext.pushDialog({
      title: '設定',
      className: "max-w-lg max-h-[80vh]",
    }, ({ closeDialog }) => (
      <AppSettingsEditDialog
        defaultValues={defaultValues}
        entityTypeList={entityTypeList}
        onSave={onSave}
        closeDialog={closeDialog}
      />
    ))
  }, [])
}

const AppSettingsEditDialog = ({
  defaultValues,
  entityTypeList,
  onSave,
  closeDialog,
}: {
  defaultValues: AppSettingsForSave,
  entityTypeList: AppSettingsForDisplay['entityTypeList'],
  onSave: (
    values: AppSettingsForSave,
    newPerspectives: Perspective[],
    entityNames: Record<string, string>
  ) => Promise<void>,
  closeDialog: () => void,
}) => {

  const { control, getValues, setValue } = useForm<AppSettingsForSave>({
    defaultValues,
  })

  // 新規追加ドキュメント種類の一覧
  const [newPerspectives, setNewPerspectives] = React.useState<Perspective[]>([])

  // 編集されたエンティティ名
  const [editedEntityNames, setEditedEntityNames] = React.useState<Record<string, string>>({})

  // エンティティ種類（種類名とトップページでの順番を編集可能）
  const entityOrder = useWatch({ control, name: "entityTypeOrder" })
  const entityTypesGridRows: GridRow[] = React.useMemo(() => {
    return entityOrder.map(entityTypeId => {
      const entityTypeName = editedEntityNames[entityTypeId]
        ?? newPerspectives.find(x => x.perspectiveId === entityTypeId)?.name
        ?? entityTypeList.find(entityType => entityType.entityTypeId === entityTypeId)?.entityTypeName
      return { entityTypeId, entityTypeName }
    })
  }, [entityOrder, newPerspectives, entityTypeList, editedEntityNames])

  const getEntityTypesGridColumns: Layout.GetColumnDefsFunction<GridRow> = React.useCallback(cellType => [
    cellType.text('entityTypeName', '種類名', { defaultWidth: 280 }),
    cellType.other('並び替え', {
      defaultWidth: 100,
      renderCell: renderEntityTypeOrderColumn(entityOrder, setValue),
    }),
  ], [entityOrder, setValue])

  // 新しいPerspectiveを追加する処理
  const handleCreateNewPerspective = useEvent(async () => {
    const newPerspective: Perspective = {
      perspectiveId: UUID.generate(),
      name: '',
      nodes: [],
      edges: [],
      attributes: [],
    };
    setNewPerspectives(prev => [...prev, newPerspective])
    setValue("entityTypeOrder", [...entityOrder, newPerspective.perspectiveId])
  });

  // エンティティ名編集
  const handleChangeEntityTypesGridRow: Layout.RowChangeEvent<GridRow> = useEvent(ev => {
    // 新規追加ドキュメント種類の場合は新規追加インスタンスの名前を直接編集。
    // 既存ドキュメント種類の場合は、名前変更のレコードの一覧を更新。
    const newPerspectiveNames: [string, string][] = []
    const existingPerspectiveNames: [string, string][] = []
    for (const row of ev.changedRows) {
      if (newPerspectives.some(x => x.perspectiveId === row.oldRow.entityTypeId)) {
        newPerspectiveNames.push([row.oldRow.entityTypeId, row.newRow.entityTypeName ?? ''])
      } else {
        existingPerspectiveNames.push([row.oldRow.entityTypeId, row.newRow.entityTypeName ?? ''])
      }
    }

    // 新規追加ドキュメントの名前更新
    setNewPerspectives(prev => {
      const newPerspectives = window.structuredClone(prev)
      for (const [perspectiveId, newName] of newPerspectiveNames) {
        const index = newPerspectives.findIndex(x => x.perspectiveId === perspectiveId)
        if (index !== -1) newPerspectives[index].name = newName
      }
      return newPerspectives
    })

    // 既存ドキュメントの名前更新
    setEditedEntityNames(prev => ({
      ...prev,
      ...Object.fromEntries(existingPerspectiveNames),
    }))
  })

  // 保存
  const [isSaving, setIsSaving] = React.useState(false)
  const handleSave = useEvent(async () => {
    setIsSaving(true)
    await onSave(getValues(), newPerspectives, editedEntityNames)
    setIsSaving(false)
    closeDialog()
  })

  return (
    <div className="w-full h-full flex flex-col gap-1 relative">
      <div className="flex flex-col gap-1">
        <div className="font-semibold">アプリケーション名</div>
        <Input.Word name="applicationName" control={control} />
      </div>
      <div className="basis-4"></div>
      <div className="flex justify-between items-center">
        <div className="font-semibold">ドキュメント種類</div>
        <Input.IconButton icon={Icon.PlusIcon} outline mini onClick={handleCreateNewPerspective}>
          新規追加
        </Input.IconButton>
      </div>
      <Layout.EditableGrid
        rows={entityTypesGridRows}
        getColumnDefs={getEntityTypesGridColumns}
        onChangeRow={handleChangeEntityTypesGridRow}
        className="flex-1"
      />
      <Input.IconButton icon={Icon.CheckIcon} fill onClick={handleSave} loading={isSaving}>
        保存
      </Input.IconButton>

      {isSaving && (
        <Layout.NowLoading />
      )}
    </div>
  )
}

/** ドキュメント種類編集グリッドの並び替え列 */
const renderEntityTypeOrderColumn = (
  entityOrder: string[],
  setValue: UseFormSetValue<AppSettingsForSave>,
): Layout.EditableGridColumnDefRenderCell<GridRow> => (ctx) => {
  const handleUp = () => {
    const reordered = [...entityOrder]
    const index = reordered.indexOf(ctx.row.original.entityTypeId)
    if (index === -1) return
    if (index === 0) return
    const temp = reordered[index - 1]
    reordered[index - 1] = reordered[index]
    reordered[index] = temp
    setValue('entityTypeOrder', reordered)
  }
  const handleDown = () => {
    const reordered = [...entityOrder]
    const index = reordered.indexOf(ctx.row.original.entityTypeId)
    if (index === -1) return
    if (index === reordered.length - 1) return
    const temp = reordered[index + 1]
    reordered[index + 1] = reordered[index]
    reordered[index] = temp
    setValue('entityTypeOrder', reordered)
  }
  return (
    <div className="w-full flex justify-around">
      <Input.IconButton icon={Icon.ArrowUpIcon} mini onClick={handleUp}>
        上へ
      </Input.IconButton>
      <Input.IconButton icon={Icon.ArrowDownIcon} mini onClick={handleDown}>
        下へ
      </Input.IconButton>
    </div>
  )
}
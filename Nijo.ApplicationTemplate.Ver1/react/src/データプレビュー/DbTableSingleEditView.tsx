import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../input"
import * as Layout from "../layout"
import { EditableDbRecord, DbTableSingleItemEditor, ReloadTrigger, DbTableMetadata } from "./types"
import useQueryEditorServerApi from "./useQueryEditorServerApi"

export type DbTableSingleItemSelectorDialogProps = {
  tableMetadata: DbTableMetadata
  onSelect: (keys: string[]) => void
  onCancel: () => void
}

/**
 * どのコードを編集するかを選択するダイアログ
 */
export const DbTableSingleItemSelectorDialog = ({
  tableMetadata,
  onSelect,
  onCancel,
}: DbTableSingleItemSelectorDialogProps) => {
  const { getDbRecords } = useQueryEditorServerApi()
  const [records, setRecords] = React.useState<EditableDbRecord[]>()
  const [error, setError] = React.useState<string | null>(null)

  React.useEffect(() => {
    (async () => {
      const res = await getDbRecords({
        tableName: tableMetadata.tableName,
        whereClause: "",
      })
      if (res.ok) {
        setRecords(res.data.records)
      } else {
        setError(res.error)
      }
    })()
  }, [])

  const columnDefs: Layout.GetColumnDefsFunction<EditableDbRecord> = React.useCallback(cellType => {
    // 選択
    const selectColumn = cellType.other('', {
      defaultWidth: 60,
      isFixed: true,
      renderCell: cell => {

        const handleClick = () => {
          const primaryKeyColumns = tableMetadata.members
            .filter(c => c.isPrimaryKey)
            .map(c => c.columnName)
          const primaryKeyValues = primaryKeyColumns.map(c => cell.row.original.values[c] ?? '')
          onSelect(primaryKeyValues)
        }

        return (
          <Input.IconButton underline mini onClick={handleClick}>
            選択
          </Input.IconButton>
        )
      },
    })

    const valueColumns = tableMetadata.members.map(column => cellType.text(
      `values.${column.columnName}` as ReactHookForm.FieldPathByValue<EditableDbRecord, string | undefined>,
      column.columnName ?? '',
      {
      })) ?? []

    return [
      selectColumn,
      ...valueColumns,
    ]
  }, [tableMetadata])

  return (
    <Layout.ModalDialog
      open
      className="relative w-[80vw] h-[80vh] bg-white flex flex-col gap-1 relative border border-gray-400"
    >
      <div className="h-full w-full flex flex-col p-1 gap-1">

        <div className="flex gap-1 p-1">
          <span className="font-bold">
            編集対象データを選択してください。
          </span>
          <div className="flex-1"></div>
          <Input.IconButton outline mini onClick={onCancel}>
            キャンセル
          </Input.IconButton>
        </div>

        {error && (
          <div className="text-red-500 flex-1">
            {error}
          </div>
        )}
        {records && (
          <Layout.EditableGrid
            rows={records}
            getColumnDefs={columnDefs}
            className="flex-1"
          />
        )}
      </div>

      {!error && !records && (
        <Layout.NowLoading />
      )}
    </Layout.ModalDialog>
  )
}

// ------------------------------------

export type DbTableSingleEditViewRef = {
  getCurrentRecord: () => EditableDbRecord
}
/**
 * DBレコードを集約単位で1件分編集するウィンドウ
 */
export const DbTableSingleEditView = React.forwardRef(({ itemIndex, value, onChangeDefinition, onDeleteDefinition, tableMetadata, trigger, zoom, handleMouseDown }: {
  itemIndex: number
  value: DbTableSingleItemEditor
  onChangeDefinition: (index: number, value: DbTableSingleItemEditor) => void
  onDeleteDefinition: (index: number) => void
  tableMetadata: DbTableMetadata[]
  trigger: ReloadTrigger
  zoom: number
  handleMouseDown: React.MouseEventHandler<Element>
}, ref: React.ForwardedRef<DbTableSingleEditViewRef>) => {

  return (
    <div>
      <h1>DBレコードを集約単位で1件分編集するウィンドウ</h1>
    </div>
  )
})


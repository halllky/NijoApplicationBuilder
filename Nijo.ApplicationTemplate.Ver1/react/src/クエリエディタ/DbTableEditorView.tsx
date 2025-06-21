import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import { EditableDbRecord, DbTableEditor, ReloadTrigger, GetDbRecordsReturn } from "./types"
import * as Input from "../input"
import * as Layout from "../layout"
import React from "react"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import { SqlTextarea } from "./SqlTextarea"

export type DbTableEditorViewRef = {
  getCurrentRecords: () => EditableDbRecord[]
}

export const DbTableEditorView = React.forwardRef(({ itemIndex, value, onChangeDefinition, allTableNames, trigger }: {
  itemIndex: number
  value: DbTableEditor
  onChangeDefinition: (index: number, value: DbTableEditor) => void
  allTableNames: string[]
  trigger: ReloadTrigger
}, ref: React.ForwardedRef<DbTableEditorViewRef>) => {

  // ---------------------------------
  // 定義編集
  const handleChangeTitle = useEvent((e: React.ChangeEvent<HTMLInputElement>) => {
    onChangeDefinition(itemIndex, {
      ...value,
      title: e.target.value,
    })
  })

  const handleChangeTableName = useEvent((e: React.ChangeEvent<HTMLSelectElement>) => {
    onChangeDefinition(itemIndex, {
      ...value,
      tableName: e.target.value,
    })
  })

  const handleChangeWhereClause = useEvent((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onChangeDefinition(itemIndex, {
      ...value,
      whereClause: e.target.value,
    })
  })

  // ---------------------------------
  // レコード編集
  const { getDbRecords } = useQueryEditorServerApi()
  const [error, setError] = React.useState<string | null>(null)
  const { getValues, control, reset, formState: { defaultValues } } = ReactHookForm.useForm<GetDbRecordsReturn>()
  const { fields, append, remove, update } = ReactHookForm.useFieldArray({ name: "records", control })
  const gridRef = React.useRef<Layout.EditableGridRef<EditableDbRecord>>(null)

  React.useImperativeHandle(ref, () => ({
    getCurrentRecords: () => getValues("records"),
  }), [getValues])

  // 読み込み
  React.useEffect(() => {
    (async () => {
      const res = await getDbRecords(value)
      if (res.ok) {
        reset(res.data)
        setError(null)
      } else {
        reset({
          columns: [],
          records: [],
        })
        setError(res.error)
      }
    })()
  }, [trigger])

  // 列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<EditableDbRecord> = React.useCallback(cellType => {
    const status = cellType.other('', {
      defaultWidth: 48,
      enableResizing: false,
      isFixed: true,
      renderCell: cell => (
        <div className="w-full">
          {!cell.row.original.existsInDb ? (
            <span className="text-green-500">
              新規
            </span>
          ) : cell.row.original.deleted ? (
            <span className="text-red-500">
              削除
            </span>
          ) : cell.row.original.changed ? (
            <span className="text-blue-500">
              変更
            </span>
          ) : (
            <span></span>
          )}
        </div>
      ),
    })
    const valueColumns = defaultValues?.columns?.map(column => cellType.text(
      `values.${column}` as ReactHookForm.FieldPathByValue<EditableDbRecord, string | undefined>,
      column ?? '',
      {

      })) ?? []

    return [
      status,
      ...valueColumns,
    ]
  }, [defaultValues])

  // ---------------------------------
  // レコード変更
  const handleChangeRecords: Layout.RowChangeEvent<EditableDbRecord> = useEvent((e) => {
    for (const changedRow of e.changedRows) {
      update(changedRow.rowIndex, { ...changedRow.newRow, changed: true })
    }
  })

  // レコード追加
  const handleAddRecord = useEvent(() => {
    append({ tableName: value.tableName, values: {}, existsInDb: false, changed: false, deleted: false })
  })

  // レコード削除
  const handleDeleteRecord = useEvent((e: React.MouseEvent<HTMLButtonElement>) => {
    const selectedRows = gridRef.current?.getSelectedRows() ?? []
    const removedIndexes: number[] = []
    for (const { row, rowIndex } of selectedRows) {
      if (row.existsInDb) {
        update(rowIndex, { ...row, deleted: true })
      } else {
        removedIndexes.push(rowIndex)
      }
    }
    remove(removedIndexes)
  })

  // キャンセル
  const handleCancel = useEvent(() => {
    if (!window.confirm('変更を取り消しますか？')) return
    reset(defaultValues)
    setError(null)
  })

  // ---------------------------------
  // 折りたたみ
  const [isCollapsed, setIsCollapsed] = React.useState(false)
  const handleToggleCollapse = useEvent(() => {
    setIsCollapsed(state => !state)
  })

  return (
    <div className="flex flex-col resize overflow-hidden border border-gray-500">

      {/* タイトル */}
      <div className="flex gap-1">
        <input type="text"
          value={value.title}
          onChange={handleChangeTitle}
          className="w-full px-1 outline-none"
          placeholder="タイトル"
        />
        {!error && (
          <>
            <Input.IconButton icon={Icon.PlusCircleIcon} onClick={handleAddRecord}>
              追加
            </Input.IconButton>
            <Input.IconButton icon={Icon.TrashIcon} onClick={handleDeleteRecord}>
              削除
            </Input.IconButton>
            <Input.IconButton icon={Icon.ArrowPathIcon} onClick={handleCancel}>
              キャンセル
            </Input.IconButton>
          </>
        )}
        <Input.IconButton
          icon={isCollapsed ? Icon.ChevronUpIcon : Icon.ChevronDownIcon}
          hideText
          onClick={handleToggleCollapse}
        >
          折りたたみ
        </Input.IconButton>
      </div>

      {/* テーブル名, WHERE句 */}
      <div className={`flex flex-col gap-1 p-1 font-mono bg-white border-t border-gray-300 ${isCollapsed ? 'hidden' : ''}`}>
        <div className="flex gap-2">
          <span className="select-none text-gray-500">
            SELECT * FROM
          </span>

          <select
            value={value.tableName}
            onChange={handleChangeTableName}
            className="border border-gray-500"
          >
            {allTableNames.map((tableName) => (
              <option key={tableName} value={tableName}>{tableName}</option>
            ))}
          </select>

          <span className="select-none text-gray-500">
            WHERE
          </span>
        </div>

        <SqlTextarea
          value={value.whereClause}
          onChange={handleChangeWhereClause}
          placeholder="抽出条件がある場合はここに記載"
          className="flex-1"
        />
      </div>

      {/* レコード */}
      {error ? (
        <div className="flex-1 text-red-500 border-t border-gray-300">
          {error}
        </div>
      ) : (
        <Layout.EditableGrid
          ref={gridRef}
          rows={fields}
          getColumnDefs={getColumnDefs}
          onChangeRow={handleChangeRecords}
          className="flex-1 border-t border-gray-300"
        />
      )}
    </div>
  )
})

import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import { DbRecord, DbTableEditor, ReloadTrigger } from "./types"
import * as Input from "../input"
import * as Layout from "../layout"
import React from "react"
import useQueryEditorServerApi from "./useQueryEditorServerApi"

export default function DbTableEditorView({ itemIndex, value, onChangeDefinition, allTableNames, trigger }: {
  itemIndex: number
  value: DbTableEditor
  onChangeDefinition: (index: number, value: DbTableEditor) => void
  allTableNames: string[]
  trigger: ReloadTrigger
}) {

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
  const { control, reset, formState: { defaultValues } } = ReactHookForm.useForm<FormValues>()
  const { fields, append, remove, update } = ReactHookForm.useFieldArray({ name: "records", control })
  const gridRef = React.useRef<Layout.EditableGridRef<DbRecord>>(null)

  React.useEffect(() => {
    (async () => {
      const res = await getDbRecords(value)
      if (res.ok) {
        reset({ records: res.records })
        setError(null)
      } else {
        reset({ records: [] })
        setError(res.error)
      }
    })()
  }, [trigger])

  const getColumnDefs: Layout.GetColumnDefsFunction<DbRecord> = React.useCallback(e => {
    return defaultValues?.records?.map(record => ({
      header: record?.tableName ?? "",
      fieldPath: "values",
      cellType: "text",
    })) ?? []
  }, [defaultValues])

  const handleChangeRecords: Layout.RowChangeEvent<DbRecord> = useEvent((e) => {
    for (const changedRow of e.changedRows) {
      update(changedRow.rowIndex, changedRow.newRow)
    }
  })

  const handleAddRecord = useEvent(() => {
    append({ tableName: value.tableName, values: {}, existsInDb: false, changed: false, deleted: false })
  })

  const handleDeleteRecord = useEvent((e: React.MouseEvent<HTMLButtonElement>) => {
    const selectedRows = gridRef.current?.getSelectedRows().map(e => e.rowIndex)
    if (selectedRows) remove(...selectedRows)
  })

  return (
    <div className="flex flex-col resize">

      {/* タイトル */}
      <input type="text"
        value={value.title}
        onChange={handleChangeTitle}
        className="w-full"
        placeholder="タイトル"
      />

      {/* テーブル名, WHERE句 */}
      <div className="block">
        <span className="select-none">
          SELECT * FROM
        </span>

        <select
          value={value.tableName}
          onChange={handleChangeTableName}
        >
          {allTableNames.map((tableName) => (
            <option key={tableName} value={tableName}>{tableName}</option>
          ))}
        </select>

        <span className="select-none">
          WHERE
        </span>

        <textarea
          value={value.whereClause}
          onChange={handleChangeWhereClause}
          className="w-full resize-none field-sizing-content outline-none"
        />
      </div>

      {/* レコード */}
      {error ? (
        <div className="flex-1 text-red-500">
          {error}
        </div>
      ) : (
        <div className="flex-1">
          <div className="flex flex-wrap gap-1">
            <Input.IconButton icon={Icon.PlusCircleIcon} onClick={handleAddRecord}>
              追加
            </Input.IconButton>
            <Input.IconButton icon={Icon.TrashIcon} onClick={handleDeleteRecord}>
              削除
            </Input.IconButton>
          </div>
          <Layout.EditableGrid
            ref={gridRef}
            rows={fields}
            getColumnDefs={getColumnDefs}
            onChangeRow={handleChangeRecords}
          />
        </div>
      )}
    </div>
  )
}

type FormValues = {
  records: DbRecord[]
}

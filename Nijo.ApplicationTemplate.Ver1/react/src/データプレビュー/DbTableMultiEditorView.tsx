import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import { EditableDbRecord, DbTableEditor, ReloadTrigger, GetDbRecordsReturn, DbTableMetadata } from "./types"
import * as Input from "../input"
import * as Layout from "../layout"
import React from "react"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import { SqlTextarea } from "./SqlTextarea"
export type DbTableEditorViewRef = {
  getCurrentRecords: () => EditableDbRecord[]
}

/**
 * DBレコード一括編集ウィンドウ
 */
export const DbTableMultiEditorView = React.forwardRef(({ itemIndex, value, onChangeDefinition, onDeleteDefinition, tableMetadata, trigger, zoom, handleMouseDown }: {
  itemIndex: number
  value: DbTableEditor
  onChangeDefinition: (index: number, value: DbTableEditor) => void
  onDeleteDefinition: (index: number) => void
  tableMetadata: DbTableMetadata[]
  trigger: ReloadTrigger
  zoom: number
  handleMouseDown: React.MouseEventHandler<Element>
}, ref: React.ForwardedRef<DbTableEditorViewRef>) => {

  // ---------------------------------
  // 定義編集
  const handleChangeTitle = useEvent(() => {
    const newTitle = window.prompt(`${value.title ?? 'テーブル編集'}の名前を入力してください`)
    if (!newTitle) return;
    onChangeDefinition(itemIndex, {
      ...value,
      title: newTitle,
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
          records: [],
        })
        setError(res.error)
      }
    })()
  }, [trigger])

  // 列定義
  const [foreignKeyReferenceDialog, setForeignKeyReferenceDialog] = React.useState<ForeignKeyReferenceDialogProps | null>(null)
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

    const thisTableMetadata = tableMetadata.find(table => table.tableName === value.tableName)
    const valueColumns: Layout.EditableGridColumnDef<EditableDbRecord>[] = []

    for (const column of thisTableMetadata?.columns ?? []) {
      if (column.foreignKeyTableName) {
        // 外部キーの列の場合は値を直接入力するのではなくダイアログから選択
        valueColumns.push(cellType.other(
          column.columnName ?? '',
          {
            renderCell: cell => {
              const handleClick = () => {
                setForeignKeyReferenceDialog({
                  tableMetadata: tableMetadata.find(table => table.tableName === column.foreignKeyTableName)!,
                  onSelect: selectedRecord => {
                    const clone = window.structuredClone(cell.row.original)
                    const value = ReactHookForm.get(selectedRecord, `values.${column.foreignKeyColumnName}` as ReactHookForm.FieldPathByValue<EditableDbRecord, string | undefined>)
                    clone.changed = true
                    ReactHookForm.set(clone, `values.${column.columnName}`, value)
                    update(cell.row.index, clone)
                    setForeignKeyReferenceDialog(null)
                  },
                  onCancel: () => setForeignKeyReferenceDialog(null),
                })
              }

              return (
                <div className="w-full flex overflow-hidden">
                  <Input.IconButton icon={Icon.MagnifyingGlassIcon} hideText onClick={handleClick}>
                    検索
                  </Input.IconButton>
                  <span className="flex-1">
                    {ReactHookForm.get(cell.row.original, `values.${column.columnName}` as ReactHookForm.FieldPathByValue<EditableDbRecord, string | undefined>)}
                  </span>
                </div>
              )
            },
          })
        )
      } else {
        // 外部キーの列でない場合は値を直接入力。
        // 数値や日付などのバリエーションは特に考慮しない。
        valueColumns.push(cellType.text(
          `values.${column.columnName}` as ReactHookForm.FieldPathByValue<EditableDbRecord, string | undefined>,
          column.columnName ?? '',
          {})
        )
      }
    }

    return [
      status,
      ...valueColumns,
    ]
  }, [defaultValues, tableMetadata])

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

  // リセット
  const handleClickReset = useEvent(() => {
    if (!window.confirm('データの変更を取り消しますか？')) return
    reset(defaultValues)
    setError(null)
  })

  // ---------------------------------
  // ウィンドウの削除
  const handleDeleteWindow = useEvent(() => {
    onDeleteDefinition(itemIndex)
  })

  // ---------------------------------
  // 折りたたみ
  const handleToggleCollapse = useEvent(() => {
    onChangeDefinition(itemIndex, {
      ...value,
      isSettingCollapsed: !value.isSettingCollapsed,
    })
  })

  const handleMouseDownButtons = useEvent((e: React.MouseEvent<Element>) => {
    e.stopPropagation()
  })

  return (<>
    <div className="bg-gray-200 border border-gray-300 h-full flex flex-col">
      <div className="flex gap-1 pl-1 items-center bg-gray-100">
        <span onMouseDown={handleMouseDown} className="select-none cursor-grab">
          {value.tableName}
        </span>
        <Input.IconButton icon={Icon.PencilIcon} hideText onClick={handleChangeTitle} onMouseDown={handleMouseDownButtons}>
          名前を変更
        </Input.IconButton>
        <div onMouseDown={handleMouseDown} className="flex-1 self-stretch cursor-grab"></div>

        {!error && (
          <>
            <Input.IconButton icon={Icon.PlusCircleIcon} onClick={handleAddRecord}>
              追加
            </Input.IconButton>
            <Input.IconButton icon={Icon.TrashIcon} onClick={handleDeleteRecord}>
              削除
            </Input.IconButton>
            <Input.IconButton icon={Icon.ArrowUturnLeftIcon} onClick={handleClickReset}>
              リセット
            </Input.IconButton>
          </>
        )}
        <Input.IconButton
          icon={value.isSettingCollapsed ? Icon.ChevronUpIcon : Icon.ChevronDownIcon}
          hideText
          onClick={handleToggleCollapse}
        >
          折りたたみ
        </Input.IconButton>
        <Input.IconButton icon={Icon.XMarkIcon} hideText onClick={handleDeleteWindow}>
          削除
        </Input.IconButton>
      </div>
      <div className="flex-1 flex flex-col min-h-0">
        {/* テーブル名, WHERE句 */}
        <div className={`flex flex-col gap-1 p-1 font-mono bg-white border-t border-gray-300 ${value.isSettingCollapsed ? 'hidden' : ''}`}>
          <div className="flex gap-2">
            <span className="select-none text-gray-500">
              SELECT * FROM
            </span>

            <select
              value={value.tableName}
              onChange={handleChangeTableName}
              className="border border-gray-500"
            >
              {tableMetadata.map(table => (
                <option key={table.tableName} value={table.tableName}>{table.tableName}</option>
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
    </div>

    {/* 外部参照テーブルのレコード選択ダイアログ */}
    {foreignKeyReferenceDialog && (
      <ForeignKeyReferenceDialog {...foreignKeyReferenceDialog} />
    )}
  </>)
})

// -----------------------------------
type ForeignKeyReferenceDialogProps = {
  tableMetadata: DbTableMetadata
  onSelect: (record: EditableDbRecord) => void
  onCancel: () => void
}

/**
 * 外部参照テーブルのレコード選択ダイアログ
 */
const ForeignKeyReferenceDialog = ({
  tableMetadata,
  onSelect,
  onCancel,
}: ForeignKeyReferenceDialogProps) => {
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
      renderCell: cell => (
        <Input.IconButton underline mini onClick={() => {
          onSelect(cell.row.original)
        }}>
          選択
        </Input.IconButton>
      ),
    })

    const valueColumns = tableMetadata.columns.map(column => cellType.text(
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
            {tableMetadata.tableName}
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

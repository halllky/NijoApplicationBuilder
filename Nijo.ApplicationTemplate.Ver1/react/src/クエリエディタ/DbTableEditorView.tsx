import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import { EditableDbRecord, DbTableEditor, ReloadTrigger, GetDbRecordsReturn } from "./types"
import * as Input from "../input"
import * as Layout from "../layout"
import React from "react"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import { SqlTextarea } from "./SqlTextarea"
import DraggableWindow from "./DraggableWindow"

export type DbTableEditorViewRef = {
  getCurrentRecords: () => EditableDbRecord[]
}

export const DbTableEditorView = React.forwardRef(({ itemIndex, value, onChangeDefinition, onDeleteDefinition, allTableNames, trigger, zoom }: {
  itemIndex: number
  value: DbTableEditor
  onChangeDefinition: (index: number, value: DbTableEditor) => void
  onDeleteDefinition: (index: number) => void
  allTableNames: string[]
  trigger: ReloadTrigger
  zoom: number
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

  // ドラッグで位置を変更
  const handleMouseMove = useEvent((e: MouseEvent) => {
    const deltaX = e.movementX / zoom
    const deltaY = e.movementY / zoom
    onChangeDefinition(itemIndex, {
      ...value,
      layout: {
        ...value.layout,
        x: Math.max(0, value.layout.x + deltaX),
        y: Math.max(0, value.layout.y + deltaY),
      },
    })
  })

  const handleMouseDownButtons = useEvent((e: React.MouseEvent<Element>) => {
    e.stopPropagation()
  })

  return (
    <DraggableWindow
      layout={value.layout}
      onMove={handleMouseMove}
      className="bg-gray-100 border border-gray-500"
    >
      {({ handleMouseDown }) => (<>
        <div className="flex gap-1 pl-1 items-center cursor-grab" onMouseDown={handleMouseDown}>
          <span className="select-none">
            {value.tableName}
          </span>
          <Input.IconButton icon={Icon.PencilIcon} hideText onClick={handleChangeTitle} onMouseDown={handleMouseDownButtons}>
            名前を変更
          </Input.IconButton>
          <div className="flex-1"></div>

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
        <div className="flex flex-col h-full">
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
      </>)}
    </DraggableWindow>
  )
})

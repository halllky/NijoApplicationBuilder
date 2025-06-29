import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import { EditableDbRecord, DbTableMultiItemEditor, ReloadTrigger, GetDbRecordsReturn, TableMetadataHelper } from "./types"
import * as Input from "../../input"
import * as Layout from "../../layout"
import React from "react"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import { SqlTextarea } from "./SqlTextarea"
import { useDbRecordGridColumnDef } from "./useDbRecordGridColumnDef"
import { UUID } from "uuidjs"
import { DataPreviewGlobalContext } from "./DataPreviewGlobalContext"
import { EditorDesignByAgggregate } from "./types"
import { DbTableMultiEditViewSettings, DbTableMultiEditViewSettingsProps } from "./MultiView.Settings"

export type DbTableMultiEditorViewProps = {
  itemIndex: number
  value: DbTableMultiItemEditor
  onChangeDefinition: (index: number, value: DbTableMultiItemEditor) => void
  onDeleteDefinition: (index: number) => void
  onIsDirtyChange: (index: number, isDirty: boolean) => void
  tableMetadataHelper: TableMetadataHelper
  trigger: ReloadTrigger
  zoom: number
  handleMouseDown: React.MouseEventHandler<Element>
}

export type DbTableEditorViewRef = {
  getCurrentRecords: () => EditableDbRecord[]
  /** SingleViewの場合のみ、このウィンドウで新規作成されたデータの主キーを取得する */
  getCurrentRootItemKeys: (() => string[]) | undefined
}

/**
 * DBレコード一括編集ウィンドウ
 */
export const DbTableMultiEditorView = React.forwardRef(({
  itemIndex,
  value,
  onChangeDefinition,
  onDeleteDefinition,
  onIsDirtyChange,
  tableMetadataHelper,
  trigger,
  zoom,
  handleMouseDown,
}: DbTableMultiEditorViewProps, ref: React.ForwardedRef<DbTableEditorViewRef>) => {

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
  const { getValues, control, reset, formState: { defaultValues, isDirty } } = ReactHookForm.useForm<GetDbRecordsReturn>()
  const { fields, append, remove, update } = ReactHookForm.useFieldArray({ name: "records", control })
  const gridRef = React.useRef<Layout.EditableGridRef<EditableDbRecord>>(null)
  const [settingsDialogProps, setSettingsDialogProps] = React.useState<DbTableMultiEditViewSettingsProps | null>(null)

  React.useEffect(() => {
    onIsDirtyChange(itemIndex, isDirty)
  }, [isDirty])

  React.useImperativeHandle(ref, () => ({
    getCurrentRecords: () => getValues("records"),
    getCurrentRootItemKeys: undefined,
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
  const aggregate = React.useMemo(() => {
    const result = tableMetadataHelper.allAggregates().find(table => table.tableName === value.tableName)
    if (!result) throw new Error(`テーブルが見つかりません: ${value.tableName}`)
    return result
  }, [tableMetadataHelper, value.tableName])
  const { getColumnDefs, ForeignKeyReferenceDialog } = useDbRecordGridColumnDef(
    'multi-record-editor',
    aggregate,
    tableMetadataHelper,
    update,
    'multiView',
    false,
  )

  // グリッドの列幅の自動保存
  const {
    formState: {
      defaultValues: dataPreviewDefaultValues
    },
    getValues: getDataPreviewValues,
    setValue: setDataPreviewValues,
  } = React.useContext(DataPreviewGlobalContext)
  const gridColumnStorage: Layout.EditableGridAutoSaveStorage = React.useMemo(() => ({
    loadState: () => {
      return dataPreviewDefaultValues?.design?.[aggregate.path]?.multiViewGridLayout ?? null
    },
    saveState: (gridState) => {
      setDataPreviewValues(`design.${aggregate.path}.multiViewGridLayout`, gridState)
    },
  }), [dataPreviewDefaultValues, setDataPreviewValues, aggregate.path])

  // ---------------------------------
  // レコード変更
  const handleChangeRecords: Layout.RowChangeEvent<EditableDbRecord> = useEvent((e) => {
    for (const changedRow of e.changedRows) {
      update(changedRow.rowIndex, { ...changedRow.newRow, changed: true })
    }
  })

  // レコード追加
  const handleAddRecord = useEvent(() => {
    append({
      uniqueId: UUID.generate(),
      tableName: value.tableName,
      values: {},
      existsInDb: false,
      changed: false,
      deleted: false,
    })
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
  // ウィンドウを閉じる
  const handleCloseWindow = useEvent(() => {
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

  // 設定ダイアログ
  const handleOpenSettings = useEvent(() => {
    setSettingsDialogProps({
      aggregate,
      tableMetadataHelper,
      initialSettings: getDataPreviewValues(`design.${aggregate.path}`) ?? {},
      onApply: (updatedSettings) => {
        setDataPreviewValues(`design.${aggregate.path}`, updatedSettings)
        setSettingsDialogProps(null)
      },
      onCancel: () => {
        setSettingsDialogProps(null)
      },
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
            <Input.IconButton icon={Icon.PlusIcon} onClick={handleAddRecord}>
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
        <Input.IconButton icon={Icon.Cog6ToothIcon} mini hideText onClick={handleOpenSettings}>
          設定
        </Input.IconButton>
        <Input.IconButton
          icon={value.isSettingCollapsed ? Icon.ChevronUpIcon : Icon.ChevronDownIcon}
          hideText
          onClick={handleToggleCollapse}
        >
          折りたたみ
        </Input.IconButton>
        <Input.IconButton icon={Icon.XMarkIcon} hideText onClick={handleCloseWindow}>
          ウィンドウを閉じる
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
              {tableMetadataHelper.allAggregates().map(table => (
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
            storage={gridColumnStorage}
            className="flex-1 border-t border-gray-300 overflow-scroll"
          />
        )}
      </div>
    </div>

    {/* 外部参照テーブルのレコード選択ダイアログ */}
    {ForeignKeyReferenceDialog}

    {/* 設定ダイアログ */}
    {settingsDialogProps && (
      <DbTableMultiEditViewSettings {...settingsDialogProps} />
    )}
  </>)
})

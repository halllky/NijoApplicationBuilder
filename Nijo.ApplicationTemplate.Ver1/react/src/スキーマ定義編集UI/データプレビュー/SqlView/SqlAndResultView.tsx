import React from "react"
import { SqlAndResult, ReloadTrigger, ExecuteQueryReturn } from "../types"
import * as Input from "../../../input"
import * as Layout from "../../../layout"
import * as Icon from "@heroicons/react/24/outline"
import useQueryEditorServerApi from "../useQueryEditorServerApi"
import useEvent from "react-use-event-hook"
import { SqlTextarea } from "../UI/SqlTextarea"
import { SqlAndResultViewSettings, SqlAndResultViewSettingsProps } from "./SqlAndResultView.Settings"
/**
 * GUI上でSQLを入力して、結果を表示するコンポーネント。
 * レコードの編集はできない。
 */
export default function SqlAndResultView({ itemIndex, value, onChangeDefinition, onDeleteDefinition, trigger, zoom, handleMouseDown }: {
  itemIndex: number
  value: SqlAndResult
  onChangeDefinition: (index: number, value: SqlAndResult) => void
  onDeleteDefinition: (index: number) => void
  trigger: ReloadTrigger
  zoom: number
  handleMouseDown: React.MouseEventHandler<Element>
}) {

  // ---------------------------------
  // 設定ダイアログ
  const [settingsDialogProps, setSettingsDialogProps] = React.useState<SqlAndResultViewSettingsProps | null>(null)
  const [triggerOnSettingChange, setTriggerOnSettingChange] = React.useState(-1) // 設定変更時にSQLを再実行するためのトリガー

  const handleOpenSettings = useEvent(() => {
    setSettingsDialogProps({
      initialSettings: {
        title: value.title ?? 'クエリ',
        sql: value.sql,
      },
      onApply: (updatedSettings) => {
        onChangeDefinition(itemIndex, {
          ...value,
          title: updatedSettings.title,
          sql: updatedSettings.sql,
        })
        setSettingsDialogProps(null)
        setTriggerOnSettingChange(state => state * -1)
      },
      onCancel: () => {
        setSettingsDialogProps(null)
      },
    })
  })

  // ---------------------------------
  // クエリ実行とその結果の表示
  const { executeQuery } = useQueryEditorServerApi()
  const [queryResult, setQueryResult] = React.useState<ExecuteQueryReturn>({ columns: [], rows: [] })
  const [error, setError] = React.useState<string | null>(null)
  React.useEffect(() => {
    (async () => {
      const res = await executeQuery(value.sql)
      if (res.ok) {
        setQueryResult(res.records)
        setError(null)
      } else {
        setQueryResult({ columns: [], rows: [] })
        setError(res.error)
      }
    })()
  }, [trigger, triggerOnSettingChange])

  // 列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<ExecuteQueryReturn['rows'][number]> = React.useCallback(e => {
    return queryResult.columns.map(colName => ({
      header: colName,
      fieldPath: colName,
      cellType: "text",
    }))
  }, [queryResult])

  // グリッドの列幅の自動保存
  const saveState = useEvent((gridState: string) => {
    onChangeDefinition(itemIndex, {
      ...value,
      windowLayout: {
        ...value.windowLayout,
        gridState,
      },
    })
  })
  const gridColumnStorage: Layout.EditableGridAutoSaveStorage = React.useMemo(() => ({
    loadState: () => {
      return value.windowLayout?.gridState ?? null
    },
    saveState,
  }), [value.windowLayout?.gridState, saveState])

  // ---------------------------------
  // ウィンドウの削除
  const handleDeleteWindow = useEvent(() => {
    onDeleteDefinition(itemIndex)
  })



  const handleMouseDownButtons = useEvent((e: React.MouseEvent<Element>) => {
    e.stopPropagation()
  })

  return (<>
    <div className="bg-gray-200 border-2 border-white h-full flex flex-col">
      <div className="flex gap-1 pl-1 py-[2px] items-center">
        <span onMouseDown={handleMouseDown} className="select-none text-gray-500 font-bold cursor-grab">
          {value.title}
        </span>
        <div onMouseDown={handleMouseDown} className="flex-1 self-stretch cursor-grab"></div>

        <Input.IconButton icon={Icon.Cog6ToothIcon} mini hideText onClick={handleOpenSettings} onMouseDown={handleMouseDownButtons}>
          設定
        </Input.IconButton>
        <Input.IconButton icon={Icon.XMarkIcon} hideText onClick={handleDeleteWindow} onMouseDown={handleMouseDownButtons}>
          削除
        </Input.IconButton>
      </div>
      <div className="flex-1 flex flex-col min-h-0">
        {/* 結果 */}
        {error ? (
          <div className="text-red-500 flex-1">
            {error}
          </div>
        ) : (
          <Layout.EditableGrid
            rows={queryResult.rows}
            getColumnDefs={getColumnDefs}
            storage={gridColumnStorage}
            className="flex-1 overflow-scroll"
          />
        )}
      </div>
    </div>

    {/* 設定ダイアログ */}
    {settingsDialogProps && (
      <SqlAndResultViewSettings {...settingsDialogProps} />
    )}
  </>)
}

import React from "react"
import { SqlAndResult, ReloadTrigger, ExecuteQueryReturn } from "./types"
import * as Input from "../input"
import * as Layout from "../layout"
import * as Icon from "@heroicons/react/24/outline"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import useEvent from "react-use-event-hook"
import { SqlTextarea } from "./SqlTextarea"
import DraggableWindow from "./DraggableWindow"

/**
 * GUI上でSQLを入力して、結果を表示するコンポーネント。
 * レコードの編集はできない。
 */
export default function SqlAndResultView({ itemIndex, value, onChangeDefinition, onDeleteDefinition, trigger, zoom }: {
  itemIndex: number
  value: SqlAndResult
  onChangeDefinition: (index: number, value: SqlAndResult) => void
  onDeleteDefinition: (index: number) => void
  trigger: ReloadTrigger
  zoom: number
}) {

  // ---------------------------------
  // 定義編集
  const handleChangeTitle = useEvent(() => {
    const newTitle = window.prompt(`${value.title ?? 'クエリ'}の名前を入力してください`)
    if (!newTitle) return;
    onChangeDefinition(itemIndex, {
      ...value,
      title: newTitle,
    })
  })
  const handleChangeSql = useEvent((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onChangeDefinition(itemIndex, {
      ...value,
      sql: e.target.value,
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
  }, [trigger])

  // 列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<ExecuteQueryReturn['rows'][number]> = React.useCallback(e => {
    return queryResult.columns.map(colName => ({
      header: colName,
      fieldPath: colName,
      cellType: "text",
    }))
  }, [queryResult])

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
      layout: {
        ...value.layout,
      },
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
        <div className="flex gap-1 pl-1 py-[2px] items-center cursor-grab" onMouseDown={handleMouseDown}>
          <span className="select-none">
            {value.title}
          </span>
          <Input.IconButton icon={Icon.PencilIcon} hideText onClick={handleChangeTitle} onMouseDown={handleMouseDownButtons}>
            名前を変更
          </Input.IconButton>
          <div className="flex-1"></div>

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
          {/* SQL */}
          <SqlTextarea
            value={value.sql}
            onChange={handleChangeSql}
            className={`border-t border-gray-300 bg-white p-1 ${value.isSettingCollapsed ? 'hidden' : ''}`}
          />

          {/* 結果 */}
          {error ? (
            <div className="text-red-500 flex-1 border-t border-gray-300">
              {error}
            </div>
          ) : (
            <Layout.EditableGrid
              rows={queryResult.rows}
              getColumnDefs={getColumnDefs}
              className="flex-1 border-t border-gray-300"
            />
          )}
        </div>
      </>)}
    </DraggableWindow >
  )
}

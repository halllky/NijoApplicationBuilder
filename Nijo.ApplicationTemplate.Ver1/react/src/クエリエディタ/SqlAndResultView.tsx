import React from "react"
import { SqlAndResult, ReloadTrigger, ExecuteQueryReturn } from "./types"
import * as Input from "../input"
import * as Layout from "../layout"
import * as Icon from "@heroicons/react/24/outline"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import useEvent from "react-use-event-hook"
import { SqlTextarea } from "./SqlTextarea"

/**
 * GUI上でSQLを入力して、結果を表示するコンポーネント。
 * レコードの編集はできない。
 */
export default function SqlAndResultView({ itemIndex, value, onChangeDefinition, trigger }: {
  itemIndex: number
  value: SqlAndResult
  onChangeDefinition: (index: number, value: SqlAndResult) => void
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

  // ---------------------------------
  // 折りたたみ
  const [isCollapsed, setIsCollapsed] = React.useState(false)
  const handleToggleCollapse = useEvent(() => {
    setIsCollapsed(state => !state)
  })

  const getColumnDefs: Layout.GetColumnDefsFunction<ExecuteQueryReturn['rows'][number]> = React.useCallback(e => {
    return queryResult.columns.map(colName => ({
      header: colName,
      fieldPath: colName,
      cellType: "text",
    }))
  }, [queryResult])

  return (
    <div className="flex flex-col resize overflow-hidden border border-gray-500">

      {/* タイトル */}
      <div className="flex gap-1">
        <input type="text"
          value={value.title}
          onChange={handleChangeTitle}
          spellCheck={false}
          className="w-full px-1 outline-none"
          placeholder="タイトル"
        />
        <Input.IconButton
          icon={isCollapsed ? Icon.ChevronUpIcon : Icon.ChevronDownIcon}
          hideText
          onClick={handleToggleCollapse}
        >
          折りたたみ
        </Input.IconButton>
      </div>

      {/* SQL */}
      <SqlTextarea
        value={value.sql}
        onChange={handleChangeSql}
        className={`border-t border-gray-300 bg-white p-1 ${isCollapsed ? 'hidden' : ''}`}
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

    </div >
  )
}

import React from "react"
import { EditableDbRecord, QueryEditor } from "./types"
import * as ReactHookForm from "react-hook-form"
import * as Layout from "../layout"
import { DbTableEditorView, DbTableEditorViewRef } from "./DbTableEditorView"
import SqlAndResultView from "./SqlAndResultView"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import useEvent from "react-use-event-hook"

/**
 * アプリケーション全体のデータの動きを確認してデータ構造の仕様の精度を上げるための、
 * 複数のテーブルや、SQLとその結果を表示するUIです。
 */
export default function () {
  const { getTableNames } = useQueryEditorServerApi()
  const [allTableNames, setAllTableNames] = React.useState<string[]>()

  React.useEffect(() => {
    (async () => {
      const res = await getTableNames()
      if (res.ok) {
        setAllTableNames(res.tableNames)
      }
    })()
  }, [getTableNames])

  if (!allTableNames) {
    return (
      <Layout.NowLoading />
    )
  }

  return (
    <AfterReady allTableNames={allTableNames} />
  )
}

const AfterReady = ({ allTableNames }: {
  allTableNames: string[]
}) => {

  // ---------------------------------
  // 定義編集
  const { control, reset } = ReactHookForm.useForm<QueryEditor>({
    defaultValues: GET_TEST_DATA(),
  })
  const { fields, append, remove, update } = ReactHookForm.useFieldArray({ name: 'items', control, keyName: 'use-field-array-id' })

  // ---------------------------------
  // 保存、およびSQL結果取得トリガー
  const { batchUpdate } = useQueryEditorServerApi()
  const [saveError, setSaveError] = React.useState<string>()
  const [trigger, setTrigger] = React.useState(-1)

  const dbTableEditorsRef = React.useRef<React.RefObject<DbTableEditorViewRef | null>[]>([])
  dbTableEditorsRef.current = []
  for (let i = 0; i < fields.length; i++) {
    dbTableEditorsRef.current[i] = React.createRef()
  }

  const handleKeyDown = useEvent(async (e: React.KeyboardEvent<HTMLDivElement>) => {
    // Ctrl + S でSQL再読み込みを実行
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
      e.preventDefault()

      // 編集中のテーブルエディタの値を保存する。
      // どれか1件でもエラーがあればロールバックされる
      const recordsToSave: EditableDbRecord[] = []
      for (const editorRef of dbTableEditorsRef.current) {
        if (editorRef.current) {
          const records = editorRef.current.getCurrentRecords()
          recordsToSave.push(...records)
        }
      }

      if (recordsToSave.length > 0) {
        const result = await batchUpdate(recordsToSave)
        if (!result.ok) {
          setSaveError(result.error)
          return
        }
      }

      setSaveError(undefined)
      setTrigger(trigger * -1)
    }
  })

  return (
    <div
      className="flex flex-col gap-4 p-4 resize outline-none"
      tabIndex={0} // キーボード操作を可能にする
      onKeyDown={handleKeyDown}
    >
      {saveError && (
        <div className="text-red-500 text-sm">
          {saveError}
        </div>
      )}

      {fields.map((item, index) => item.type === "sqlAndResult" ? (
        <SqlAndResultView
          key={item.id}
          itemIndex={index}
          value={item}
          onChangeDefinition={update}
          trigger={trigger}
        />
      ) : (
        <DbTableEditorView
          ref={dbTableEditorsRef.current[index]}
          key={item.id}
          itemIndex={index}
          value={item}
          onChangeDefinition={update}
          allTableNames={allTableNames}
          trigger={trigger}
        />
      ))}
    </div>
  )
}

const GET_TEST_DATA = (): QueryEditor => ({
  id: "1",
  title: "クエリエディタ",
  items: [
    {
      id: "1",
      title: "名前に3が含まれる顧客",
      type: "sqlAndResult",
      sql: "SELECT * FROM 顧客マスタ WHERE CUSTOMER_NAME LIKE '%3%'",
    },
    {
      id: "2",
      type: "dbTableEditor",
      title: "顧客マスタ",
      tableName: "顧客マスタ",
      whereClause: "",
    }
  ]
})
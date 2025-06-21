import React from "react"
import { QueryEditor } from "./types"
import * as ReactHookForm from "react-hook-form"
import * as Layout from "../layout"
import DbTableEditorView from "./DbTableEditorView"
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
  }, [])

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
  const [trigger, setTrigger] = React.useState(-1)

  const handleKeyDown = useEvent((e: React.KeyboardEvent<HTMLDivElement>) => {
    // Ctrl + S でSQL再読み込みを実行
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
      e.preventDefault()
      setTrigger(trigger * -1)
    }
  })

  return (
    <div
      className="flex flex-col gap-px p-4 resize outline-none"
      tabIndex={0} // キーボード操作を可能にする
      onKeyDown={handleKeyDown}
    >

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
  ]
})
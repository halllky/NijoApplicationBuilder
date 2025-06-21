import React from "react"
import { EditableDbRecord, QueryEditor, QueryEditorItem } from "./types"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../input"
import * as Icon from "@heroicons/react/24/outline"
import * as Layout from "../layout"
import { DbTableEditorView, DbTableEditorViewRef } from "./DbTableEditorView"
import SqlAndResultView from "./SqlAndResultView"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import useEvent from "react-use-event-hook"
import { UUID } from "uuidjs"

export type QueryEditorProps = {
  className?: string
}

/**
 * アプリケーション全体のデータの動きを確認してデータ構造の仕様の精度を上げるための、
 * 複数のテーブルや、SQLとその結果を表示するUIです。
 */
export default function ({ className }: QueryEditorProps) {
  const { getTableNames } = useQueryEditorServerApi()
  const [allTableNames, setAllTableNames] = React.useState<string[]>()
  const [defaultValues, setDefaultValues] = React.useState<QueryEditor>()

  React.useEffect(() => {
    // テーブル名一覧を取得
    (async () => {
      const res = await getTableNames()
      if (res.ok) {
        setAllTableNames(res.tableNames)
      }
    })()

    // ローカルストレージからデータを読み込む
    try {
      const item = localStorage.getItem(LOCALSTORAGE_KEY)
      if (item !== null) {
        const queryEditor: QueryEditor = JSON.parse(item)
        setDefaultValues(queryEditor)
      } else {
        setDefaultValues(GET_DEFAULT_DATA())
      }
    } catch {
      setDefaultValues(GET_DEFAULT_DATA())
    }
  }, [getTableNames])

  // ローカルストレージに保存
  const handleSave = useEvent((data: QueryEditor) => {
    localStorage.setItem(LOCALSTORAGE_KEY, JSON.stringify(data))
  })

  if (!allTableNames || !defaultValues) {
    return (
      <Layout.NowLoading />
    )
  }

  return (
    <AfterReady
      allTableNames={allTableNames}
      defaultValues={defaultValues}
      onSave={handleSave}
      className={className}
    />
  )
}

const AfterReady = ({ allTableNames, defaultValues, onSave, className }: {
  allTableNames: string[]
  defaultValues: QueryEditor
  onSave: (data: QueryEditor) => void
  className?: string
}) => {

  // ---------------------------------
  // 定義編集
  const { control, getValues } = ReactHookForm.useForm<QueryEditor>({ defaultValues })
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
      setTrigger(trigger * -1) // データ再読み込みをトリガー
      onSave(getValues()) // レイアウトとSQL定義を保存
    }
  })

  // ---------------------------------
  // 拡大縮小（0.1 ～ 1.0）
  const [zoom, setZoom] = React.useState(1)
  const handleZoomOut = useEvent(() => {
    setZoom(prev => Math.max(0.1, prev - 0.1))
  })
  const handleZoomIn = useEvent(() => {
    setZoom(prev => Math.min(1, prev + 0.1))
  })
  const handleResetZoom = useEvent(() => {
    setZoom(1)
  })

  // ---------------------------------
  // ウィンドウの追加（クエリ）
  const handleAddQuery = useEvent(() => {
    const newQueryTitle = window.prompt("クエリのタイトルを入力してください")
    if (!newQueryTitle) return;
    append(createNewQueryEditorItem("sqlAndResult", newQueryTitle))
  })

  // ウィンドウの追加（テーブル編集）
  const [newTableName, setNewTableName] = React.useState("")
  const handleChangeNewTableName = useEvent((e: React.ChangeEvent<HTMLSelectElement>) => {
    setNewTableName(e.target.value)
  })
  const handleAddTableEditor = useEvent(() => {
    append(createNewQueryEditorItem("dbTableEditor", newTableName))
  })

  // ---------------------------------
  // ウィンドウの削除
  const handleRemoveWindow = useEvent((itemIndex: number) => {
    if (!window.confirm(`${fields[itemIndex].title}を削除しますか？`)) return;
    remove(itemIndex)
  })

  return (
    <div
      className={`relative flex flex-col overflow-auto resize outline-none ${className ?? ""}`}
      tabIndex={0} // キーボード操作を可能にする
      onKeyDown={handleKeyDown}
    >
      {saveError && (
        <div className="text-red-500 text-sm">
          {saveError}
        </div>
      )}

      {/* スクロールエリア */}
      <div className="flex-1 relative overflow-auto bg-white border border-gray-500" style={{ zoom }}>
        {fields.map((item, index) => item.type === "sqlAndResult" ? (
          <SqlAndResultView
            key={item.id}
            itemIndex={index}
            value={item}
            onChangeDefinition={update}
            onDeleteDefinition={handleRemoveWindow}
            trigger={trigger}
            zoom={zoom}
          />
        ) : (
          <DbTableEditorView
            ref={dbTableEditorsRef.current[index]}
            key={item.id}
            itemIndex={index}
            value={item}
            onChangeDefinition={update}
            onDeleteDefinition={handleRemoveWindow}
            allTableNames={allTableNames}
            trigger={trigger}
            zoom={zoom}
          />
        ))}
      </div>

      {/* ウィンドウの追加削除 */}
      <div className="absolute top-4 right-4 flex flex-col gap-1 items-end">
        <div className="flex gap-1">
          <select
            value={newTableName}
            onChange={handleChangeNewTableName}
            className="flex-1 bg-white border border-gray-500"
          >
            {allTableNames.map(name => (
              <option key={name} value={name}>{name}</option>
            ))}
          </select>
          <Input.IconButton onClick={handleAddTableEditor} fill>
            追加
          </Input.IconButton>
        </div>
        <Input.IconButton icon={Icon.PlusIcon} onClick={handleAddQuery} fill>
          クエリ追加
        </Input.IconButton>
      </div>

      {/* ズーム */}
      <div className="flex flex-col absolute bottom-4 right-4">
        <div className="text-sm select-none">
          ズーム
        </div>
        <div className="flex gap-1">
          <Input.IconButton icon={Icon.MinusIcon} onClick={handleZoomOut} fill hideText>
            ズームアウト
          </Input.IconButton>
          <Input.IconButton icon={Icon.PlusIcon} onClick={handleZoomIn} fill hideText>
            ズームイン
          </Input.IconButton>
          <Input.IconButton icon={Icon.ArrowPathIcon} onClick={handleResetZoom} fill hideText>
            リセット
          </Input.IconButton>
        </div>
      </div>
    </div>
  )
}

const LOCALSTORAGE_KEY = ":query-editor:"

const GET_DEFAULT_DATA = (): QueryEditor => ({
  id: UUID.generate(),
  title: "クエリエディタ",
  items: [],
})

const createNewQueryEditorItem = (type: "sqlAndResult" | "dbTableEditor", queryTitleOrTableName: string): QueryEditorItem => {
  if (type === "sqlAndResult") {
    return {
      id: UUID.generate(),
      title: queryTitleOrTableName,
      type,
      sql: "SELECT 1",
      isSettingCollapsed: false,
      layout: {
        x: 0,
        y: 0,
        width: 640,
        height: 200,
      },
    }
  } else {
    return {
      id: UUID.generate(),
      title: queryTitleOrTableName,
      type,
      tableName: queryTitleOrTableName,
      whereClause: "",
      isSettingCollapsed: false,
      layout: {
        x: 0,
        y: 0,
        width: 640,
        height: 200,
      },
    }
  }
}

import React from "react"
import { DbTableMetadata, EditableDbRecord, QueryEditor, QueryEditorItem, QueryEditorDiagramItem } from "./types"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../input"
import * as Icon from "@heroicons/react/24/outline"
import * as Layout from "../layout"
import { DiagramView } from "../layout/DiagramView"
import { DbTableEditorView, DbTableEditorViewRef } from "./DbTableEditorView"
import SqlAndResultView from "./SqlAndResultView"
import useQueryEditorServerApi, { QueryEditorServerApiContext } from "./useQueryEditorServerApi"
import useEvent from "react-use-event-hook"
import { UUID } from "uuidjs"
import { CommentView } from "./CommentView"

export type QueryEditorProps = {
  /** データ操作対象のバックエンドのURL */
  backendUrl: string
  className?: string
}

/**
 * アプリケーション全体のデータの動きを確認してデータ構造の仕様の精度を上げるための、
 * 複数のテーブルや、SQLとその結果を表示するUIです。
 */
export default function ({ backendUrl, className }: QueryEditorProps) {
  const { getTableMetadata } = useQueryEditorServerApi(backendUrl)
  const [loadError, setLoadError] = React.useState<string>()
  const [tableMetadata, setTableMetadata] = React.useState<DbTableMetadata[]>()
  const [defaultValues, setDefaultValues] = React.useState<QueryEditor>()

  React.useEffect(() => {
    setLoadError(undefined);

    // テーブル名一覧を取得
    (async () => {
      const res = await getTableMetadata()
      if (res.ok) {
        setTableMetadata(res.data)
      } else {
        setLoadError(res.error)
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
  }, [getTableMetadata])

  // ローカルストレージに保存
  const handleSave = useEvent((data: QueryEditor) => {
    localStorage.setItem(LOCALSTORAGE_KEY, JSON.stringify(data))
  })

  if (loadError) {
    return (
      <div className={`relative ${className ?? ""}`}>
        <div className="text-red-500 text-sm whitespace-pre-wrap">{loadError}</div>
      </div>
    )
  }

  if (!tableMetadata || !defaultValues) {
    return (
      <div className={`relative ${className ?? ""}`}>
        <Layout.NowLoading />
      </div>
    )
  }

  return (
    <QueryEditorServerApiContext.Provider value={backendUrl}>
    <AfterReady
      tableMetadata={tableMetadata}
      defaultValues={defaultValues}
      onSave={handleSave}
      className={className}
    />
    </QueryEditorServerApiContext.Provider>
  )
}

const AfterReady = ({ tableMetadata, defaultValues, onSave, className }: {
  tableMetadata: DbTableMetadata[]
  defaultValues: QueryEditor
  onSave: (data: QueryEditor) => void
  className?: string
}) => {

  // ---------------------------------
  // 定義編集
  const { control, getValues } = ReactHookForm.useForm<QueryEditor>({ defaultValues })
  const { fields, append, remove, update } = ReactHookForm.useFieldArray({ name: 'items', control, keyName: 'use-field-array-id' })

  // DiagramView用にitemsとcommentsを統合
  const commentFields = ReactHookForm.useFieldArray({ name: 'comments', control, keyName: 'use-field-array-id' })

  const diagramItems: QueryEditorDiagramItem[] = React.useMemo(() => {
    const items: QueryEditorDiagramItem[] = [...fields]
    const comments: QueryEditorDiagramItem[] = commentFields.fields.map((comment): QueryEditorDiagramItem => ({
      ...comment,
      type: "comment" as const,
    }))
    return [...items, ...comments]
  }, [fields, commentFields.fields])

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
  // DiagramView用のハンドラー
  const handleUpdateDiagramItem = useEvent((index: number, item: QueryEditorDiagramItem) => {
    if (item.type === "comment") {
      const commentIndex = commentFields.fields.findIndex(c => c.id === item.id)
      if (commentIndex >= 0) {
        commentFields.update(commentIndex, item)
      }
    } else {
      const itemIndex = fields.findIndex(f => f.id === item.id)
      if (itemIndex >= 0) {
        update(itemIndex, item)
      }
    }
  })

  const handleRemoveDiagramItem = useEvent((index: number) => {
    const item = diagramItems[index]
    if (item.type === "comment") {
      const commentIndex = commentFields.fields.findIndex(c => c.id === item.id)
      if (commentIndex >= 0) {
        if (!window.confirm(`コメントを削除しますか？`)) return
        commentFields.remove(commentIndex)
    }
    } else {
      const itemIndex = fields.findIndex(f => f.id === item.id)
      if (itemIndex >= 0) {
        if (!window.confirm(`${item.title}を削除しますか？`)) return
        remove(itemIndex)
    }
    }
  })

  // ---------------------------------
  // ウィンドウの追加（クエリ）
  const handleAddQuery = useEvent(() => {
    const newQueryTitle = window.prompt("クエリのタイトルを入力してください")
    if (!newQueryTitle) return;
    append(createNewQueryEditorItem("sqlAndResult", newQueryTitle))
  })

  // ウィンドウの追加（テーブル編集）
  const [newTableName, setNewTableName] = React.useState(tableMetadata[0]?.tableName ?? "")
  const handleChangeNewTableName = useEvent((e: React.ChangeEvent<HTMLSelectElement>) => {
    setNewTableName(e.target.value)
  })
  const handleAddTableEditor = useEvent(() => {
    append(createNewQueryEditorItem("dbTableEditor", newTableName))
  })

  // ---------------------------------
  // DiagramView用のrenderItem関数
  const renderDiagramItem = React.useCallback((item: QueryEditorDiagramItem, index: number, { zoom, DragHandle }: {
    onUpdateLayout: (layout: any) => void
    onRemove: () => void
    zoom: number
    DragHandle: React.ReactNode
    handleMouseDown: React.MouseEventHandler<Element>
  }) => {
    if (item.type === "comment") {
      const commentIndex = commentFields.fields.findIndex(c => c.id === item.id)
      return (
        <CommentView
          commentIndex={commentIndex}
          comment={item}
          onChangeComment={(idx, updatedComment) => {
            const updatedItem = { ...updatedComment, type: "comment" as const }
            handleUpdateDiagramItem(index, updatedItem)
          }}
          onDeleteComment={() => handleRemoveDiagramItem(index)}
          zoom={zoom}
          DragHandle={DragHandle}
        />
      )
    } else if (item.type === "sqlAndResult") {
      const itemIndex = fields.findIndex(f => f.id === item.id)
      return (
        <SqlAndResultView
          itemIndex={itemIndex}
          value={item}
          onChangeDefinition={(idx, updatedItem) => handleUpdateDiagramItem(index, updatedItem)}
          onDeleteDefinition={() => handleRemoveDiagramItem(index)}
          trigger={trigger}
          zoom={zoom}
          DragHandle={DragHandle}
        />
      )
    } else {
      const itemIndex = fields.findIndex(f => f.id === item.id)
      const refIndex = itemIndex >= 0 ? itemIndex : 0
      return (
        <DbTableEditorView
          ref={dbTableEditorsRef.current[refIndex]}
          itemIndex={itemIndex}
          value={item}
          onChangeDefinition={(idx, updatedItem) => handleUpdateDiagramItem(index, updatedItem)}
          onDeleteDefinition={() => handleRemoveDiagramItem(index)}
          tableMetadata={tableMetadata}
          trigger={trigger}
          zoom={zoom}
          DragHandle={DragHandle}
        />
      )
    }
  }, [fields, commentFields.fields, tableMetadata, trigger, handleUpdateDiagramItem, handleRemoveDiagramItem, dbTableEditorsRef])

  // ---------------------------------
  // コメント
  const handleAddComment = useEvent(() => {
    commentFields.append({
      id: UUID.generate(),
      content: "",
      layout: {
        x: 0,
        y: 0,
        width: 320,
        height: 200,
      },
    })
  })

  return (
    <div
      className={`relative flex flex-col overflow-hidden resize outline-none ${className ?? ""}`}
      tabIndex={0} // キーボード操作を可能にする
      onKeyDown={handleKeyDown}
    >
      {saveError && (
        <div className="text-red-500 text-sm">
          {saveError}
        </div>
      )}

      <DiagramView
        items={diagramItems}
        onUpdateItem={handleUpdateDiagramItem}
        onRemoveItem={handleRemoveDiagramItem}
        renderItem={renderDiagramItem}
        className="flex-1"
        >
      {/* ウィンドウの追加削除 */}
      <div className="absolute top-4 right-4 flex flex-col gap-1 items-end">
        <div className="flex gap-1">
          <select
            value={newTableName}
            onChange={handleChangeNewTableName}
            className="flex-1 bg-white border border-gray-500"
          >
            {tableMetadata.map(table => (
              <option key={table.tableName} value={table.tableName}>{table.tableName}</option>
            ))}
          </select>
          <Input.IconButton onClick={handleAddTableEditor} fill>
            追加
          </Input.IconButton>
        </div>
        <div className="flex gap-1">
          <Input.IconButton icon={Icon.PlusIcon} onClick={handleAddQuery} fill>
            クエリ追加
          </Input.IconButton>
          <Input.IconButton icon={Icon.PlusIcon} onClick={handleAddComment} fill>
            コメント追加
          </Input.IconButton>
        </div>
      </div>
      </DiagramView>
    </div>
  )
}

const LOCALSTORAGE_KEY = ":query-editor:"

const GET_DEFAULT_DATA = (): QueryEditor => ({
  id: UUID.generate(),
  title: "クエリエディタ",
  items: [],
  comments: [],
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

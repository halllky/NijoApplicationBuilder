import React from "react"
import { DbTableMetadata, EditableDbRecord, QueryEditor, QueryEditorItem } from "./types"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../input"
import * as Icon from "@heroicons/react/24/outline"
import * as Layout from "../layout"
import { DbTableEditorView, DbTableEditorViewRef } from "./DbTableEditorView"
import SqlAndResultView from "./SqlAndResultView"
import useQueryEditorServerApi from "./useQueryEditorServerApi"
import useEvent from "react-use-event-hook"
import { UUID } from "uuidjs"
import { CommentView } from "./CommentView"

export type QueryEditorProps = {
  className?: string
}

/**
 * アプリケーション全体のデータの動きを確認してデータ構造の仕様の精度を上げるための、
 * 複数のテーブルや、SQLとその結果を表示するUIです。
 */
export default function ({ className }: QueryEditorProps) {
  const { getTableMetadata } = useQueryEditorServerApi()
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
    <AfterReady
      tableMetadata={tableMetadata}
      defaultValues={defaultValues}
      onSave={handleSave}
      className={className}
    />
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
  // パン操作（背景ドラッグでの移動）
  const [panOffset, setPanOffset] = React.useState({ x: 0, y: 0 })
  const [isDragging, setIsDragging] = React.useState(false)
  const [dragStart, setDragStart] = React.useState({ x: 0, y: 0 })
  const [dragStartOffset, setDragStartOffset] = React.useState({ x: 0, y: 0 })

  const scrollRef = React.useRef<HTMLDivElement>(null)
  const canvasRef = React.useRef<HTMLDivElement>(null)
  const handleMouseDown = useEvent((e: React.MouseEvent<HTMLDivElement>) => {
    const target = e.target as HTMLElement
    if (target === canvasRef.current || target === scrollRef.current) {
      setIsDragging(true)
      setDragStart({ x: e.clientX, y: e.clientY })
      setDragStartOffset({ ...panOffset })
      e.preventDefault()
    }
  })

  // パン操作開始
  React.useEffect(() => {
    if (!isDragging) return

    const handleGlobalMouseMove = (e: MouseEvent) => {
      const deltaX = (e.clientX - dragStart.x) / zoom
      const deltaY = (e.clientY - dragStart.y) / zoom
      setPanOffset({
        x: dragStartOffset.x + deltaX,
        y: dragStartOffset.y + deltaY,
      })
    }

    // パン操作終了
    const handleGlobalMouseUp = () => {
      setIsDragging(false)
    }

    document.addEventListener('mousemove', handleGlobalMouseMove)
    document.addEventListener('mouseup', handleGlobalMouseUp)

    return () => {
      document.removeEventListener('mousemove', handleGlobalMouseMove)
      document.removeEventListener('mouseup', handleGlobalMouseUp)
    }
  }, [isDragging, dragStart.x, dragStart.y, dragStartOffset.x, dragStartOffset.y])

  const handleResetPan = useEvent(() => {
    setPanOffset({ x: 0, y: 0 })
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

  // ---------------------------------
  // コメント
  const commentFields = ReactHookForm.useFieldArray({ name: 'comments', control, keyName: 'use-field-array-id' })
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
  const handleRemoveComment = useEvent((commentIndex: number) => {
    if (!window.confirm(`コメントを削除しますか？`)) return;
    commentFields.remove(commentIndex)
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

      {/* スクロールエリア */}
      <div
        ref={scrollRef}
        className="flex-1 relative overflow-hidden bg-white border border-gray-500 select-none"
        style={{
          zoom,
          cursor: isDragging ? 'grabbing' : 'grab',
        }}
        onMouseDown={handleMouseDown}
      >
        {/* キャンバス領域 */}
        <div
          ref={canvasRef}
          className="relative w-[150vw] h-[150vh] min-w-[150vw] min-h-[150vh]"
          style={{
            transform: `translate(${panOffset.x}px, ${panOffset.y}px)`,
          }}
        >
          {/* クエリウィンドウ、テーブル編集ウィンドウ */}
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
              tableMetadata={tableMetadata}
              trigger={trigger}
              zoom={zoom}
            />
          ))}

          {/* コメント */}
          {commentFields.fields.map((comment, index) => (
            <CommentView
              key={comment.id}
              commentIndex={index}
              comment={comment}
              onChangeComment={commentFields.update}
              onDeleteComment={handleRemoveComment}
              zoom={zoom}
            />
          ))}
        </div>
      </div>

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

      {/* ズーム */}
      <div className="flex flex-col absolute bottom-4 right-4">
        <div className="text-sm select-none">
          ズーム {Math.round(zoom * 100)}%
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
        <div className="basis-1"></div>
        <Input.IconButton icon={Icon.ArrowPathIcon} onClick={handleResetPan} fill>
          位置リセット
        </Input.IconButton>
      </div>
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

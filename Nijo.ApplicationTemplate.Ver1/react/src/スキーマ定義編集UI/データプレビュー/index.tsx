import React from "react"
import * as ReactRouter from "react-router"
import { EditableDbRecord, QueryEditor, QueryEditorItem, QueryEditorDiagramItem, TableMetadataHelper } from "./types"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../input"
import * as Icon from "@heroicons/react/24/outline"
import * as Layout from "../../layout"
import * as Util from "../../util"
import { DiagramView } from "../../layout/DiagramView"
import { DbTableMultiEditorView, DbTableEditorViewRef } from "./DbTableMultiEditorView"
import SqlAndResultView from "./SqlAndResultView"
import useQueryEditorServerApi, { QueryEditorServerApiContext } from "./useQueryEditorServerApi"
import useEvent from "react-use-event-hook"
import { UUID } from "uuidjs"
import { CommentView } from "./CommentView"
import { DiagramItemLayout } from "../../layout/DiagramView/types"
import { DbTableSingleEditView, DbTableSingleItemSelectorDialog, DbTableSingleItemSelectorDialogProps } from "./DbTableSingleEditView"
import { SERVER_DOMAIN } from "../../routes"
import { SERVER_API_TYPE_INFO, SERVER_URL_SUBDIRECTORY } from "../型つきドキュメント/TypedDocumentContext"
import { PageFrame } from "../PageFrame"

/** 自動生成されたあとのアプリケーションのwebapiのURL。とりあえず決め打ち */
export const BACKEND_URL = "https://localhost:7098"

/**
 * アプリケーション全体のデータの動きを確認してデータ構造の仕様の精度を上げるための、
 * 複数のテーブルや、SQLとその結果を表示するUIです。
 */
export const DataPreview = () => {
  const { dataPreviewId } = ReactRouter.useParams()
  const { getTableMetadata } = useQueryEditorServerApi(BACKEND_URL)
  const [loadError, setLoadError] = React.useState<string>()
  const [tableMetadata, setTableMetadata] = React.useState<TableMetadataHelper>()
  const [defaultValues, setDefaultValues] = React.useState<QueryEditor>()
  const [isDirty, setIsDirty] = React.useState(false)
  const afterLoadedRef = React.useRef<{ triggerSave: () => void } | null>(null)

  // 保存ボタンの状態
  const [saveButtonText, setSaveButtonText] = React.useState('保存(Ctrl + S)')
  const [nowSaving, setNowSaving] = React.useState(false)

  React.useEffect(() => {
    setLoadError(undefined);

    (async () => {
      // テーブル名一覧を取得
      const res = await getTableMetadata()
      if (!res.ok) {
        setLoadError(res.error)
        return
      }
      setTableMetadata(res.data)

      // サーバーからデータを読み込む
      const QUERY_KEY = "dataPreviewId" satisfies keyof SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.LOAD_DATA_PREVIEW]["query"]
      const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.LOAD_DATA_PREVIEW}?${QUERY_KEY}=${dataPreviewId}`, {
        method: 'GET',
      })
      if (!response.ok) {
        setDefaultValues(GET_DEFAULT_DATA())
        setLoadError(response.statusText)
        return
      }
      const data: SERVER_API_TYPE_INFO[typeof SERVER_URL_SUBDIRECTORY.LOAD_DATA_PREVIEW]["response"] = await response.json()
      setDefaultValues(data)
    })();
  }, [])

  // サーバーに保存
  const [saveError, setSaveError] = React.useState<string>()
  const handleSave = useEvent(async (data: QueryEditor) => {
    const response = await fetch(`${SERVER_DOMAIN}${SERVER_URL_SUBDIRECTORY.SAVE_DATA_PREVIEW}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    })
    if (!response.ok) {
      setSaveError(response.statusText)
    }
  })

  return (
    <PageFrame
      title="データプレビュー"
      shouldBlock={isDirty}
      headerComponent={(
        <>
          <span className="flex-1 text-xs text-gray-500">
            ※ 自動生成後のアプリケーションのwebapiを用いて動作しています。
            起動しない場合は、デバッグメニューからdotnetのデバッグ用サーバーを起動してください。
          </span>
          <div className="basis-36 flex justify-end">
            <Input.IconButton
              fill
              onClick={afterLoadedRef.current?.triggerSave}
              loading={nowSaving}
            >
              {saveButtonText}
            </Input.IconButton>
          </div>
        </>
      )}
    >
      {loadError && (
        <div className={`relative h-full border-t border-gray-300`}>
          <div className="text-red-500 text-sm whitespace-pre-wrap">{loadError}</div>
        </div>
      )}

      {(!tableMetadata || !defaultValues) && (
        <div className={`relative h-full border-t border-gray-300`}>
          <Layout.NowLoading />
        </div>
      )}

      {!loadError && tableMetadata && defaultValues && (
        <QueryEditorServerApiContext.Provider value={BACKEND_URL}>
          {saveError && (
            <div className="text-rose-500 text-sm">
              {saveError}
            </div>
          )}
          <AfterReady
            ref={afterLoadedRef}
            tableMetadata={tableMetadata}
            defaultValues={defaultValues}
            onSave={handleSave}
            onIsDirtyChange={setIsDirty}
            setSaveButtonText={setSaveButtonText}
            setNowSaving={setNowSaving}
            className="h-full border-t border-gray-300"
          />
        </QueryEditorServerApiContext.Provider>
      )}
    </PageFrame>
  )
}

const AfterReady = React.forwardRef(({ tableMetadata, defaultValues, onSave, onIsDirtyChange, setSaveButtonText, setNowSaving, className }: {
  tableMetadata: TableMetadataHelper
  defaultValues: QueryEditor
  onSave: (data: QueryEditor) => void
  onIsDirtyChange: (isDirty: boolean) => void
  setSaveButtonText: (text: string) => void
  setNowSaving: (saving: boolean) => void
  className?: string
}, ref: React.ForwardedRef<{ triggerSave: () => void }>) => {

  // ---------------------------------
  // 定義編集
  const { control, getValues, formState: { isDirty }, reset } = ReactHookForm.useForm<QueryEditor>({ defaultValues })
  const { fields, append, remove, update } = ReactHookForm.useFieldArray({ name: 'items', control, keyName: 'use-field-array-id' })

  // 画面離脱時のメッセージ表示用の状態
  const [dirtyItems, setDirtyItems] = React.useState<number[]>([]) // レコード編集ウィンドウはそれぞれ独自のuseFormを持っているため
  React.useEffect(() => {
    onIsDirtyChange(isDirty || dirtyItems.length > 0)
  }, [isDirty, dirtyItems])
  const handleIsDirtyChange = React.useCallback((index: number, isDirty: boolean) => {
    if (isDirty) {
      setDirtyItems(prev => [...prev, index])
    } else {
      setDirtyItems(prev => prev.filter(i => i !== index))
    }
  }, [])

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
  for (let i = 0; i < diagramItems.length; i++) {
    dbTableEditorsRef.current[i] = React.createRef()
  }

  // Ctrl+S でSQL再読み込みを実行
  const handleSaveAndReload = useEvent(async () => {
    setSaveError(undefined)
    setNowSaving(true)

    // データベースのレコードの更新。
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
        setNowSaving(false)
        return
      }
    }

    // データ再読み込みをトリガー
    setSaveError(undefined)
    setTrigger(trigger * -1)

    // -------------------------------
    // レイアウトとSQL定義を保存
    const currentValues = window.structuredClone(getValues())

    // 新規作成SingleViewウィンドウの主キー項目を設定
    for (const item of currentValues.items) {
      if (item.type !== "dbTableSingleEditor") continue
      if (item.rootItemKeys) continue
      const itemIndex = fields.findIndex(f => f.id === item.id)
      if (itemIndex === -1) continue
      const editorRef = dbTableEditorsRef.current[itemIndex]
      if (!editorRef.current || !editorRef.current.getCurrentRootItemKeys) continue

      item.rootItemKeys = editorRef.current.getCurrentRootItemKeys()
    }

    onSave(currentValues)

    // -------------------------------
    // 保存完了メッセージを表示
    setSaveButtonText('保存しました。')
    window.setTimeout(() => {
      setSaveButtonText('保存(Ctrl + S)')
    }, 2000)
    setNowSaving(false)
    reset(currentValues)
  })

  Util.useCtrlS(handleSaveAndReload)

  React.useImperativeHandle(ref, () => ({
    triggerSave: handleSaveAndReload,
  }), [handleSaveAndReload])

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

  // ウィンドウの追加（テーブル一括編集）
  const [newTableName, setNewTableName] = React.useState(tableMetadata.rootAggregates()[0]?.tableName ?? "")
  const handleChangeNewTableName = useEvent((e: React.ChangeEvent<HTMLSelectElement>) => {
    setNewTableName(e.target.value)
  })
  const handleAddMultiItemEditor = useEvent(() => {
    append(createNewQueryEditorItem("dbTableEditor", newTableName))
  })

  // ウィンドウの追加（テーブル詳細編集）
  const [dbTableSingleItemSelectorDialogProps, setDbTableSingleItemSelectorDialogProps] = React.useState<DbTableSingleItemSelectorDialogProps | null>(null)
  const handleOpenSingleItemSelector = useEvent(() => {
    const editTableMetadata = tableMetadata.allAggregates().find(t => t.tableName === newTableName)
    if (!editTableMetadata) {
      console.log("テーブルが見つかりません", newTableName)
      return
    }

    // 詳細編集は必ずルート集約単位なので、selectで子孫集約が選択された場合はルート集約を選択したものとして扱う
    const rootAggregate = tableMetadata.getRoot(editTableMetadata)
    setDbTableSingleItemSelectorDialogProps({
      tableMetadata: rootAggregate,
      tableMetadataHelper: tableMetadata,
      onSelect: (keys: string[]) => {
        append(createNewQueryEditorItem("dbTableSingleEditor", newTableName, keys))
        setDbTableSingleItemSelectorDialogProps(null)
      },
      onCancel: () => {
        setDbTableSingleItemSelectorDialogProps(null)
      },
    })
  })

  const handleAddNewRecord = useEvent(() => {
    append(createNewQueryEditorItem("dbTableSingleEditor(new)", newTableName))
    setDbTableSingleItemSelectorDialogProps(null)
  })

  // ---------------------------------
  // DiagramView用のrenderItem関数
  const renderDiagramItem = React.useCallback((item: QueryEditorDiagramItem, index: number, { zoom, handleMouseDown }: {
    onUpdateLayout: (layout: DiagramItemLayout) => void
    onRemove: () => void
    zoom: number
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
          handleMouseDown={handleMouseDown}
        />
      )
    } else if (item.type === "sqlAndResult") {
      const itemIndex = fields.findIndex(f => f.id === item.id)
      return (
        <SqlAndResultView
          itemIndex={itemIndex}
          value={item}
          onChangeDefinition={handleUpdateDiagramItem}
          onDeleteDefinition={handleRemoveDiagramItem}
          trigger={trigger}
          zoom={zoom}
          handleMouseDown={handleMouseDown}
        />
      )
    } else if (item.type === "dbTableEditor") {
      const itemIndex = fields.findIndex(f => f.id === item.id)
      const refIndex = itemIndex >= 0 ? itemIndex : 0
      return (
        <DbTableMultiEditorView
          ref={dbTableEditorsRef.current[refIndex]}
          itemIndex={itemIndex}
          value={item}
          onChangeDefinition={handleUpdateDiagramItem}
          onDeleteDefinition={handleRemoveDiagramItem}
          onIsDirtyChange={handleIsDirtyChange}
          tableMetadataHelper={tableMetadata}
          trigger={trigger}
          zoom={zoom}
          handleMouseDown={handleMouseDown}
        />
      )
    } else {
      const itemIndex = fields.findIndex(f => f.id === item.id)
      const refIndex = itemIndex >= 0 ? itemIndex : 0
      return (
        <DbTableSingleEditView
          ref={dbTableEditorsRef.current[refIndex]}
          itemIndex={itemIndex}
          value={item}
          onChangeDefinition={handleUpdateDiagramItem}
          onDeleteDefinition={handleRemoveDiagramItem}
          onIsDirtyChange={handleIsDirtyChange}
          tableMetadataHelper={tableMetadata}
          trigger={trigger}
          zoom={zoom}
          handleMouseDown={handleMouseDown}
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
      className={`relative flex flex-col overflow-hidden outline-none ${className ?? ""}`}
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
              {tableMetadata.allAggregates().map(table => (
                <option key={table.tableName} value={table.tableName}>{table.displayName}({table.tableName})</option>
              ))}
            </select>
            <Input.IconButton onClick={handleAddMultiItemEditor} fill>
              一括編集
            </Input.IconButton>
            <Input.IconButton onClick={handleOpenSingleItemSelector} fill>
              詳細編集
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

      {dbTableSingleItemSelectorDialogProps && (
        <DbTableSingleItemSelectorDialog {...dbTableSingleItemSelectorDialogProps}>
          <Input.IconButton icon={Icon.PlusIcon} outline onClick={handleAddNewRecord}>
            新しいデータを作成する
          </Input.IconButton>
          <div className="basis-6"></div>
        </DbTableSingleItemSelectorDialog>
      )}
    </div>
  )
})

export const DATA_PREVIEW_LOCALSTORAGE_KEY = ":query-editor:"

export const GET_DEFAULT_DATA = (): QueryEditor => ({
  id: UUID.generate(),
  title: "クエリエディタ",
  items: [],
  comments: [],
})

const createNewQueryEditorItem = (type: "sqlAndResult" | "dbTableEditor" | "dbTableSingleEditor" | "dbTableSingleEditor(new)", queryTitleOrTableName: string, keys?: string[]): QueryEditorItem => {
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
  } else if (type === "dbTableEditor") {
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
  } else {
    if (type === "dbTableSingleEditor" && !keys) throw new Error("keys is required")
    if (type === "dbTableSingleEditor(new)" && keys) throw new Error("keys is not allowed")

    return {
      id: UUID.generate(),
      title: queryTitleOrTableName,
      type: "dbTableSingleEditor",
      rootTableName: queryTitleOrTableName,
      rootItemKeys: type === "dbTableSingleEditor" ? keys! : null,
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

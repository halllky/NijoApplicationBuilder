import React from "react"
import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../input"
import * as Layout from "../../layout"
import * as Util from "../../util"
import * as Icon from "@heroicons/react/24/solid"
import { GraphViewRef } from "../../layout/GraphView"
import * as ReactRouter from "react-router-dom"
import { SchemaDefinitionGlobalState } from "./types"
import { ViewState } from "../../layout/GraphView/Cy"
import { asTree } from "./types"
import { SERVER_DOMAIN } from "../../routes"
import cytoscape from 'cytoscape'; // cytoscapeの型情報をインポート
import { useLayoutSaving } from './index.Grid.useLayoutSaving';
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import { PageRootAggregate } from "./index.Grid"
import { UUID } from "uuidjs"
import { PageFrame } from "../PageFrame"
import { useValidation } from "./useValidation"
import NijoUiErrorMessagePane from "./index.ErrorMessage"
import { AppSchemaDefinitionGraph } from "./index.Graph"

export const NijoUiAggregateDiagram = () => {

  // 画面初期表示時、サーバーからスキーマ情報を読み込む
  const [schema, setSchema] = React.useState<SchemaDefinitionGlobalState>()
  const [loadError, setLoadError] = React.useState<string>()
  const load = useEvent(async () => {
    try {
      const schemaResponse = await fetch(`${SERVER_DOMAIN}/load`)

      if (!schemaResponse.ok) {
        const body = await schemaResponse.text();
        throw new Error(`Failed to load schema: ${schemaResponse.status} ${body}`);
      }

      const schemaData: SchemaDefinitionGlobalState = await schemaResponse.json()
      setSchema(schemaData)
    } catch (error) {
      console.error(error)
      setLoadError(error instanceof Error ? error.message : `不明なエラー(${error})`)
    }
  })
  React.useEffect(() => {
    load()
  }, [load])

  // 保存処理
  const handleSave = useEvent(async (valuesToSave: SchemaDefinitionGlobalState): Promise<{ ok: boolean, error?: string }> => {
    try {
      const response = await fetch(`${SERVER_DOMAIN}/save`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(valuesToSave),
      })
      if (!response.ok) {
        const bodyText = await response.text()
        try {
          const bodyJson = JSON.parse(bodyText) as string[]
          console.error(bodyJson)
          return { ok: false, error: `保存に失敗しました:\n${bodyJson.join('\n')}` }
        } catch {
          console.error(bodyText)
          return { ok: false, error: `保存に失敗しました (サーバーからの応答が不正です):\n${bodyText}` }
        }
      }
      return { ok: true }
    } catch (error) {
      console.error(error)
      return { ok: false, error: error instanceof Error ? error.message : `不明なエラー(${error})` }
    }
  })

  // ノード状態の保存と復元
  const saveLoad = useLayoutSaving();

  const defaultValues = React.useMemo(() => ({
    onlyRoot: saveLoad.savedOnlyRoot ?? false,
    savedViewState: saveLoad.savedViewState ?? {},
  }), [saveLoad.savedOnlyRoot, saveLoad.savedViewState])

  // 読み込みエラー
  if (loadError) {
    return (
      <div>
        読み込みでエラーが発生しました: {loadError}
        <Input.IconButton onClick={load}>
          再読み込み
        </Input.IconButton>
      </div>
    )
  }

  // 読み込み中
  if (schema === undefined) {
    return <Layout.NowLoading />
  }

  return (
    <AfterLoaded
      triggerSaveLayout={saveLoad.triggerSaveLayout}
      clearSavedLayout={saveLoad.clearSavedLayout}
      onlyRootDefaultValue={defaultValues.onlyRoot}
      savedViewStateDefaultValue={defaultValues.savedViewState}
      formDefaultValues={schema}
      executeSave={handleSave}
    />
  )
}

const AfterLoaded = ({ triggerSaveLayout, clearSavedLayout, onlyRootDefaultValue, savedViewStateDefaultValue, formDefaultValues, executeSave }: {
  triggerSaveLayout: ReturnType<typeof useLayoutSaving>["triggerSaveLayout"]
  clearSavedLayout: ReturnType<typeof useLayoutSaving>["clearSavedLayout"]
  onlyRootDefaultValue: boolean
  savedViewStateDefaultValue: Partial<ViewState>
  formDefaultValues: SchemaDefinitionGlobalState
  executeSave: (values: SchemaDefinitionGlobalState) => Promise<{ ok: boolean, error?: string }>
}) => {
  const formMethods = ReactHookForm.useForm<SchemaDefinitionGlobalState>({
    defaultValues: formDefaultValues,
  })
  const { getValues, control, formState: { isDirty } } = formMethods
  const xmlElementTrees = getValues("xmlElementTrees")
  const graphViewRef = React.useRef<GraphViewRef>(null)

  // 属性定義
  const watchedAttributeDefs = ReactHookForm.useWatch({ name: 'attributeDefs', control })
  const attributeDefsMap = React.useMemo(() => {
    return new Map(watchedAttributeDefs.map(attrDef => [attrDef.attributeName, attrDef]))
  }, [watchedAttributeDefs])

  // 入力検証
  const { getValidationResult, trigger, validationResultList } = useValidation(getValues)
  React.useEffect(() => {
    trigger() // 画面表示時に入力検証を実行
  }, [])

  // 主要な列のみ表示
  const [showLessColumns, setShowLessColumns] = React.useState(false)
  const handleShowLessColumnsChange = useEvent((e: React.ChangeEvent<HTMLInputElement>) => {
    setShowLessColumns(e.target.checked)
  })

  // グラフの準備ができたときに呼ばれる
  const handleReadyGraph = useEvent(() => {
    if (savedViewStateDefaultValue && savedViewStateDefaultValue.nodePositions && Object.keys(savedViewStateDefaultValue.nodePositions).length > 0) {
      graphViewRef.current?.applyViewState(savedViewStateDefaultValue);
    } else {
      // 保存されたViewStateがない場合や、あってもノード位置情報がない場合は、初期レイアウトを実行
      graphViewRef.current?.resetLayout();
    }
  });

  // EditableGrid表示位置
  const [editableGridPosition, setEditableGridPosition] = React.useState<"vertical" | "horizontal">("horizontal");

  // 選択中のルート集約を画面右側に表示する
  const [selectedRootAggregateIndex, setSelectedRootAggregateIndex] = React.useState<number | undefined>(undefined);
  const selectRootAggregate = useEvent((aggregateId: string) => {
    const index = xmlElementTrees?.findIndex(tree => tree.xmlElements?.[0]?.uniqueId === aggregateId)
    if (index !== undefined && index !== -1) setSelectedRootAggregateIndex(index)
  })

  const handleSelectionChange = useEvent((event: cytoscape.EventObject) => {
    const selectedNodes = event.cy.nodes().filter(node => node.selected());
    if (selectedNodes.length === 0) {
      setSelectedRootAggregateIndex(undefined);
    } else {
      const aggregateId = selectedNodes[0].id();
      const tree = xmlElementTrees?.find(tree => tree.xmlElements?.some(el => el.uniqueId === aggregateId));
      if (!tree) return;
      const aggregateItem = tree.xmlElements?.find(el => el.uniqueId === aggregateId);
      if (!aggregateItem) return;
      const rootAggregateId = asTree(tree.xmlElements).getRoot(aggregateItem)?.uniqueId;
      if (!rootAggregateId) return;

      selectRootAggregate(rootAggregateId)
    }
  });

  // 新規ルート集約の作成
  const { append, remove } = ReactHookForm.useFieldArray({ name: 'xmlElementTrees', control })
  const handleNewRootAggregate = useEvent(() => {
    const localName = prompt('ルート集約名を入力してください。')
    if (!localName) return
    append({ xmlElements: [{ uniqueId: UUID.generate(), indent: 0, localName, attributes: {} }] })
  })

  // 選択中のルート集約の削除
  const handleDeleteRootAggregate = useEvent(() => {
    if (selectedRootAggregateIndex === undefined) return

    const rootAggregateTree = xmlElementTrees?.[selectedRootAggregateIndex]
    const rootAggregateName = rootAggregateTree?.xmlElements?.[0]?.localName || 'ルート集約'

    if (confirm(`「${rootAggregateName}」を削除しますか？この操作は取り消せません。`)) {
      remove(selectedRootAggregateIndex)
      setSelectedRootAggregateIndex(undefined) // 削除後は選択を解除
    }
  })

  // 保存
  const [saveButtonText, setSaveButtonText] = React.useState('保存(Ctrl + S)')
  const [nowSaving, setNowSaving] = React.useState(false)
  const [saveError, setSaveError] = React.useState<string>()
  const handleSave = useEvent(async () => {
    if (nowSaving) return;
    setSaveError(undefined)
    setNowSaving(true)
    const currentValues = getValues()
    const result = await executeSave(currentValues)
    if (result.ok) {
      setSaveButtonText('保存しました。')
      formMethods.reset(currentValues)
      window.setTimeout(() => {
        setSaveButtonText('保存(Ctrl + S)')
      }, 2000)
    } else {
      setSaveError(result.error)
    }
    setNowSaving(false)
  })
  Util.useCtrlS(handleSave)

  return (
    <PageFrame
      title="ソースコード自動生成設定"
      shouldBlock={isDirty}
      headerComponent={(
        <>
          <div className="basis-4"></div>

          <div className="basis-4"></div>
          <label>
            <input type="checkbox" checked={showLessColumns} onChange={handleShowLessColumnsChange} />
            主要な列のみ表示
          </label>
          <div className="basis-4"></div>
          <div className="flex">
            <Input.IconButton fill={editableGridPosition === "horizontal"} outline onClick={() => setEditableGridPosition("horizontal")}>横</Input.IconButton>
            <Input.IconButton fill={editableGridPosition === "vertical"} outline onClick={() => setEditableGridPosition("vertical")}>縦</Input.IconButton>
          </div>
          <div className="flex-1"></div>
          <Input.IconButton icon={Icon.PlusIcon} outline onClick={handleNewRootAggregate}>新規作成</Input.IconButton>
          <Input.IconButton
            icon={Icon.TrashIcon}
            outline
            onClick={handleDeleteRootAggregate}
            disabled={selectedRootAggregateIndex === undefined}
          >
            削除
          </Input.IconButton>
          <Input.IconButton outline onClick={() => alert('区分定義は未実装です。通常の単語型として定義してください。')}>区分定義</Input.IconButton>
          <div className="basis-36 flex justify-end">
            <Input.IconButton fill onClick={handleSave} loading={nowSaving}>{saveButtonText}</Input.IconButton>
          </div>
        </>
      )}
    >
      {saveError && (
        <div className="text-rose-500 text-sm">
          {saveError}
        </div>
      )}
      <PanelGroup className="flex-1" direction={editableGridPosition}>
        <Panel className="border border-gray-300">
          <AppSchemaDefinitionGraph
            onlyRootDefaultValue={onlyRootDefaultValue}
            graphViewRef={graphViewRef}
            xmlElementTrees={xmlElementTrees}
            handleReadyGraph={handleReadyGraph}
            handleSelectionChange={handleSelectionChange}
            triggerSaveLayout={triggerSaveLayout}
            clearSavedLayout={clearSavedLayout}
          />
        </Panel>

        <PanelResizeHandle className={editableGridPosition === "horizontal" ? "w-1" : "h-1"} />

        <Panel collapsible minSize={10}>
          <PanelGroup className="h-full" direction="vertical">
            <Panel>
              {selectedRootAggregateIndex !== undefined && (
                <PageRootAggregate
                  key={selectedRootAggregateIndex} // 選択中のルート集約が変更されたタイミングで再描画
                  rootAggregateIndex={selectedRootAggregateIndex}
                  formMethods={formMethods}
                  getValidationResult={getValidationResult}
                  trigger={trigger}
                  attributeDefs={attributeDefsMap}
                  showLessColumns={showLessColumns}
                  className="h-full"
                />
              )}
            </Panel>

            <PanelResizeHandle className="h-1" />

            <Panel defaultSize={20} minSize={8} collapsible>
              <NijoUiErrorMessagePane
                getValues={getValues}
                validationResultList={validationResultList}
                selectRootAggregate={selectRootAggregate}
                className="h-full"
              />
            </Panel>
          </PanelGroup>
        </Panel>
      </PanelGroup>
    </PageFrame>
  )
}


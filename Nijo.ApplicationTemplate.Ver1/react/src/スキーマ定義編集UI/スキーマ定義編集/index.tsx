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
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import { PageRootAggregate } from "./index.Grid"
import { UUID } from "uuidjs"
import { PageFrame } from "../PageFrame"
import { useValidation } from "./useValidation"
import NijoUiErrorMessagePane from "./index.ErrorMessage"
import { AppSchemaDefinitionGraph } from "./index.Graph"
import { useSaveLoad } from "./useSaveLoad"

export const NijoUiAggregateDiagram = () => {

  const {
    schema,
    loadError,
    reloadSchema,
    saveSchema,
  } = useSaveLoad()

  // 読み込みエラー
  if (loadError) {
    return (
      <div>
        読み込みでエラーが発生しました: {loadError}
        <Input.IconButton onClick={reloadSchema}>
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
      formDefaultValues={schema}
      executeSave={saveSchema}
    />
  )
}

const AfterLoaded = ({ formDefaultValues, executeSave }: {
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
            xmlElementTrees={xmlElementTrees}
            graphViewRef={graphViewRef}
            handleSelectionChange={handleSelectionChange}
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


import React from "react"
import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../input"
import * as Layout from "../../layout"
import * as Util from "../../util"
import * as Icon from "@heroicons/react/24/solid"
import { GraphView, GraphViewRef } from "../../layout/GraphView"
import * as ReactRouter from "react-router-dom"
import { XmlElementItem, ATTR_TYPE, TYPE_DATA_MODEL, TYPE_COMMAND_MODEL, TYPE_QUERY_MODEL, TYPE_CHILD, TYPE_CHILDREN, ATTR_GENERATE_DEFAULT_QUERY_MODEL, TYPE_STATIC_ENUM_MODEL, TYPE_VALUE_OBJECT_MODEL, SchemaDefinitionGlobalState } from "./types"
import { CytoscapeDataSet, ViewState } from "../../layout/GraphView/Cy"
import { Node as CyNode, Edge as CyEdge } from "../../layout/GraphView/DataSource"
import * as AutoLayout from "../../layout/GraphView/Cy.AutoLayout"
import { findRefToTarget } from "./findRefToTarget"
import { asTree } from "./types"
import { SERVER_DOMAIN } from "../../routes"
import cytoscape from 'cytoscape'; // cytoscapeの型情報をインポート
import { useLayoutSaving } from './useLayoutSaving';
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import { PageRootAggregate } from "./index.Grid"
import { UUID } from "uuidjs"
import { PageFrame } from "../PageFrame"
import { useValidation } from "./useValidation"
import NijoUiErrorMessagePane from "./index.ErrorMessage"
import { MentionUtil } from '../UI'

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

  // ルート集約のみ表示の状態
  const [onlyRoot, setOnlyRoot] = React.useState(onlyRootDefaultValue)
  const handleOnlyRootChange = useEvent((e: React.ChangeEvent<HTMLInputElement>) => {
    setOnlyRoot(e.target.checked)
  })

  // 主要な列のみ表示
  const [showLessColumns, setShowLessColumns] = React.useState(false)
  const handleShowLessColumnsChange = useEvent((e: React.ChangeEvent<HTMLInputElement>) => {
    setShowLessColumns(e.target.checked)
  })

  const dataSet: CytoscapeDataSet = React.useMemo(() => {
    if (!xmlElementTrees) return { nodes: {}, edges: [] }

    const nodes: Record<string, CyNode> = {}
    const edges: { source: string, target: string, label: string, sourceModel: string, isMention?: boolean }[] = []

    // メンション情報からターゲットIDを取得する関数
    const getMentionTargets = (element: XmlElementItem): string[] => {
      const targets: string[] = []

      // commentからメンション情報を解析
      if (element.comment) {
        const parts = MentionUtil.parseAsMentionText(element.comment)
        for (const part of parts) {
          if (part.isMention) {
            targets.push(part.targetId)
          }
        }
      }

      return targets
    }

    // 全要素のIDマップを作成（メンション解決用）
    const elementIdMap = new Map<string, { element: XmlElementItem, rootElement: XmlElementItem }>()
    for (const rootAggregateGroup of xmlElementTrees) {
      if (!rootAggregateGroup || !rootAggregateGroup.xmlElements || rootAggregateGroup.xmlElements.length === 0) continue;
      const rootElement = rootAggregateGroup.xmlElements[0];
      if (!rootElement) continue;

      for (const element of rootAggregateGroup.xmlElements) {
        elementIdMap.set(element.uniqueId, { element, rootElement })
      }
    }

    for (const rootAggregateGroup of xmlElementTrees) {
      if (!rootAggregateGroup || !rootAggregateGroup.xmlElements || rootAggregateGroup.xmlElements.length === 0) continue;
      const rootElement = rootAggregateGroup.xmlElements[0];
      if (!rootElement) continue;

      // enum, valueObject を除いて表示
      const model = rootElement.attributes[ATTR_TYPE]
      if (model === TYPE_STATIC_ENUM_MODEL || model === TYPE_VALUE_OBJECT_MODEL) continue;

      const treeHelper = asTree(rootAggregateGroup.xmlElements); // ツリーヘルパーを初期化

      const addMembersRecursively = (owner: XmlElementItem, parentId: string | undefined) => {
        // ルート要素（ルート集約以外も表示する場合は Child, Children含む）のみ表示
        const type = owner.attributes[ATTR_TYPE];
        if (onlyRoot) {
          if (owner.indent !== 0) return;
        } else {
          if (owner.indent !== 0 && type !== TYPE_CHILD && type !== TYPE_CHILDREN) return;
        }

        // ダイアグラムノードを追加
        let bgColor: string | undefined = undefined
        let borderColor: string | undefined = undefined
        if (model === TYPE_DATA_MODEL) {
          bgColor = borderColor = '#ea580c' // orange-600
        } else if (model === TYPE_COMMAND_MODEL) {
          bgColor = borderColor = '#0284c7' // sky-600
        } else if (model === TYPE_QUERY_MODEL) {
          bgColor = borderColor = '#059669' // emerald-600
        }
        nodes[owner.uniqueId] = {
          id: owner.uniqueId,
          label: owner.localName ?? '',
          parent: parentId,
          "background-color": bgColor,
          "border-color": borderColor,
        } satisfies CyNode;

        // owner要素自身のメンション処理
        const ownerMentionTargets = getMentionTargets(owner)
        for (const mentionTargetId of ownerMentionTargets) {
          const mentionTarget = elementIdMap.get(mentionTargetId)
          if (mentionTarget) {
            const mentionTargetUniqueId = onlyRoot
              ? mentionTarget.rootElement.uniqueId
              : mentionTarget.element.uniqueId

            // メンションエッジを追加（自分自身への参照は除く）
            if (owner.uniqueId !== mentionTargetUniqueId) {
              edges.push({
                source: owner.uniqueId,
                target: mentionTargetUniqueId,
                label: ``,
                sourceModel: model,
                isMention: true,
              })
            }
          }
        }

        // 子要素を再帰的に処理し、ref-toエッジを収集。
        // ルート集約のみ表示の場合は、直近の子のみならず、孫要素のref-toも収集する
        const members = onlyRoot
          ? treeHelper.getDescendants(owner)
          : treeHelper.getChildren(owner)

        for (const member of members) {
          // 外部参照でない場合はここでundefinedになる
          const target = findRefToTarget(member, xmlElementTrees)
          const targetUniqueId = onlyRoot
            ? target?.refToRoot?.uniqueId
            : target?.refTo?.uniqueId

          // ダイアグラムエッジを追加。
          // 重複するエッジは最後にまとめてグルーピングする
          if (targetUniqueId && owner.uniqueId !== targetUniqueId) {
            edges.push({
              source: owner.uniqueId,
              target: targetUniqueId,
              label: member.localName ?? '',
              sourceModel: model,
            })
          }

          // メンション情報に基づくエッジの作成
          const mentionTargets = getMentionTargets(member)
          for (const mentionTargetId of mentionTargets) {
            const mentionTarget = elementIdMap.get(mentionTargetId)
            if (mentionTarget) {
              const mentionTargetUniqueId = onlyRoot
                ? mentionTarget.rootElement.uniqueId
                : mentionTarget.element.uniqueId

              // メンションエッジを追加（自分自身への参照は除く）
              if (owner.uniqueId !== mentionTargetUniqueId) {
                edges.push({
                  source: owner.uniqueId,
                  target: mentionTargetUniqueId,
                  label: '',
                  sourceModel: model,
                  isMention: true,
                })
              }
            }
          }

          // 再帰的に子孫要素を処理 (XML構造上の子)
          if (!onlyRoot) addMembersRecursively(member, owner.uniqueId)
        }
      };
      addMembersRecursively(rootElement, undefined);
    }

    // 重複するエッジのグルーピング
    const groupedEdges = edges.reduce((acc, { source, target, label, sourceModel, isMention }) => {
      const existingEdge = acc.find(e => e.source === source && e.target === target)
      if (existingEdge) {
        existingEdge.labels.push(label)
        if (isMention) existingEdge.isMention = true
      } else {
        acc.push({ source, target, labels: [label], sourceModel, isMention })
      }
      return acc
    }, [] as { source: string, target: string, labels: string[], sourceModel: string, isMention?: boolean }[])
    const cyEdges: CyEdge[] = groupedEdges.map(group => {
      const label = group.labels.length === 1 ? group.labels[0] : `${group.labels[0]}など${group.labels.length}件の参照`

      let lineColor: string | undefined = undefined
      if (group.sourceModel === TYPE_DATA_MODEL) {
        lineColor = '#ea580c' // orange-600
      } else if (group.sourceModel === TYPE_COMMAND_MODEL) {
        lineColor = '#0284c7' // sky-600
      } else if (group.sourceModel === TYPE_QUERY_MODEL) {
        lineColor = '#059669' // emerald-600
      }

      return ({
        source: group.source,
        target: group.target,
        label,
        'line-color': lineColor,
        'line-style': group.isMention ? 'dashed' : 'solid',
      } satisfies CyEdge)
    })

    return {
      nodes: nodes,
      edges: cyEdges,
    }
  }, [xmlElementTrees, onlyRoot])

  // 「ルート集約のみ表示」の状態がユーザー操作または上記の復元処理で変更されたときに実行
  React.useEffect(() => {
    // triggerSaveLayout は現在の onlyRoot の値を localStorage に保存する。
    // ノード位置は localStorage 内の既存のものが維持される（NijoUiAggregateDiagram.StateSaving.ts の実装による）。
    triggerSaveLayout(undefined, onlyRoot);
  }, [onlyRoot, triggerSaveLayout]); // onlyRoot または triggerSaveLayout (の参照) が変更されたときに実行

  const handleLayoutChange = useEvent((event: cytoscape.EventObject) => {
    // ドラッグ、パン、ズーム操作完了時に呼ばれる。
    // この event には最新のノード位置、ズーム、パン情報が含まれる。
    triggerSaveLayout(event, onlyRoot);
  });

  const [layoutLogic, setLayoutLogic] = React.useState<AutoLayout.LayoutLogicName>('klay');
  const handleAutoLayout = useEvent(() => {
    // clearSavedLayout は localStorage からすべてのレイアウト情報を削除する。
    clearSavedLayout();
    // その後、現在の layoutLogic でグラフを整列する。
    // resetLayout 内部で viewStateApplied フラグもクリアされる。
    graphViewRef.current?.resetLayout();
  });

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

          <Input.IconButton onClick={handleAutoLayout} outline>
            整列
          </Input.IconButton>
          <select className="border text-sm" value={layoutLogic} onChange={(e) => setLayoutLogic(e.target.value as AutoLayout.LayoutLogicName)}>
            {Object.entries(AutoLayout.OPTION_LIST).map(([key, value]) => (
              <option key={key} value={key}>ロジック: {value.name}</option>
            ))}
          </select>
          <div className="basis-4"></div>
          <label>
            <input type="checkbox" checked={onlyRoot} onChange={handleOnlyRootChange} />
            ルート集約のみ表示
          </label>
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
          <GraphView
            key={onlyRoot ? 'onlyRoot' : 'all'} // このフラグが切り替わったタイミングで全部洗い替え
            ref={graphViewRef}
            nodes={Object.values(dataSet.nodes)} // dataSet.nodesの値を配列として渡す
            edges={dataSet.edges} // dataSet.edgesをそのまま渡す
            parentMap={Object.fromEntries(Object.entries(dataSet.nodes).filter(([, node]) => node.parent).map(([id, node]) => [id, node.parent!]))} // dataSet.nodesからparentMapを生成
            onReady={handleReadyGraph}
            layoutLogic={layoutLogic}
            onLayoutChange={handleLayoutChange}
            onSelectionChange={handleSelectionChange}
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


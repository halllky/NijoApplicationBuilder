import React from "react"
import useEvent from "react-use-event-hook"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../input"
import * as Icon from "@heroicons/react/24/solid"
import { GraphView, GraphViewRef } from "../../layout/GraphView"
import * as ReactRouter from "react-router-dom"
import { SchemaDefinitionOutletContextType, XmlElementItem, ATTR_TYPE, TYPE_DATA_MODEL, TYPE_COMMAND_MODEL, TYPE_QUERY_MODEL, TYPE_CHILD, TYPE_CHILDREN, ATTR_GENERATE_DEFAULT_QUERY_MODEL, TYPE_STATIC_ENUM_MODEL, TYPE_VALUE_OBJECT_MODEL } from "./types"
import { CytoscapeDataSet, ViewState } from "../../layout/GraphView/Cy"
import { Node as CyNode, Edge as CyEdge } from "../../layout/GraphView/DataSource"
import * as AutoLayout from "../../layout/GraphView/Cy.AutoLayout"
import { findRefToTarget } from "./refResolver"
import { asTree } from "./types"
import { getNavigationUrl } from "../../routes"
import cytoscape from 'cytoscape'; // cytoscapeの型情報をインポート
import { useLayoutSaving } from './NijoUiAggregateDiagram.StateSaving';
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import { PageRootAggregate } from "./RootAggregatePage"
import { UUID } from "uuidjs"

export const NijoUiAggregateDiagram = () => {
  // ノード状態の保存と復元
  const saveLoad = useLayoutSaving();

  const defaultValues = React.useMemo(() => ({
    onlyRoot: saveLoad.savedOnlyRoot ?? false,
    savedViewState: saveLoad.savedViewState ?? {},
  }), [saveLoad.savedOnlyRoot, saveLoad.savedViewState])

  return (
    <AfterLoaded
      triggerSaveLayout={saveLoad.triggerSaveLayout}
      clearSavedLayout={saveLoad.clearSavedLayout}
      defaultValues={defaultValues}
    />
  )
}

const AfterLoaded = ({ triggerSaveLayout, clearSavedLayout, defaultValues }: {
  triggerSaveLayout: ReturnType<typeof useLayoutSaving>["triggerSaveLayout"]
  clearSavedLayout: ReturnType<typeof useLayoutSaving>["clearSavedLayout"]
  defaultValues: {
    onlyRoot: boolean
    savedViewState: Partial<ViewState>
  }
}) => {
  const { executeSave, formMethods } = ReactRouter.useOutletContext<SchemaDefinitionOutletContextType>()
  const { getValues, control } = formMethods
  const xmlElementTrees = getValues("xmlElementTrees")
  const graphViewRef = React.useRef<GraphViewRef>(null)
  const navigate = ReactRouter.useNavigate()

  const [onlyRoot, setOnlyRoot] = React.useState(defaultValues.onlyRoot)

  const dataSet: CytoscapeDataSet = React.useMemo(() => {
    if (!xmlElementTrees) return { nodes: {}, edges: [] }

    const nodes: Record<string, CyNode> = {}
    const edges: { source: string, target: string, label: string, sourceModel: string }[] = []

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

          // 再帰的に子孫要素を処理 (XML構造上の子)
          if (!onlyRoot) addMembersRecursively(member, owner.uniqueId)
        }
      };
      addMembersRecursively(rootElement, undefined);
    }

    // 重複するエッジのグルーピング
    const groupedEdges = edges.reduce((acc, { source, target, label, sourceModel }) => {
      const existingEdge = acc.find(e => e.source === source && e.target === target)
      if (existingEdge) {
        existingEdge.labels.push(label)
      } else {
        acc.push({ source, target, labels: [label], sourceModel })
      }
      return acc
    }, [] as { source: string, target: string, labels: string[], sourceModel: string }[])
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

  const handleNodeDoubleClick = useEvent((event: cytoscape.EventObject) => {
    const clickedNodeId = event.target.id();

    let aggregateId = clickedNodeId;
    // クリックされたノードがルート集約でない場合、ルート集約のIDを探す
    const clickedNodeIsRoot = xmlElementTrees?.some(tree => tree.xmlElements?.[0]?.uniqueId === clickedNodeId);

    if (!clickedNodeIsRoot) {
      for (const tree of xmlElementTrees ?? []) {
        if (!tree.xmlElements) continue;
        const found = tree.xmlElements.find(el => el.uniqueId === clickedNodeId);
        if (found) {
          aggregateId = tree.xmlElements[0]?.uniqueId ?? clickedNodeId; // ルート要素のID
          break;
        }
      }
    }

    const url = getNavigationUrl({ aggregateId });
    navigate(url);
  });

  // グラフの準備ができたときに呼ばれる
  const handleReadyGraph = useEvent(() => {
    if (defaultValues.savedViewState && defaultValues.savedViewState.nodePositions && Object.keys(defaultValues.savedViewState.nodePositions).length > 0) {
      graphViewRef.current?.applyViewState(defaultValues.savedViewState);
    } else {
      // 保存されたViewStateがない場合や、あってもノード位置情報がない場合は、初期レイアウトを実行
      graphViewRef.current?.resetLayout();
    }
  });

  // EditableGrid表示位置
  const [editableGridPosition, setEditableGridPosition] = React.useState<"vertical" | "horizontal">("horizontal");

  // 選択中のルート集約を画面右側に表示する
  const [selectedRootAggregateIndex, setSelectedRootAggregateIndex] = React.useState<number | undefined>(undefined);
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
      const rootAggregateIndex = xmlElementTrees?.findIndex(tree => tree.xmlElements?.[0]?.uniqueId === rootAggregateId);
      if (rootAggregateIndex !== undefined) {
        setSelectedRootAggregateIndex(rootAggregateIndex);
      }
    }
  });

  // 新規ルート集約の作成
  const { append } = ReactHookForm.useFieldArray({ name: 'xmlElementTrees', control })
  const handleNewRootAggregate = useEvent(() => {
    const localName = prompt('ルート集約名を入力してください。')
    if (!localName) return
    append({ xmlElements: [{ uniqueId: UUID.generate(), indent: 0, localName, attributes: {} }] })
  })

  return (
    <div className="h-full flex flex-col">
      <div className="flex flex-wrap items-center p-1 gap-1">
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
          <input type="checkbox" checked={onlyRoot} onChange={(e) => setOnlyRoot(e.target.checked)} />
          ルート集約のみ表示
        </label>
        <div className="basis-4"></div>
        <div className="flex">
          <Input.IconButton fill={editableGridPosition === "horizontal"} outline onClick={() => setEditableGridPosition("horizontal")}>横</Input.IconButton>
          <Input.IconButton fill={editableGridPosition === "vertical"} outline onClick={() => setEditableGridPosition("vertical")}>縦</Input.IconButton>
        </div>
        <div className="flex-1"></div>
        <Input.IconButton icon={Icon.PlusIcon} outline onClick={handleNewRootAggregate}>新規作成</Input.IconButton>
        <Input.IconButton outline onClick={() => alert('未実装')}>区分定義</Input.IconButton>
        <div className="basis-4"></div>
        <Input.IconButton fill onClick={executeSave}>保存</Input.IconButton>
      </div>
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
            onNodeDoubleClick={handleNodeDoubleClick}
            onSelectionChange={handleSelectionChange}
          />
        </Panel>

        <PanelResizeHandle className={editableGridPosition === "horizontal" ? "w-1" : "h-1"} />

        <Panel collapsible minSize={10}>
          {selectedRootAggregateIndex !== undefined && (
            <PageRootAggregate
              key={selectedRootAggregateIndex} // 選択中のルート集約が変更されたタイミングで再描画
              rootAggregateIndex={selectedRootAggregateIndex}
              formMethods={formMethods}
            />
          )}
        </Panel>
      </PanelGroup>
    </div >
  )
}


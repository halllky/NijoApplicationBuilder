import React from "react"
import useEvent from "react-use-event-hook"
import * as Input from "../../input"
import { GraphView, GraphViewRef } from "../../layout/GraphView"
import * as ReactRouter from "react-router-dom"
import { SchemaDefinitionOutletContextType, XmlElementItem, ATTR_TYPE, TYPE_DATA_MODEL, TYPE_COMMAND_MODEL, TYPE_QUERY_MODEL, TYPE_CHILD, TYPE_CHILDREN, ATTR_GENERATE_DEFAULT_QUERY_MODEL } from "./types"
import { CytoscapeDataSet } from "../../layout/GraphView/Cy"
import { Node as CyNode, Edge as CyEdge } from "../../layout/GraphView/DataSource"
import * as AutoLayout from "../../layout/GraphView/Cy.AutoLayout"
import { findRefToTarget } from "./refResolver"
import { asTree } from "./types"
import { getNavigationUrl } from "../routing"
import cytoscape from 'cytoscape'; // cytoscapeの型情報をインポート
import { useLayoutSaving } from './NijoUiAggregateDiagram.StateSaving';

export const NijoUiAggregateDiagram = () => {
  const { formMethods } = ReactRouter.useOutletContext<SchemaDefinitionOutletContextType>()
  const { getValues } = formMethods
  const xmlElementTrees = getValues("xmlElementTrees")
  const graphViewRef = React.useRef<GraphViewRef>(null)
  const [onlyRoot, setOnlyRoot] = React.useState(false)
  const navigate = ReactRouter.useNavigate()

  const dataSet: CytoscapeDataSet = React.useMemo(() => {
    if (!xmlElementTrees) return { nodes: {}, edges: [] }

    const nodes: Record<string, CyNode> = {}
    const edges: { source: string, target: string, label: string, sourceModel: string }[] = []

    for (const rootAggregateGroup of xmlElementTrees) {
      if (!rootAggregateGroup || !rootAggregateGroup.xmlElements || rootAggregateGroup.xmlElements.length === 0) continue;
      const rootElement = rootAggregateGroup.xmlElements[0];
      if (!rootElement) continue;

      // Data, Query, Commandのみ表示
      const model = rootElement.attributes[ATTR_TYPE]
      if (model !== TYPE_DATA_MODEL && model !== TYPE_COMMAND_MODEL && model !== TYPE_QUERY_MODEL) continue;

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
    // console.log(edges.map(e => `${nodes[e.source]?.label} -- ${e.label} --> ${nodes[e.target]?.label}`))
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

  // ノード状態の保存と復元
  const {
    savedOnlyRoot,
    savedViewState,
    triggerSaveLayout,
    clearSavedLayout,
  } = useLayoutSaving();

  // 「ルート集約のみ表示」の復元
  React.useEffect(() => {
    if (savedOnlyRoot) setOnlyRoot(savedOnlyRoot)
  }, [])

  // 「ルート集約のみ表示」の状態が変更されたときにレイアウトを保存
  React.useEffect(() => {
    triggerSaveLayout(undefined /** positionsはlocalStorageの情報を正とする */, onlyRoot)
  }, [onlyRoot, triggerSaveLayout]);

  const handleLayoutChange = useEvent((event: cytoscape.EventObject) => {
    triggerSaveLayout(event, onlyRoot)
  })

  const [layoutLogic, setLayoutLogic] = React.useState('klay')
  const handleAutoLayout = useEvent(() => {
    graphViewRef.current?.applyLayout(layoutLogic)
    clearSavedLayout()
  })

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

  return (
    <div className="h-full flex flex-col">
      <div className="flex flex-wrap items-center p-1 gap-1">
        <Input.IconButton onClick={handleAutoLayout} outline>
          整列
        </Input.IconButton>
        <select className="border text-sm" value={layoutLogic} onChange={(e) => setLayoutLogic(e.target.value)}>
          {Object.entries(AutoLayout.OPTION_LIST).map(([key, value]) => (
            <option key={key} value={key}>ロジック: {value.name}</option>
          ))}
        </select>
        <div className="basis-4"></div>
        <label>
          <input type="checkbox" checked={onlyRoot} onChange={(e) => setOnlyRoot(e.target.checked)} />
          ルート集約のみ表示
        </label>
      </div>
      <div className="flex-1">
        <GraphView
          key={onlyRoot ? 'onlyRoot' : 'all'} // このフラグが切り替わったタイミングで全部洗い替え
          ref={graphViewRef}
          initialDataSet={dataSet}
          initialViewState={savedViewState}
          onLayoutChange={handleLayoutChange}
          onNodeDoubleClick={handleNodeDoubleClick}
        />
      </div>
    </div >
  )
}


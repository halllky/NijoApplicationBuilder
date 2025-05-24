import React from "react"
import useEvent from "react-use-event-hook"
import * as Input from "../../input"
import GraphView, { GraphViewRef } from "../../layout/GraphView"
import * as ReactRouter from "react-router-dom"
import { SchemaDefinitionOutletContextType, XmlElementItem, ATTR_TYPE, TYPE_DATA_MODEL, TYPE_COMMAND_MODEL, TYPE_QUERY_MODEL, TYPE_CHILD, TYPE_CHILDREN } from "./types"
import { CytoscapeDataSet } from "../../layout/GraphView/Cy"
import { Node as CyNode, Edge as CyEdge } from "../../layout/GraphView/DataSource"
import * as AutoLayout from "../../layout/GraphView/Cy.AutoLayout"
import { findRefToTarget } from "./refResolver"
import { asTree } from "./types"

export const NijoUiAggregateDiagram = () => {
  const { formMethods } = ReactRouter.useOutletContext<SchemaDefinitionOutletContextType>()
  const { getValues } = formMethods
  const xmlElementTrees = getValues("xmlElementTrees")
  const graphViewRef = React.useRef<GraphViewRef>(null)
  const [onlyRoot, setOnlyRoot] = React.useState(false)

  const dataSet: CytoscapeDataSet = React.useMemo(() => {
    if (!xmlElementTrees) return { nodes: {}, edges: [] }

    const nodes: Record<string, CyNode> = {}
    const edges: { source: string, target: string, label: string }[] = []

    for (const rootAggregateGroup of xmlElementTrees) {
      if (!rootAggregateGroup || !rootAggregateGroup.xmlElements || rootAggregateGroup.xmlElements.length === 0) continue;
      const rootElement = rootAggregateGroup.xmlElements[0];
      if (!rootElement) continue;

      // Data, Query, Commandのみ表示
      if (rootElement.attributes[ATTR_TYPE] !== TYPE_DATA_MODEL
        && rootElement.attributes[ATTR_TYPE] !== TYPE_COMMAND_MODEL
        && rootElement.attributes[ATTR_TYPE] !== TYPE_QUERY_MODEL) continue;

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
        nodes[owner.uniqueId] = {
          label: owner.localName ?? '',
          parent: parentId,
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
    const groupedEdges = edges.reduce((acc, { source, target, label }) => {
      const existingEdge = acc.find(e => e.source === source && e.target === target)
      if (existingEdge) {
        existingEdge.labels.push(label)
      } else {
        acc.push({ source, target, labels: [label] })
      }
      return acc
    }, [] as { source: string, target: string, labels: string[] }[])
    const cyEdges: CyEdge[] = groupedEdges.map(group => ({
      source: group.source,
      target: group.target,
      label: group.labels.length === 1 ? group.labels[0] : `${group.labels[0]}など${group.labels.length}件の参照`,
    } satisfies CyEdge))

    return {
      nodes: nodes,
      edges: cyEdges,
    }
  }, [xmlElementTrees, onlyRoot])

  const [layoutLogic, setLayoutLogic] = React.useState('klay')
  const handleAutoLayout = useEvent(() => {
    graphViewRef.current?.applyLayout(layoutLogic)
  })

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
          ref={graphViewRef}
          initialDataSet={dataSet}
        />
      </div>
    </div >
  )
}


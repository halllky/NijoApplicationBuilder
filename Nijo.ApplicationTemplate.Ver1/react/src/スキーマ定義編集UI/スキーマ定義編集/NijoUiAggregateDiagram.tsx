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

  const dataSet: CytoscapeDataSet = React.useMemo(() => {
    if (!xmlElementTrees) return { nodes: {}, edges: [] }

    const nodes: Record<string, CyNode> = {}

    // 重複するエッジをグルーピングするためのMap。
    // 第1キーはsource, 第2キーはtarget, 値はlabels
    const edges: Map<string, Map<string, string[]>> = new Map()

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
        // ルート要素、 Child, Childrenのみ表示
        const type = owner.attributes[ATTR_TYPE];
        if (owner.indent !== 0 && type !== TYPE_CHILD && type !== TYPE_CHILDREN) return;

        // ダイアグラムノードを追加
        nodes[owner.uniqueId] = {
          label: owner.localName ?? '',
          parent: parentId,
        } satisfies CyNode;

        // 子要素を再帰的に処理し、ref-toエッジを収集
        for (const member of treeHelper.getChildren(owner)) {
          // 外部参照でない場合はここでundefinedになる
          const target = findRefToTarget(member, xmlElementTrees);
          if (target && owner.uniqueId !== target.uniqueId) {
            // ダイアグラムエッジを追加。
            // 重複するエッジは最後にまとめてグルーピングする
            const first = edges.get(owner.uniqueId)
            const second = first?.get(target.uniqueId)
            if (!first) {
              edges.set(owner.uniqueId, new Map([[target.uniqueId, [member.localName ?? '']]]));
            } else if (!second) {
              first.set(target.uniqueId, [member.localName ?? '']);
            } else {
              second.push(member.localName ?? '');
            }
          }

          // 再帰的に子孫要素を処理 (XML構造上の子)
          addMembersRecursively(member, owner.uniqueId);
        }
      };
      addMembersRecursively(rootElement, undefined);
    }

    // 重複するエッジのグルーピング
    const cyEdges: CyEdge[] = Array.from(edges.entries()).flatMap(([source, targetMap]) => {
      return Array.from(targetMap.entries()).map(([target, labels]) => ({
        source: source,
        target: target,
        label: labels.length === 1
          ? labels[0]
          : `${labels[0]}など${labels.length}件の参照`,
      }));
    });

    return {
      nodes: nodes,
      edges: cyEdges,
    }
  }, [xmlElementTrees])

  const [layoutLogic, setLayoutLogic] = React.useState('klay')
  const handleAutoLayout = useEvent(() => {
    graphViewRef.current?.applyLayout(layoutLogic)
  })

  const handleSelectAll = useEvent(() => {
    graphViewRef.current?.selectAll()
  })

  const handleCollapseSelection = useEvent(() => {
    graphViewRef.current?.collapseSelections()
  })

  const handleExpandSelection = useEvent(() => {
    graphViewRef.current?.expandSelections()
  })

  return (
    <div className="h-full flex flex-col">
      <div className="flex flex-wrap p-1 gap-1">
        <select className="border text-sm" value={layoutLogic} onChange={(e) => setLayoutLogic(e.target.value)}>
          {Object.entries(AutoLayout.OPTION_LIST).map(([key, value]) => (
            <option key={key} value={key}>{value.name}</option>
          ))}
        </select>
        <Input.IconButton onClick={handleAutoLayout} outline>
          整列
        </Input.IconButton>
        <div className="basis-4"></div>
        <Input.IconButton onClick={handleSelectAll} outline>
          全選択
        </Input.IconButton>
        <Input.IconButton onClick={handleCollapseSelection} outline>
          選択を折りたたむ
        </Input.IconButton>
        <Input.IconButton onClick={handleExpandSelection} outline>
          選択を展開
        </Input.IconButton>
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


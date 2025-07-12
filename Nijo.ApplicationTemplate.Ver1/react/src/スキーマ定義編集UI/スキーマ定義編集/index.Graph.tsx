import React from "react"
import useEvent from "react-use-event-hook"
import { GraphView, GraphViewRef, } from "../../layout"
import { Node as CyNode, Edge as CyEdge } from "../../layout/GraphView/DataSource"
import { CytoscapeDataSet } from "../../layout/GraphView/Cy"
import { asTree, ATTR_IS_KEY, ATTR_TYPE, ModelPageForm, TYPE_CHILD, TYPE_CHILDREN, TYPE_COMMAND_MODEL, TYPE_DATA_MODEL, TYPE_QUERY_MODEL, TYPE_STATIC_ENUM_MODEL, TYPE_VALUE_OBJECT_MODEL, XmlElementItem } from "./types"
import { MentionUtil } from "../UI"
import { findRefToTarget } from "./findRefToTarget"
import * as AutoLayout from "../../layout/GraphView/Cy.AutoLayout"
import * as Input from "../../input"
import { useLayoutSaving, DisplayMode, LOCAL_STORAGE_KEY_DISPLAY_MODE } from './index.Graph.useLayoutSaving'

export const AppSchemaDefinitionGraph = ({
  xmlElementTrees,
  graphViewRef,
  handleSelectionChange,
}: {
  xmlElementTrees: ModelPageForm[]
  graphViewRef: React.RefObject<GraphViewRef | null>
  handleSelectionChange: (event: cytoscape.EventObject) => void
}) => {

  // displayModeの初期値を保存された値から取得
  const [displayMode, setDisplayMode] = React.useState<DisplayMode>(() => {
    const saved = localStorage.getItem(LOCAL_STORAGE_KEY_DISPLAY_MODE)
    return saved === 'er' ? 'er' : 'schema' // デフォルトは'schema'
  })

  // レイアウト保存機能（displayModeに応じて）
  const { triggerSaveLayout, clearSavedLayout, savedOnlyRoot, savedViewState, saveDisplayMode } = useLayoutSaving(displayMode)
  const handleDisplayModeChange = useEvent((e: React.ChangeEvent<HTMLSelectElement>) => {
    const newMode = e.target.value as DisplayMode
    setDisplayMode(newMode)
    saveDisplayMode(newMode)
  })

  // ルート集約のみ表示の状態
  const [onlyRoot, setOnlyRoot] = React.useState(savedOnlyRoot ?? false)
  const handleOnlyRootChange = useEvent((e: React.ChangeEvent<HTMLInputElement>) => {
    setOnlyRoot(e.target.checked)
  })

  // 整列ロジックの状態
  const [layoutLogic, setLayoutLogic] = React.useState<AutoLayout.LayoutLogicName>('klay');
  const handleAutoLayout = useEvent(() => {
    // clearSavedLayout は localStorage からすべてのレイアウト情報を削除する。
    clearSavedLayout();
    // その後、現在の layoutLogic でグラフを整列する。
    // resetLayout 内部で viewStateApplied フラグもクリアされる。
    graphViewRef.current?.resetLayout();
  });

  // レイアウト変更時の処理
  const handleLayoutChange = useEvent((event: cytoscape.EventObject) => {
    // ドラッグ、パン、ズーム操作完了時に呼ばれる。
    // この event には最新のノード位置、ズーム、パン情報が含まれる。
    triggerSaveLayout(event, onlyRoot);
  });

  // 「ルート集約のみ表示」の状態がユーザー操作または上記の復元処理で変更されたときに実行
  React.useEffect(() => {
    // triggerSaveLayout は現在の onlyRoot の値を localStorage に保存する。
    // ノード位置は localStorage 内の既存のものが維持される（NijoUiAggregateDiagram.StateSaving.ts の実装による）。
    triggerSaveLayout(undefined, onlyRoot);
  }, [onlyRoot, triggerSaveLayout]); // onlyRoot または triggerSaveLayout (の参照) が変更されたときに実行

  // グラフの準備ができたときに呼ばれる処理を拡張
  const handleReadyGraph = useEvent(() => {
    if (savedViewState && savedViewState.nodePositions && Object.keys(savedViewState.nodePositions).length > 0) {
      graphViewRef.current?.applyViewState(savedViewState);
    } else {
      // 保存されたViewStateがない場合や、あってもノード位置情報がない場合は、初期レイアウトを実行
      graphViewRef.current?.resetLayout();
    }
  });


  const dataSet: CytoscapeDataSet = React.useMemo(() => {
    if (!xmlElementTrees) return { nodes: {}, edges: [] }

    if (displayMode === 'er') {
      // ER図表示モード
      return createERDiagramDataSet(xmlElementTrees)
    } else {
      // スキーマ定義モード（既存の処理）
      return createSchemaDefinitionDataSet(xmlElementTrees, onlyRoot)
    }
  }, [xmlElementTrees, onlyRoot, displayMode])

  return (
    <div className="h-full relative">
      <GraphView
        key={`${displayMode}-${onlyRoot ? 'onlyRoot' : 'all'}`} // モードとフラグが切り替わったタイミングで全部洗い替え
        ref={graphViewRef}
        nodes={Object.values(dataSet.nodes)} // dataSet.nodesの値を配列として渡す
        edges={dataSet.edges} // dataSet.edgesをそのまま渡す
        parentMap={Object.fromEntries(Object.entries(dataSet.nodes).filter(([, node]) => node.parent).map(([id, node]) => [id, node.parent!]))} // dataSet.nodesからparentMapを生成
        onReady={handleReadyGraph}
        layoutLogic={layoutLogic}
        onLayoutChange={handleLayoutChange}
        onSelectionChange={handleSelectionChange}
        className="h-full"
      />

      {/* スキーマ定義グラフ表示オプション */}
      <div className="flex flex-col items-start gap-2 p-1 absolute top-0 left-0">
        <div className="flex items-center gap-2">
          <label className="text-sm font-medium">表示モード:</label>
          <select className="border text-sm bg-white" value={displayMode} onChange={handleDisplayModeChange}>
            <option value="schema">スキーマ定義</option>
            <option value="er">ER図</option>
          </select>
        </div>
        {displayMode === 'schema' && (
          <label className="flex items-center gap-1">
            <input type="checkbox" checked={onlyRoot} onChange={handleOnlyRootChange} />
            ルート集約のみ表示
          </label>
        )}
        <div className="flex items-center gap-2">
          <Input.IconButton onClick={handleAutoLayout} outline mini className="bg-white">
            整列
          </Input.IconButton>
          <select className="border text-sm bg-white" value={layoutLogic} onChange={(e) => setLayoutLogic(e.target.value as AutoLayout.LayoutLogicName)}>
            {Object.entries(AutoLayout.OPTION_LIST).map(([key, value]) => (
              <option key={key} value={key}>ロジック: {value.name}</option>
            ))}
          </select>
        </div>
      </div>
    </div>
  )
}

// ---------------------------------------------

//#region スキーマ定義

// スキーマ定義モードのdataSet作成関数
const createSchemaDefinitionDataSet = (xmlElementTrees: ModelPageForm[], onlyRoot: boolean): CytoscapeDataSet => {
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
}

//#endregion

// ---------------------------------------------

//#region ER図

// ER図表示モードのdataSet作成関数
const createERDiagramDataSet = (xmlElementTrees: ModelPageForm[]): CytoscapeDataSet => {
  const nodes: Record<string, CyNode> = {}
  const edges: { source: string, target: string, label: string, sourceModel: string, isMention?: boolean }[] = []

  // ルート集約のツリーを取得する関数
  const getTreeFromRootElement = (rootElement: XmlElementItem) => {
    const rootAggregateGroup = xmlElementTrees.find(element => element.xmlElements[0]?.localName === rootElement.localName)
    if (!rootAggregateGroup) return undefined
    return asTree(rootAggregateGroup.xmlElements)
  }

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

  // 全テーブルの主キーを収集
  const primaryKeys: Map<XmlElementItem, XmlElementItem[]> = new Map()
  for (const rootAggregateGroup of xmlElementTrees) {
    if (!rootAggregateGroup || !rootAggregateGroup.xmlElements || rootAggregateGroup.xmlElements.length === 0) continue;
    const rootElement = rootAggregateGroup.xmlElements[0];
    if (!rootElement) continue;

    // ツリーヘルパーを初期化
    const treeHelper = getTreeFromRootElement(rootElement);
    if (!treeHelper) continue;

    for (const element of rootAggregateGroup.xmlElements) {
      const pks: XmlElementItem[] = []
      // 親のキー
      const parent = treeHelper.getParent(element)
      if (parent) {
        const parentPks = primaryKeys.get(parent)
        if (parentPks) pks.push(...parentPks)
      }

      // 自身のキー
      const children = treeHelper.getChildren(element)
      const ownPks = children.filter(child => child.attributes[ATTR_IS_KEY])
      pks.push(...ownPks)
      primaryKeys.set(element, pks)
    }
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

    // TYPE_DATA_MODEL のみ表示
    const model = rootElement.attributes[ATTR_TYPE]
    if (model !== TYPE_DATA_MODEL) continue;

    const treeHelper = getTreeFromRootElement(rootElement); // ツリーヘルパーを初期化
    if (!treeHelper) continue;

    const addTableRecursively = (owner: XmlElementItem) => {
      // ルート集約、child、childrenのみ表示
      const type = owner.attributes[ATTR_TYPE];
      if (owner.indent !== 0 && type !== TYPE_CHILD && type !== TYPE_CHILDREN) return;

      // テーブルのカラムを収集
      const columns: string[] = []
      const members = treeHelper.getChildren(owner)

      // 子テーブルは親テーブルの主キーを継承する
      const parent = treeHelper.getParent(owner)
      if (parent) {
        const pks = primaryKeys.get(parent)
        if (pks) {
          for (const pk of pks) {
            columns.push(`Parent_${pk.localName ?? ''} (PK)`)
          }
        }
      }

      // 自身のメンバー。
      // 値型の場合はそのままカラムとして認識する。
      // 外部キーの場合は相手方の主キーをカラムとして追加する。
      for (const member of members) {
        const target = findRefToTarget(member, xmlElementTrees)
        if (!target?.refTo) {
          // 値要素またはプリミティブ型の場合はそのままカラムとして認識する。
          const isPk = member.attributes[ATTR_IS_KEY]
          columns.push(isPk
            ? `${member.localName ?? ''} (PK)`
            : member.localName ?? '')

        } else {
          // 外部キーの場合は相手方の主キーをカラムとして追加する。
          const refToTree = getTreeFromRootElement(target.refToRoot)
          if (!refToTree) continue;
          const isPk = member.attributes[ATTR_IS_KEY]
          const refToPks = primaryKeys.get(target.refTo)
          if (refToPks) {
            for (const refToPk of refToPks) {
              columns.push(isPk
                ? `${member.localName ?? ''}_${refToPk.localName ?? ''} (PK)`
                : `${member.localName ?? ''}_${refToPk.localName ?? ''}`)
            }
          }
        }
      }

      // ダイアグラムノードを追加（テーブル）
      nodes[owner.uniqueId] = {
        id: owner.uniqueId,
        label: owner.localName ?? '',
        members: columns, // カラム情報

        // data-modelの色にあわせる
        "background-color": '#ea580c', // orange-600
        "border-color": '#ea580c', // orange-600
      } satisfies CyNode;

      // owner要素自身のメンション処理
      const ownerMentionTargets = getMentionTargets(owner)
      for (const mentionTargetId of ownerMentionTargets) {
        const mentionTarget = elementIdMap.get(mentionTargetId)
        if (mentionTarget) {
          const mentionTargetUniqueId = mentionTarget.element.uniqueId

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

      // 子から親へのエッジ
      if (parent) {
        edges.push({
          source: owner.uniqueId,
          target: parent.uniqueId,
          label: `(Parent)`,
          sourceModel: model,
        })
      }

      // 外部キー関係のエッジを処理
      for (const member of members) {
        // 外部参照がある場合は外部キーとして処理
        const target = findRefToTarget(member, xmlElementTrees)
        const targetUniqueId = target?.refTo?.uniqueId

        // 外部キーエッジを追加
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
            const mentionTargetUniqueId = mentionTarget.element.uniqueId

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
      }

      // 再帰的に子孫要素を処理 (XML構造上の子)
      for (const member of members) {
        addTableRecursively(member)
      }
    };
    addTableRecursively(rootElement);
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

    // ER図では外部キーは実線、メンションは破線
    return ({
      source: group.source,
      target: group.target,
      label,
      'line-color': '#6c757d', // gray
      'line-style': group.isMention ? 'dashed' : 'solid',
    } satisfies CyEdge)
  })

  return {
    nodes: nodes,
    edges: cyEdges,
  }
}
//#endregion

import React from "react";
import * as ReactHookForm from "react-hook-form";
import { EditingProject, EditingRootAggregate, EditingMember, ATTR_PARAMETER, ATTR_RETURN_VALUE, MODEL_DATA, MODEL_QUERY, MODEL_COMMAND } from "../../backend";
import { GraphView2 } from "@nijo/ui-components";
import { parseAsMentionText } from "../../UI/Mention";
import { findRefToTarget } from "../findRefToTarget";
import { asTree } from "../../asTree";

export type NodeMetadata = {
  rootAggregateUniqueId: string
}

/**
 * ダイアグラム用のデータセット算出。
 * ダイアグラムに表示するのはデータ構造・コマンドのルート集約のみ
 * （静的区分・値オブジェクト・定数はそれぞれ専用タブで編集するため対象外）。
 */
export function useDiagramDataSet(formMethods: ReactHookForm.UseFormReturn<EditingProject>): {
  nodes: GraphView2.Node[]
  edges: GraphView2.Edge[]
} {
  const dataStructures = formMethods.watch('dataStructures') || [];
  const commands = formMethods.watch('commands') || [];
  // メンション解決のためには、専用タブで編集される種類のルート集約も対象に含める
  const staticEnums = formMethods.watch('staticEnums') || [];
  const valueObjects = formMethods.watch('valueObjects') || [];
  const constants = formMethods.watch('constants') || [];

  return React.useMemo(() => {
    const nodes: Record<string, GraphView2.Node> = {}
    const edges: { source: string, target: string, label: string, sourceModel: string | null | undefined, isMention?: boolean }[] = []

    // メンション情報からターゲットIDを取得する関数
    const getMentionTargets = (comment: string | null | undefined): string[] => {
      return parseAsMentionText(comment ?? '')
        .filter(part => part.isMention)
        .map(part => part.targetId)
    }

    // 全要素のIDマップを作成（メンション解決・Parameter/ReturnValue解決用）
    const elementIdMap = new Map<string, { uniqueId: string, physicalName: string | null | undefined }>()
    for (const list of [dataStructures, commands, staticEnums, valueObjects, constants]) {
      for (const root of list) {
        elementIdMap.set(root.uniqueId, { uniqueId: root.uniqueId, physicalName: root.physicalName })
        for (const member of root.members) {
          elementIdMap.set(member.uniqueId, { uniqueId: member.uniqueId, physicalName: member.physicalName })
        }
      }
    }

    for (const root of [...dataStructures, ...commands]) {
      const model = root.model
      const treeHelper = asTree(root.members, m => m.uniqueId)

      // ownerの直属メンバーを取得する。ownerがルート自身の場合、indent1の要素は必ずルート直属なのでasTreeは使わない。
      const getChildrenOf = (owner: EditingRootAggregate | EditingMember): EditingMember[] => {
        return 'indent' in owner ? treeHelper.getChildren(owner) : root.members.filter(m => m.indent === 1)
      }

      const addMembersRecursively = (owner: EditingRootAggregate | EditingMember, parentId: string | undefined) => {
        const isMember = 'indent' in owner

        // 処理対象外（ルート自身、またはchild/childrenメンバーのみノード化・再帰する）
        if (isMember && owner.type.kind !== 'child' && owner.type.kind !== 'children') {
          return;
        }

        // ダイアグラムノードを追加
        let bgColor: string | undefined = undefined
        let borderColor: string | undefined = undefined
        if (model === MODEL_DATA) {
          bgColor = borderColor = '#ea580c' // orange-600
        } else if (model === MODEL_COMMAND) {
          bgColor = borderColor = '#0284c7' // sky-600
        } else if (model === MODEL_QUERY) {
          bgColor = borderColor = '#059669' // emerald-600
        }
        nodes[owner.uniqueId] = {
          id: owner.uniqueId,
          label: owner.physicalName ?? '',
          parent: parentId,
          "background-color": bgColor,
          "border-color": borderColor,
          meta: {
            rootAggregateUniqueId: root.uniqueId,
          } satisfies NodeMetadata,
        };

        // owner要素自身のメンション処理
        for (const mentionTargetId of getMentionTargets(owner.comment)) {
          const mentionTarget = elementIdMap.get(mentionTargetId)
          // メンションエッジを追加（自分自身への参照は除く）
          if (mentionTarget && owner.uniqueId !== mentionTarget.uniqueId) {
            edges.push({
              source: owner.uniqueId,
              target: mentionTarget.uniqueId,
              label: ``,
              sourceModel: model,
              isMention: true,
            })
          }
        }

        // コマンドモデルの場合、ParameterとReturnValue属性で定義されている物理名のノードにエッジを追加
        if (model === MODEL_COMMAND) {
          // Parameter属性の処理
          // ※QueryModelを参照する場合は「xxxxx:DisplayData」のようにコロンの前が物理名
          const parameterAttr = owner.attributes[ATTR_PARAMETER]
          const parameterValue = typeof parameterAttr === 'string' ? parameterAttr.split(':')[0] : undefined
          if (parameterValue) {
            const targetElement = Array.from(elementIdMap.values()).find(el => el.physicalName === parameterValue)
            if (targetElement && owner.uniqueId !== targetElement.uniqueId) {
              edges.push({
                source: owner.uniqueId,
                target: targetElement.uniqueId,
                label: '引数',
                sourceModel: model,
              })
            }
          }

          // ReturnValue属性の処理
          const returnValueAttr = owner.attributes[ATTR_RETURN_VALUE]
          const returnValue = typeof returnValueAttr === 'string' ? returnValueAttr.split(':')[0] : undefined
          if (returnValue) {
            const targetElement = Array.from(elementIdMap.values()).find(el => el.physicalName === returnValue)
            if (targetElement && owner.uniqueId !== targetElement.uniqueId) {
              edges.push({
                source: owner.uniqueId,
                target: targetElement.uniqueId,
                label: '戻り値',
                sourceModel: model,
              })
            }
          }
        }

        // 子要素を再帰的に処理し、ref-toエッジを収集。
        // ルート集約のみ表示の場合は、直近の子のみならず、孫要素のref-toも収集する
        const members = getChildrenOf(owner)

        for (const member of members) {
          // 外部参照でない場合はここでundefinedになる
          const target = findRefToTarget(member, dataStructures)
          const targetUniqueId = target?.refTo?.uniqueId

          // ダイアグラムエッジを追加。
          // 重複するエッジは最後にまとめてグルーピングする
          if (targetUniqueId && owner.uniqueId !== targetUniqueId) {
            edges.push({
              source: owner.uniqueId,
              target: targetUniqueId,
              label: member.physicalName ?? '',
              sourceModel: model,
            })
          }

          // メンション情報に基づくエッジの作成
          for (const mentionTargetId of getMentionTargets(member.comment)) {
            const mentionTarget = elementIdMap.get(mentionTargetId)
            if (mentionTarget && owner.uniqueId !== mentionTarget.uniqueId) {
              edges.push({
                source: owner.uniqueId,
                target: mentionTarget.uniqueId,
                label: '',
                sourceModel: model,
                isMention: true,
              })
            }
          }

          // 再帰的に子孫要素を処理 (XML構造上の子)
          addMembersRecursively(member, owner.uniqueId)
        }
      };
      addMembersRecursively(root, undefined);
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
    }, [] as { source: string, target: string, labels: string[], sourceModel: string | null | undefined, isMention?: boolean }[])
    const cyEdges: GraphView2.Edge[] = groupedEdges.map(group => {
      const label = group.labels.length === 1 ? group.labels[0] : `${group.labels[0]}など${group.labels.length}件の参照`

      let lineColor: string | undefined = undefined
      if (group.sourceModel === MODEL_DATA) {
        lineColor = '#ea580c' // orange-600
      } else if (group.sourceModel === MODEL_COMMAND) {
        lineColor = '#0284c7' // sky-600
      } else if (group.sourceModel === MODEL_QUERY) {
        lineColor = '#059669' // emerald-600
      }

      return {
        source: group.source,
        target: group.target,
        label,
        'line-color': lineColor,
        'line-style': group.isMention ? 'dashed' : 'solid',
        targetEndShape: 'triangle',
      }
    })

    return {
      nodes: Object.values(nodes),
      edges: cyEdges,
    }
  }, [dataStructures, commands, staticEnums, valueObjects, constants]);
}

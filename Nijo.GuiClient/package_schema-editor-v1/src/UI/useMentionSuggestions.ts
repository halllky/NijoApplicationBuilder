import React from "react"
import * as ReactHookForm from "react-hook-form"
import { EditingProject } from "../backend"
import { MentionableTextarea } from "./Mention"

/**
 * スキーマ定義データからメンションの候補リストを取得するカスタムフック
 */
export function useMentionSuggestions(
  getValues: ReactHookForm.UseFormGetValues<EditingProject>
): Parameters<typeof MentionableTextarea>[0]['getSuggestions'] {

  return React.useCallback((query, callback) => {
    const project = getValues()
    if (!project) {
      callback([])
      return
    }

    // メンション対象: データ構造・コマンドのルート集約自身、
    // および全リスト（データ構造・コマンド・静的区分・値オブジェクト・定数）の child, children メンバー
    const targets: { uniqueId: string, physicalName: string | null | undefined }[] = []
    for (const root of [...project.dataStructures, ...project.commands]) {
      targets.push({ uniqueId: root.uniqueId, physicalName: root.physicalName })
    }
    for (const root of [...project.dataStructures, ...project.commands, ...project.staticEnums, ...project.valueObjects, ...project.constants]) {
      for (const member of root.members) {
        if (member.type.kind === 'child' || member.type.kind === 'children') {
          targets.push({ uniqueId: member.uniqueId, physicalName: member.physicalName })
        }
      }
    }

    // クエリに基づいてフィルタリング
    const filtered = targets.filter(el => {
      const physicalName = el.physicalName || ''
      return physicalName.toLowerCase().includes(query.toLowerCase())
    })

    // 提案リストを作成
    const suggestions = filtered.map(el => ({
      id: el.uniqueId,
      display: el.physicalName || '(名前なし)',
    }))

    callback(suggestions)
  }, [getValues])
}

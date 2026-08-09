import React from "react"
import * as ReactHookForm from "react-hook-form"
import { XmlElementItem, ATTR_TYPE, TYPE_DATA_MODEL, TYPE_COMMAND_MODEL, TYPE_QUERY_MODEL, TYPE_CHILD, TYPE_CHILDREN, GeneratedProjectInGui, TYPE_STRUCTURE_MODEL } from "../types"
import { MentionableTextarea } from "./Mention"

/**
 * スキーマ定義データからメンションの候補リストを取得するカスタムフック
 */
export function useMentionSuggestions(
  getValues: ReactHookForm.UseFormGetValues<GeneratedProjectInGui>
): Parameters<typeof MentionableTextarea>[0]['getSuggestions'] {

  return React.useCallback((query, callback) => {
    const schemaDefinitionData = getValues()
    if (!schemaDefinitionData) {
      callback([])
      return
    }

    // 全てのXML要素を収集
    const allElements: XmlElementItem[] = []
    for (const tree of schemaDefinitionData.xmlElementTrees) {
      allElements.push(...tree.xmlElements)
    }

    // ルート集約、child、childrenのみに制限
    const targetElements = allElements.filter(el => {
      const type = el.attributes[ATTR_TYPE]

      // ルート集約
      if (el.indent === 0
        && (type === TYPE_DATA_MODEL
          || type === TYPE_QUERY_MODEL
          || type === TYPE_COMMAND_MODEL
          || type === TYPE_STRUCTURE_MODEL)) return true

      // child または children
      if (type === TYPE_CHILD || type === TYPE_CHILDREN) return true

      return false
    })

    // クエリに基づいてフィルタリング
    const filtered = targetElements.filter(el => {
      const localName = el.localName || ''
      return localName.toLowerCase().includes(query.toLowerCase())
    })

    // 提案リストを作成
    const suggestions = filtered.map(el => ({
      id: el.uniqueId,
      display: el.localName || '(名前なし)',
    }))

    callback(suggestions)
  }, [getValues])
}

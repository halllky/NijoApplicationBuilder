import React from "react"
import { SchemaEditorRule } from "../backend"

const SchemaEditorRuleContext = React.createContext<SchemaEditorRule | null>(null)

/**
 * スキーマ編集画面が必要とする、プロジェクトに依存しない固定ルール（属性定義・値の種類・モデル種別・
 * プロジェクト設定項目のメタ情報）を配信するコンテキスト。
 * 編集対象ではないため react-hook-form のフォームには含めず、これを介して供給する。
 */
export const SchemaEditorRuleProvider = ({ rule, children }: {
  rule: SchemaEditorRule
  children?: React.ReactNode
}) => {
  return (
    <SchemaEditorRuleContext.Provider value={rule}>
      {children}
    </SchemaEditorRuleContext.Provider>
  )
}

export const useSchemaEditorRule = (): SchemaEditorRule => {
  const rule = React.useContext(SchemaEditorRuleContext)
  if (!rule) throw new Error("SchemaEditorRuleProvider の外で useSchemaEditorRule が呼ばれました。")
  return rule
}

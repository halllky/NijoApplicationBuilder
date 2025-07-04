import React from "react"
import * as ReactMention from 'react-mentions'
import * as Icon from "@heroicons/react/24/solid"
import * as Layout from "../../layout"
import { XmlElementItem } from "./types"
import useEvent from "react-use-event-hook"
import { SchemaDefinitionContext, COLUMN_ID_COMMENT } from "./index.Grid"
import { MentionInputWrapper } from "../UI/MentionInputWrapper"

/**
 * メンションを含むセル編集エディタ（スキーマ定義編集用）。
 * コメント列でのみメンション使用可能。
 */
export const CellEditorWithMention = React.forwardRef(({
  value,
  onChange,
  showOptions,
  columnDef,
}: Layout.CellEditorTextareaProps, ref: React.ForwardedRef<Layout.CellEditorTextareaRef>) => {

  const textareaRef = React.useRef<HTMLTextAreaElement>(null);

  React.useImperativeHandle(ref, () => ({
    focus: () => textareaRef.current?.focus(),
    select: () => textareaRef.current?.select(),
    value: value ?? '',
  }), [value, onChange])

  // コメント列かどうかを判断
  const isCommentColumn = columnDef?.columnId === COLUMN_ID_COMMENT

  return (
    <>
      {isCommentColumn ? (
        // コメント列の場合：メンション機能付きテキストエリア
        <SchemaDefinitionMentionTextarea
          ref={textareaRef}
          value={value ?? ''}
          onChange={onChange}
          className="flex-1 mx-[3px] my-[-1px]"
        />
      ) : (
        // それ以外の場合：通常のテキストエリア
        <textarea
          ref={textareaRef}
          value={value ?? ''}
          onChange={e => onChange(e.target.value)}
          className="flex-1 mx-[3px] my-[-1px] outline-none break-all resize-none field-sizing-content"
          spellCheck={false}
        />
      )}
      {showOptions && (
        <Icon.ChevronDownIcon className="w-4 cursor-pointer" />
      )}
    </>
  )
})

/**
 * スキーマ定義編集用メンションテキストエリア
 */
const SchemaDefinitionMentionTextarea = React.forwardRef(({
  value,
  onChange,
  className,
  placeholder,
}: {
  value?: string
  onChange?: (value: string) => void
  className?: string
  placeholder?: string
}, ref: React.Ref<HTMLTextAreaElement>) => {

  // スキーマ定義データを取得
  const schemaDefinitionData = React.useContext(SchemaDefinitionContext)

  const getSuggestions: ReactMention.DataFunc = React.useCallback(async (query, callback) => {
    if (!schemaDefinitionData) {
      callback([])
      return
    }

    // 全てのXML要素を収集
    const allElements: XmlElementItem[] = []
    for (const tree of schemaDefinitionData.xmlElementTrees) {
      allElements.push(...tree.xmlElements)
    }

    // クエリに基づいてフィルタリング
    const filtered = allElements.filter(el => {
      const localName = el.localName || ''
      return localName.toLowerCase().includes(query.toLowerCase())
    })

    // 提案リストを作成
    const suggestions = filtered.map(el => ({
      id: el.uniqueId,
      display: el.localName || '(名前なし)',
    }))

    callback(suggestions)
  }, [schemaDefinitionData])

  return (
    <MentionInputWrapper
      ref={ref}
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      className={className}
      getSuggestions={getSuggestions}
    />
  )
})

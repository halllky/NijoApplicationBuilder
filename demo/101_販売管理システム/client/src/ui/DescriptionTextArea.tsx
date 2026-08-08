import React from "react"

export type DescriptionTextAreaProps = React.TextareaHTMLAttributes<HTMLTextAreaElement> & {
}

/**
 * 文章入力テキストエリア。
 * 通常の `textarea` に加え、以下の機能を持つ。
 *
 * - 基本的な共通レイアウト（以下、例）
 *   - 入力可能なら枠と背景色を変更
 *   - 入力不可能なら枠なし背景色なし
 *   - 内容の縦幅に応じて自動的に高さを調整（もちろん max-height による上限指定可能）
 *   - spellcheck や autocomplete を無効化
 */
export const DescriptionTextArea = React.forwardRef<HTMLTextAreaElement, DescriptionTextAreaProps>((props, ref) => {
  const { className, readOnly, disabled, ...rest } = props

  const baseStyle = "field-sizing-content block py-px px-1 border outline-blue-500 focus:ring-blue-500 resize-none overflow-hidden"
  const editableStyle = "border-gray-700 bg-white"
  const readOnlyStyle = "border-transparent bg-transparent shadow-none cursor-default focus:ring-0"

  const isEditable = !readOnly && !disabled

  return (
    <textarea
      ref={ref}
      readOnly={readOnly}
      disabled={disabled}
      spellCheck={false}
      autoComplete="off"
      className={`${baseStyle} ${isEditable ? editableStyle : readOnlyStyle} ${className ?? ''}`}
      {...rest}
    />
  )
})

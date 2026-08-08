import React from "react"

export type WordTextBoxProps = React.InputHTMLAttributes<HTMLInputElement> & {
}

/**
 * 単語入力テキストボックス。
 * 通常の `input type="text"` に加え、以下の機能を持つ。
 *
 * - 基本的な共通レイアウト（以下、例）
 *   - 入力可能なら枠と背景色を変更
 *   - 入力不可能なら枠なし背景色なし
 *   - spellcheck や autocomplete を無効化
 * - フォーカスアウト時に前後の空白を自動削除
 * - フォーカスアウト時にUnicode正規化（NFKC）を自動実行
 */
export const WordTextBox = React.forwardRef<HTMLInputElement, WordTextBoxProps>((props, ref) => {
  const { className, readOnly, disabled, onBlur, onChange, ...rest } = props

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    let val = e.target.value
    // 前後の空白削除
    val = val.trim()
    // Unicode正規化 (NFKC)
    val = val.normalize('NFKC')

    if (val !== e.target.value) {
      e.target.value = val
      onChange?.(e as any)
    }
    onBlur?.(e)
  }

  const baseStyle = "block py-px px-1 border outline-blue-500 focus:ring-blue-500"
  const editableStyle = "border-gray-700 bg-white"
  const readOnlyStyle = "border-transparent bg-transparent shadow-none cursor-default focus:ring-0"

  const isEditable = !readOnly && !disabled

  return (
    <input
      type="text"
      ref={ref}
      readOnly={readOnly}
      disabled={disabled}
      spellCheck={false}
      autoComplete="off"
      className={`${baseStyle} ${isEditable ? editableStyle : readOnlyStyle} ${className ?? ''}`}
      onBlur={handleBlur}
      onChange={onChange}
      {...rest}
    />
  )
})

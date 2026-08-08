import React from "react"

export type DateInputProps = React.InputHTMLAttributes<HTMLInputElement> & {
  /** 外観。未指定の場合は "date" */
  appearance?: "yearmonth" | "date" | "datetime"
}

/**
 * 日付入力テキストボックス。
 * 基本的にはブラウザのネイティブな日付入力コントロールの仕様に従う。
 * 以下の機能を持つ。
 *
 * - 基本的な共通レイアウト（以下、例）
 *   - 入力可能なら枠と背景色を変更
 *   - 入力不可能なら枠なし背景色なし
 */
export const DateInput = React.forwardRef<HTMLInputElement, DateInputProps>((props, ref) => {
  const { appearance = "date", className, readOnly, disabled, ...rest } = props

  let type: HTMLInputElement["type"]
  if (appearance === "yearmonth") {
    type = "month"
  } else if (appearance === "date") {
    type = "date"
  } else {
    type = "datetime-local"
  }

  const baseStyle = "block py-px px-1 border outline-blue-500 focus:ring-blue-500"
  const editableStyle = "border-gray-700 bg-white"
  const readOnlyStyle = "border-transparent bg-transparent shadow-none cursor-default focus:ring-0"

  const isEditable = !readOnly && !disabled

  return (
    <input
      type={type}
      ref={ref}
      readOnly={readOnly}
      disabled={disabled}
      className={`${baseStyle} ${isEditable ? editableStyle : readOnlyStyle} ${className ?? ''}`}
      {...rest}
    />
  )
})

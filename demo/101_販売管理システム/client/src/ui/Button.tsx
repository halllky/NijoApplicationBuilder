import React from "react"

export type ButtonProps = {
  /** デフォルトでは type="button" ですが、この属性を true にすると type="submit" になります。 */
  submit?: boolean
  /** クリック時処理 */
  onClick?: React.MouseEventHandler
  /** 塗りつぶし */
  fill?: boolean
  /** 枠線 */
  outline?: boolean
  /** 下線 */
  underline?: boolean
  /** ちょっと小さめになります。 */
  mini?: boolean
  /** ボタンを無効にします。 */
  disabled?: boolean
  /** 読み込み中であることを示すインジケーターが表示されます。 */
  loading?: boolean
  /** テキストを非表示にし、アイコンだけを表示します。 */
  hideText?: boolean
  /** アイコン。 */
  icon?: React.ElementType
  /** アイコンを右側に配置します。 */
  iconRight?: boolean
  /** テキスト */
  children?: React.ReactNode
  /** インライン表示 */
  inline?: boolean
  /** タブインデックス */
  tabIndex?: number
  /** 細かいレイアウトの微調整に使用 */
  className?: string
  /** このボタンをクリックしたときに送信するformのid */
  form?: string
  /** マウスダウン時の処理 */
  onMouseDown?: React.MouseEventHandler
}

/**
 * ボタンコンポーネント
 */
export const Button = ({
  submit,
  onClick,
  fill,
  outline,
  underline,
  mini,
  disabled,
  loading,
  hideText,
  icon: Icon,
  iconRight,
  children,
  inline,
  tabIndex,
  className,
  form,
  onMouseDown,
}: ButtonProps) => {

  // 基本的なスタイル
  const baseStyle = "rounded transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-teal-400 select-none"

  // レイアウト
  const layoutStyle = `flex items-center justify-center gap-1 ${inline ? 'inline-flex' : 'flex'} ${iconRight ? 'flex-row-reverse' : 'flex-row'}`

  // サイズ
  const sizeStyle = mini ? 'px-2 py-0.5 text-xs' : 'px-4 py-1.5 text-sm'

  // 見た目（塗りつぶし、枠線など）
  let appearanceStyle = ""
  if (fill) {
    appearanceStyle = "bg-teal-600 text-white hover:bg-teal-700 border border-transparent"
  } else if (outline) {
    appearanceStyle = "bg-white text-teal-700 border border-teal-700 hover:bg-gray-50"
  } else if (underline) {
    appearanceStyle = "bg-transparent text-teal-600 underline hover:text-teal-800"
  } else {
    appearanceStyle = "bg-transparent text-teal-700 hover:bg-gray-100"
  }

  // 無効状態
  const disabledStyle = (disabled || loading) ? "opacity-50 cursor-not-allowed pointer-events-none" : "cursor-pointer"

  const combinedClassName = [
    baseStyle,
    layoutStyle,
    sizeStyle,
    appearanceStyle,
    disabledStyle,
    className
  ].filter(Boolean).join(" ")

  return (
    <button
      type={submit ? 'submit' : 'button'}
      onClick={onClick}
      onMouseDown={onMouseDown}
      disabled={disabled || loading}
      tabIndex={tabIndex}
      form={form}
      className={combinedClassName}
      title={hideText && typeof children === 'string' ? children : undefined}
    >
      {/* ローディングインジケータ */}
      {loading && (
        <svg className="animate-spin h-4 w-4 text-current" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      )}

      {/* アイコン (ローディング中は非表示) */}
      {!loading && Icon && (
        <Icon className="w-4 h-4" />
      )}

      {/* テキスト */}
      {!hideText && children && (
        <span>{children}</span>
      )}
    </button>
  )
}

import React from "react"
import "./IconButton.css"

/** ボタン。アイコンをつけることができます。 */
export const IconButton = (args: {
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
}) => {

  let className
    = 'justify-center items-center gap-1 select-none'
    + (args.inline ? ' inline-flex' : ' flex')
    + (args.iconRight ? ' flex-row-reverse' : ' flex-row')
    + (args.mini ? ' px-1 py-px' : ' px-2 py-1')
    + (args.underline ? ' underline underline-offset-2' : '')
    + (args.loading ? '' : ' cursor-pointer')

  // 文字色
  if (args.fill) {
    className += args.loading
      ? ' button-style-text-fill-loading'
      : ' button-style-text-fill'
  } else {
    className += args.loading
      ? ' button-style-text-loading'
      : ' button-style-text'
  }

  // 枠線
  if (args.outline) {
    className += args.loading
      ? ' border button-style-outline-loading'
      : ' border button-style-outline'
  } else {
    className += ' border border-transparent'
  }

  // 塗りつぶし
  if (args.fill) {
    className += args.loading
      ? ' button-style-bg-fill-loading'
      : ' button-style-bg-fill'
  }

  // 画面側で指定されたレイアウト
  className += ` ${args.className ?? ''}`

  // 読み込み中のくるくる
  let nowLoadingStyle = `animate-spin h-4 w-4 border-2 rounded-full border-t-transparent`
  if (args.fill) {
    nowLoadingStyle += ' border-sky-50'
  } else if (args.underline) {
    nowLoadingStyle += ' border-sky-300'
  } else {
    nowLoadingStyle += ' border-sky-500'
  }

  return (
    <button
      type={args.submit ? undefined : 'button'}
      onClick={args.loading ? undefined : args.onClick}
      onMouseDown={args.onMouseDown}
      className={className}
      title={args.hideText ? (args.children as string) : undefined}
      tabIndex={args.tabIndex}
      form={args.form}
    >

      {/* アイコン or 読み込み中のくるくる */}
      {args.icon && !args.loading && (
        React.createElement(args.icon, { className: 'w-4' })
      )}
      {args.icon && args.loading && (
        <div className={nowLoadingStyle}></div>
      )}

      {/* テキスト */}
      {!args.hideText && args.children && (
        <span className={`text-sm whitespace-nowrap relative ${(args.loading && !args.icon ? '' : '')}`}>
          {!args.hideText && args.children}
          {args.loading && !args.icon && (
            <div className={`absolute inset-0 m-auto ${nowLoadingStyle}`}></div>
          )}
        </span>
      )}
    </button >
  )
}
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
}) => {

  const flex = args.inline ? 'inline-flex' : 'flex'
  const direction = args.iconRight ? 'flex-row-reverse' : 'flex-row'

  let className = `${flex} ${direction} justify-center items-center gap-1 select-none ${args.className}`

  if (args.fill || args.outline) {
    className += (args.mini ? ' px-1 py-px' : ' px-2 py-1')
  }

  if (args.fill) {
    className += args.loading
      ? ' button-style-fill-loading'
      : ' button-style-fill'

    // 普通に text-color-8 などとするとTailwind既定のスタイルリセットに優先度で負けるので背景色は独自クラスを指定
    // className += args.loading
    //   ? ` text-color-0 bg-color-button-loading`
    //   : ` text-color-0 bg-color-button`
  } else if (args.outline) {
    className += args.loading || args.disabled
      ? ' button-style-outline-loading'
      : ' button-style-outline'

    // className += args.loading
    //   ? ` border border-color-5 text-color-5`
    //   : ` border border-color-7`
  } else if (args.underline) {
    className += args.loading || args.disabled
      ? ' button-style-link-loading'
      : ' button-style-link'

    // className += args.loading
    //   ? ` pr-1 text-sky-300 border-b border-sky-300`
    //   : ` pr-1 text-sky-600 border-b border-sky-600`
  } else {
    className += args.loading || args.disabled
      ? ' button-style-text-loading'
      : ' button-style-text'
  }

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
      {args.children && (
        <span className={`text-sm whitespace-nowrap relative ${(args.loading && !args.icon ? '' : '')}`}>
          {args.hideText ? '\u200B' : args.children}
          {args.loading && !args.icon && (
            <div className={`absolute inset-0 m-auto ${nowLoadingStyle}`}></div>
          )}
        </span>
      )}
    </button >
  )
}
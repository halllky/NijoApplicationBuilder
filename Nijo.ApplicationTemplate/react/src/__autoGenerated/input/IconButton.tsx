import React from "react"

export const IconButton = (args: {
  submit?: boolean
  onClick?: React.MouseEventHandler
  fill?: boolean
  outline?: boolean
  underline?: boolean
  mini?: boolean
  loading?: boolean
  hideText?: boolean
  icon?: React.ElementType
  iconRight?: boolean
  children?: React.ReactNode
  inline?: boolean
  tabIndex?: number
  className?: string
}) => {

  const flex = args.inline ? 'inline-flex' : 'flex'
  const direction = args.iconRight ? 'flex-row-reverse' : 'flex-row'

  let className = `${flex} ${direction} justify-center items-center gap-1 select-none outline-none ${args.className}`

  if (args.fill || args.outline) {
    className += (args.mini ? ' px-1 py-px' : ' px-2 py-1')
  }

  if (args.fill) {
    // 普通に text-color-8 などとするとTailwind既定のスタイルリセットに優先度で負けるので背景色は独自クラスを指定
    className += args.loading
      ? ` text-color-0 bg-color-button-loading`
      : ` text-color-0 bg-color-button`
  } else if (args.outline) {
    className += args.loading
      ? ` border border-color-5 text-color-5`
      : ` border border-color-7`
  } else if (args.underline) {
    className += args.loading
      ? ` pr-1 text-sky-300 border-b border-sky-300`
      : ` pr-1 text-sky-600 border-b border-sky-600`
  }

  // 読み込み中のくるくる
  let nowLoadingStyle = `animate-spin h-4 w-4 border-2 rounded-full border-t-transparent`
  if (args.fill) {
    nowLoadingStyle += ' border-color-0'
  } else if (args.underline) {
    nowLoadingStyle += ' border-sky-300'
  } else {
    nowLoadingStyle += ' border-color-5'
  }

  return (
    <button
      type={args.submit ? undefined : 'button'}
      onClick={args.loading ? undefined : args.onClick}
      className={className}
      title={args.hideText ? (args.children as string) : undefined}
      tabIndex={args.tabIndex}
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
          {args.children}
          {args.loading && !args.icon && (
            <div className={`absolute inset-0 m-auto ${nowLoadingStyle}`}></div>
          )}
        </span>
      )}
    </button >
  )
}

import React from "react"

export const IconButton = (args: {
  submit?: boolean
  onClick?: React.MouseEventHandler
  fill?: boolean
  outline?: boolean
  underline?: boolean
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
  let className: string
  if (args.fill) {
    className = `${flex} ${direction} justify-center items-center select-none outline-none ${args.className} gap-1 px-2 py-1 text-color-0 bg-color-button`
  } else if (args.outline) {
    className = `${flex} ${direction} justify-center items-center select-none outline-none ${args.className} gap-1 px-2 py-1 border border-color-7`
  } else if (args.underline) {
    className = `${flex} ${direction} justify-center items-center select-none outline-none ${args.className} pr-1 text-blue-600 border-b border-blue-500`
  } else {
    className = `${flex} ${direction} justify-center items-center select-none outline-none ${args.className} gap-1`
  }

  return (
    <button
      type={args.submit ? undefined : 'button'}
      onClick={args.onClick}
      className={className}
      title={args.hideText ? (args.children as string) : undefined}
      tabIndex={args.tabIndex}
    >

      {args.icon && React.createElement(args.icon, { className: 'w-4' })}

      {!args.hideText && args.children && (
        <span className="text-sm whitespace-nowrap">
          {args.children}
        </span>
      )}
    </button>
  )
}

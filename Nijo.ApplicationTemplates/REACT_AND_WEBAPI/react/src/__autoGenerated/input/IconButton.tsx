import React from "react"

export const IconButton = (args: {
  submit?: boolean
  onClick?: React.MouseEventHandler
  fill?: true
  outline?: true
  underline?: true
  hideText?: true
  icon?: React.ElementType
  children?: React.ReactNode
  inline?: boolean
  className?: string
}) => {

  const flex = args.inline ? 'inline-flex' : 'flex'
  let className: string
  if (args.fill) {
    className = `${flex} flex-row justify-center items-center select-none outline-none ${args.className} space-x-2 px-2 py-1 text-color-0 bg-color-button`
  } else if (args.outline) {
    className = `${flex} flex-row justify-center items-center select-none outline-none ${args.className} space-x-2 px-2 py-1 border border-color-7`
  } else if (args.underline) {
    className = `${flex} flex-row justify-center items-center select-none outline-none ${args.className} pr-1 text-blue-600 border-b border-blue-500`
  } else {
    className = `${flex} flex-row justify-center items-center select-none outline-none ${args.className} space-x-2`
  }

  return (
    <button
      type={args.submit ? undefined : 'button'}
      onClick={args.onClick}
      className={className}
      title={args.hideText && (args.children as string)}
    >

      {args.icon && React.createElement(args.icon, { className: 'w-4' })}

      {!args.hideText && args.children && (
        <span className="text-sx">
          {args.children}
        </span>
      )}
    </button>
  )
}

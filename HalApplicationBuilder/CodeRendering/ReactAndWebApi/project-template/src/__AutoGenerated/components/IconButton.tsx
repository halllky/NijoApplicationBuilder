import React from "react"

export const IconButton = (args: {
  onClick?: React.MouseEventHandler
  fill?: true
  outline?: true
  underline?: true
  hideText?: true
  icon?: React.ElementType
  children?: React.ReactNode
  className?: string
}) => {

  let className: string
  if (args.fill) {
    className = `flex flex-row justify-center items-center select-none ${args.className} space-x-2 px-2 py-1 text-white bg-neutral-600`
  } else if (args.outline) {
    className = `flex flex-row justify-center items-center select-none ${args.className} space-x-2 px-2 py-1 border border-neutral-600`
  } else if (args.underline) {
    className = `flex flex-row justify-center items-center select-none ${args.className} pr-1 border-b border-neutral-500`
  } else {
    className = `flex flex-row justify-center items-center select-none ${args.className} space-x-2`
  }

  return (
    <button
      onClick={args.onClick}
      className={className}
      title={args.hideText && (args.children as string)}
    >

      {args.icon && React.createElement(args.icon, { className: 'w-4' })}

      {!args.hideText && args.children && (
        <span className="text-sm">
          {args.children}
        </span>
      )}
    </button>
  )
}

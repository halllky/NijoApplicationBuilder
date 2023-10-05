import React, { useRef } from "react"
import { useFocusTarget } from "../hooks/GlobalFocus"

export const IconButton = (args: {
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
  const ref = useRef<HTMLButtonElement>(null)

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
      ref={ref}
      onClick={args.onClick}
      className={className}
      title={args.hideText && (args.children as string)}
      {...useFocusTarget(ref)}
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

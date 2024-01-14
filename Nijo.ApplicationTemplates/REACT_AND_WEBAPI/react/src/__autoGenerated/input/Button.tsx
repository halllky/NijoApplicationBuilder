import React from 'react'
import { forwardRefEx } from '../util'

type ButtonAttrs = {
  submit?: boolean
  icon?: React.ElementType
  small?: boolean
  iconOnly?: boolean
  outlined?: boolean
}
export const Button = forwardRefEx<HTMLButtonElement, React.ButtonHTMLAttributes<HTMLButtonElement> & ButtonAttrs>((props, ref) => {
  const {
    type,
    icon,
    small,
    outlined,
    title: argTitle,
    submit,
    className: additionalClassName,
    children,
    iconOnly,
    ...rest
  } = props

  let className = 'flex items-center select-none text-color-7'
  if (outlined) className += ' border border-1 border-color-7'
  if (!icon) className += ' px-1'
  if (additionalClassName) className += ' ' + additionalClassName

  let title: string | undefined
  if (argTitle) title = argTitle
  else if (icon && iconOnly) title = children as string
  else title = undefined

  let childNode: React.ReactNode
  if (icon) childNode = (<>
    {React.createElement(icon, { className: `flex-1 ${small ? 'w-3' : 'w-6'}` })}
    {(iconOnly ? undefined : children)}
  </>)
  else if (children) childNode = children
  else childNode = undefined

  return (
    <button ref={ref} {...rest}
      type={type ?? (submit ? 'submit' : 'button')}
      className={className}
      title={title}
    >
      {childNode}
    </button>
  )
})

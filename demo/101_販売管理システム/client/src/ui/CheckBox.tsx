import React from "react"

export type CheckBoxProps = React.InputHTMLAttributes<HTMLInputElement> & {
  children?: React.ReactNode
}

/**
 * チェックボックス。 `label` + `input type="checkbox"` の組み合わせ。
 */
export const CheckBox = React.forwardRef<HTMLInputElement, CheckBoxProps>((props, ref) => {
  const { children, className, ...rest } = props
  return (
    <label className={`inline-flex items-center gap-2 cursor-pointer select-none ${className ?? ''}`}>
      <input
        type="checkbox"
        ref={ref}
        className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        {...rest}
      />
      {children && <span className="text-sm text-gray-700">{children}</span>}
    </label>
  )
})

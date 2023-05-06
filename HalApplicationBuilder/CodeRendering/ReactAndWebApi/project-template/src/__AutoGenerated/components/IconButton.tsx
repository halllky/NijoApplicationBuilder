import React from "react"

export const IconButton = ({ onClick, icon, outline, children, className }: {
    onClick?: React.MouseEventHandler
    outline?: true
    icon?: React.ElementType
    children?: React.ReactNode
    className?: string
}) => {
    const className2 = outline
        ? `flex flex-row justify-center items-center select-none px-2 py-1 border border-neutral-600 ${className}`
        : `flex flex-row justify-center items-center select-none px-2 py-1 text-white bg-neutral-600 ${className}`

    // アイコンだけ
    if (!children) return (
        <button onClick={onClick} className={className}>
            {icon && React.createElement(icon, { className: 'w-4' })}
        </button>
    )

    // アイコン + 文字
    return (
        <button onClick={onClick} className={className2}>
            {icon && React.createElement(icon, { className: 'w-4 mr-1' })}
            <span className="text-sm">
                {children}
            </span>
        </button>
    )
}

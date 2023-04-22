import React from "react"

export const IconButton = ({ onClick, icon, outline, children }: {
    onClick?: React.MouseEventHandler
    outline?: true
    icon?: React.ElementType
    children?: React.ReactNode
}) => {
    const className = outline
        ? 'flex flex-row justify-center items-center select-none px-2 py-1 border border-neutral-600'
        : 'flex flex-row justify-center items-center select-none px-2 py-1 text-white bg-neutral-600'
    return (
        <button onClick={onClick} className={className}>
            {icon && React.createElement(icon, { className: 'w-4 mr-1' })}
            <span className="text-sm">
                {children}
            </span>
        </button>
    )
}

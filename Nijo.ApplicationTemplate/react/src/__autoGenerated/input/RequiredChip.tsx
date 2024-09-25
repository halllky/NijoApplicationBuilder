import React from 'react'

/** 必須入力項目であることを表すUI */
export const RequiredChip = React.memo(({ className }: {
  className?: string
}) => {
  return (
    <span className={`p-1 select-none text-xs text-color-5 bg-color-3 font-bold whitespace-nowrap rounded-md ${className ?? ''}`}>
      必須
    </span>
  )
})

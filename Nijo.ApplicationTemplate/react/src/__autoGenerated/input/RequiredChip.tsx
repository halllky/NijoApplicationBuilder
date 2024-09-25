import React from 'react'

/** 必須入力項目であることを表すUI */
export const RequiredChip = React.memo(({ className }: {
  className?: string
}) => {
  return (
    <span className={`select-none text-sm font-bold ${className ?? ''}`}>
      必須
    </span>
  )
})

import React from "react"
import { Button } from "./Button"

type PagerProps = {
  pageIndex: number
  pageSize: number
  totalCount: number
  onPageChange: (newPage: number) => void
  disabled?: boolean
  className?: string
}

export function Pager(props: PagerProps) {
  const { pageIndex, pageSize, totalCount, onPageChange, disabled } = props

  const totalPages = Math.ceil(totalCount / pageSize)
  const canPrev = pageIndex > 0
  const canNext = pageIndex < totalPages - 1

  return (
    <div className={`w-full flex justify-center items-center ${props.className ?? ""}`}>
      <div className="flex gap-2 items-center">
        <Button outline
          onClick={() => onPageChange(pageIndex - 1)}
          disabled={disabled || !canPrev}
        >
          前へ
        </Button>
        <span className="px-2 py-px select-none">
          {pageIndex + 1} / {Math.max(1, totalPages)}
        </span>
        <Button outline
          onClick={() => onPageChange(pageIndex + 1)}
          disabled={disabled || !canNext}
        >
          次へ
        </Button>
      </div>
    </div>
  )
}

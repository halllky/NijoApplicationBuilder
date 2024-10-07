import React from 'react'
import useEvent from 'react-use-event-hook'
import * as Icon from '@heroicons/react/24/solid'
import { IconButton } from './IconButton'

export const usePager = (
  /** SQLのSKIP句に設定される件数 */
  sqlSkip: number | undefined,
  /** 1ページ当たり表示件数 */
  pageSize: number | undefined,
  /** 現在の検索条件にヒットするデータの総件数 */
  totalItemCount: number | undefined,
  /** skipの値が変更された時の処理 */
  onSqlSkipChanged: (skip: number) => void
) => {
  const currentPageIndex = pageSize === undefined || pageSize === 0
    ? 0 // ゼロ除算回避
    : Math.floor((sqlSkip ?? 0) / pageSize)

  const maxPageIndex = pageSize === undefined || pageSize === 0
    ? 0 // ゼロ除算回避
    : (Math.ceil((totalItemCount ?? 0) / pageSize) - 1)

  const toFirstPage = useEvent(() => {
    const newSkip = 0
    if (newSkip !== sqlSkip) onSqlSkipChanged(newSkip)
  })
  const toLastPage = useEvent(() => {
    const newSkip = maxPageIndex * (pageSize ?? 0)
    if (newSkip !== sqlSkip) onSqlSkipChanged(newSkip)
  })
  const toPreviousPage = useEvent(() => {
    const newSkip = Math.max(0, (sqlSkip ?? 0) - (pageSize ?? 0))
    if (newSkip !== sqlSkip) onSqlSkipChanged(newSkip)
  })
  const toNextPage = useEvent(() => {
    const newSkip = Math.min(maxPageIndex * (pageSize ?? 0), (sqlSkip ?? 0) + (pageSize ?? 0))
    if (newSkip !== sqlSkip) onSqlSkipChanged(newSkip)
  })
  const toPageIndex = useEvent((pageIndex: number) => {
    const newSkip = Math.max(0, Math.min(pageIndex, maxPageIndex)) * (pageSize ?? 0)
    if (newSkip !== sqlSkip) onSqlSkipChanged(newSkip)
  })

  return {
    /** 現在ページ（ゼロ始まり） */
    currentPageIndex,
    /** 最大ページ（ゼロ始まり） */
    maxPageIndex,
    /** 最初のページへ */
    toFirstPage,
    /** 最後のページへ */
    toLastPage,
    /** 前のページへ */
    toPreviousPage,
    /** 次のページへ */
    toNextPage,
    /** 指定したページへ */
    toPageIndex,
  }
}

/** usePagerの戻り値 */
export type PagerReturns = ReturnType<typeof usePager>

/** サーバー側ページングのページャ */
export const ServerSidePager = React.memo(({ className, ...state }: PagerReturns & {
  className?: string
}) => {
  return (
    <div className={`flex px-1 gap-4 ${className ?? ''}`}>
      <IconButton onClick={state.toFirstPage} icon={Icon.ChevronDoubleLeftIcon} outline hideText>最初のページへ</IconButton>
      <IconButton onClick={state.toPreviousPage} icon={Icon.ChevronLeftIcon} outline hideText>前のページへ</IconButton>

      <span className="select-none whitespace-nowrap">
        {state.currentPageIndex + 1} / {state.maxPageIndex + 1} ページ
      </span>

      <IconButton onClick={state.toNextPage} icon={Icon.ChevronRightIcon} outline hideText>次のページへ</IconButton>
      <IconButton onClick={state.toLastPage} icon={Icon.ChevronDoubleRightIcon} outline hideText>最後のページへ</IconButton>
    </div>
  )
})

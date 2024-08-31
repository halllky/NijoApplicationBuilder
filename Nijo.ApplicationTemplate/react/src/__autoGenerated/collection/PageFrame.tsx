import React, { HTMLAttributes } from 'react'
import * as Util from '../util'
import { NowLoading } from '../input/NowLoading'

/**
 * ページコンテンツの枠
 */
export const PageFrame = ({ header, footer, children, className, nowLoading, ...rest }: HTMLAttributes<HTMLDivElement> & {
  header?: React.ReactNode
  footer?: React.ReactNode
  children?: React.ReactNode
  nowLoading?: boolean
}) => {
  return (
    <div {...rest} className={`flex flex-col justify-start h-full overflow-auto ${className ?? ''}`}>
      <header className="flex justify-start items-center basis-10 p-1 gap-4">
        <Util.SideMenuCollapseButton />
        {header}
      </header>

      <Util.InlineMessageList />

      <main className="relative flex-1 p-1 overflow-auto">
        {children}
        {nowLoading && (
          <NowLoading />
        )}
      </main>

      {footer && (
        <footer className="flex justify-start items-center p-1 gap-1">
          {footer}
        </footer>
      )}

    </div>
  )
}

/** ページタイトルの文字 */
export const PageTitle = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  return (
    <h1 className={`text-base font-semibold select-none whitespace-nowrap overflow-hidden text-ellipsis ${className ?? ''}`}>
      {children}
    </h1>
  )
}

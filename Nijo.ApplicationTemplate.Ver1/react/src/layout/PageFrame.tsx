import React from "react";

type PageFrameProps = {
  headerContent?: React.ReactNode
  children?: React.ReactNode
  className?: string
};

/** 画面の枠 */
export const PageFrame = (props: PageFrameProps) => {
  return (
    <div className="flex flex-col h-full">

      {/* 画面ヘッダ部 */}
      <div className="flex items-center p-1 gap-2 border-b">
        {props.headerContent}
      </div>

      {/* ページ本体 */}
      <div className={`flex-1 overflow-auto ${props.className ?? ''}`}>
        {props.children}
      </div>

    </div>
  )
}

export const PageFrameTitle = (props: { children?: React.ReactNode }) => {
  return (
    <h1 className="text-xl font-bold">
      {props.children}
    </h1>
  )
}

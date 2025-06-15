import React from "react"

/** ページの枠 */
export const PageFrame = (props: {
  title: string | undefined
  children?: React.ReactNode
}) => {
  return (
    <div className="h-full flex flex-col gap-4 p-1">
      {props.title && (
        <h1 className="text-xl font-bold">{props.title}</h1>
      )}
      <div className="flex-1 flex flex-col gap-4 overflow-y-scroll">
        {props.children}
      </div>
    </div>
  )
}

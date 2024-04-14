import React from "react"

/** 0個以上のItemまたはContainerを内部に含む入れ物。Containerに内包された要素はインデントが1段下がる。 */
const Container = ({
  label,
  labelSide,
  leftColumnMinWidth: propsLeftColMinWidth,
  indentSizePx: propsIndentSizePx,
  children,
  className,
}: {
  label?: string
  labelSide?: React.ReactNode
  leftColumnMinWidth?: string
  indentSizePx?: number
  children?: React.ReactNode
  className?: string
}) => {
  const { depth, indentSizePx, leftColumnMinWidth } = React.useContext(VFormContext)
  const innerContextValue = React.useMemo<VFormContextValue>(() => ({
    depth: depth + 1,
    leftColumnMinWidth: propsLeftColMinWidth ?? leftColumnMinWidth,
    indentSizePx: propsIndentSizePx ?? indentSizePx,
  }), [depth, propsLeftColMinWidth, leftColumnMinWidth, propsIndentSizePx, indentSizePx])

  const isRoot = depth === 0

  return (
    <VFormContext.Provider value={innerContextValue}>
      <div className={`flex flex-col justify-start ${!isRoot && 'basis-full border-t border-l border-color-5'} ${className}`}>
        {(label || labelSide) && (
          <div className={`flex flex-wrap gap-2 items-center ${isRoot ? 'py-1' : 'p-1'}`}>
            <LabelText>{label}</LabelText>
            {labelSide}
          </div>)}
        <div className="flex-1 flex bg-color-3">
          {!isRoot && <Indent className="bg-color-3" />}
          {/* なぜか width:0 （というよりwidthに明示的な値を）を指定しないとDataTableなど横長のコンテンツがページ外まで伸びてしまう */}
          <div className={`w-0 flex-1 flex flex-wrap overflow-x-hidden ${isRoot && 'border-r border-b'} border-color-5`}>
            {children}
          </div>
        </div>
      </div>
    </VFormContext.Provider>
  )
}

/** 名前と値のペア */
const Item = ({ label, labelSide, wide, children, className }: {
  label?: string
  labelSide?: React.ReactNode
  wide?: boolean
  children?: React.ReactNode
  className?: string
}) => {
  const { depth, leftColumnMinWidth, indentSizePx } = React.useContext(VFormContext)
  const leftColumnStyle: React.CSSProperties = {
    minWidth: leftColumnMinWidth ? `calc(${leftColumnMinWidth} - ${depth * (indentSizePx ?? 24)}px)` : undefined,
  }

  return <>
    {wide && (label || labelSide) && (
      <div className={`basis-full flex flex-wrap items-center gap-2 p-1 border-t border-l border-color-5 ${className}`}>
        <LabelText>{label}</LabelText>
        {labelSide}
      </div >
    )}
    <div className={`flex-1 ${(wide ? 'basis-full' : 'min-w-fit')} flex align-center overflow-x-hidden border-t border-l border-color-5 ${className}`}>
      {!wide && (label || labelSide) && (
        <div className="flex flex-col items-start gap-2 p-1 bg-color-3 border-r border-color-5" style={leftColumnStyle}>
          <LabelText>{label}</LabelText>
          {labelSide}
        </div>
      )}
      <div className="flex-1 p-1 bg-color-0 overflow-x-auto">
        {children}
      </div>
    </div>
  </>
}
const LabelText = ({ children }: {
  children?: React.ReactNode
}) => {
  return children && (
    <span className="select-none text-color-7">
      {children}
    </span>
  )
}

const Indent = ({ className }: {
  className?: string
}) => {
  const { indentSizePx } = React.useContext(VFormContext)

  return (
    <div
      className={className}
      style={{ flexBasis: indentSizePx ?? 24 }}
    ></div>
  )
}

type VFormContextValuePublic = {
  leftColumnMinWidth?: string
  indentSizePx?: number
}
type VFormContextValue = VFormContextValuePublic & {
  depth: number
}
const VFormContext = React.createContext<VFormContextValue>({
  depth: 0,
})

export const VerticalForm = {
  Container,
  Item,
}

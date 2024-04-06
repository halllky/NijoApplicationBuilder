import React from "react"

/** 0個以上のItemまたはContainerを内部に含む入れ物。Containerに内包された要素はインデントが1段下がる。 */
const Container = ({
  label,
  labelSide,
  leftColumnWidth: propsLeftColWidth,
  indentSizePx: propsIndentSizePx,
  children,
  className,
}: {
  label?: string
  labelSide?: React.ReactNode
  leftColumnWidth?: string
  indentSizePx?: number
  children?: React.ReactNode
  className?: string
}) => {
  const { depth, indentSizePx, leftColumnWidth } = React.useContext(VFormContext)
  const innerContextValue = React.useMemo<VFormContextValue>(() => ({
    depth: depth + 1,
    leftColumnWidth: propsLeftColWidth ?? leftColumnWidth,
    indentSizePx: propsIndentSizePx ?? indentSizePx,
  }), [depth, propsLeftColWidth, leftColumnWidth, propsIndentSizePx, indentSizePx])

  const isRoot = depth === 0

  return (
    <VFormContext.Provider value={innerContextValue}>
      <div className={`flex flex-col justify-start ${!isRoot && 'border-t border-color-5'} ${className}`}>
        {(label || labelSide) && (
          <div className={`flex flex-wrap gap-2 items-center ${isRoot ? 'py-1' : 'p-1'}`}>
            <LabelText>{label}</LabelText>
            {labelSide}
          </div>)}
        <div className="flex-1 flex bg-color-3">
          {!isRoot && <Indent className="bg-color-3" />}
          <div className={`flex-1 flex flex-col overflow-x-hidden border-l ${isRoot && 'border-r border-b'} border-color-5`}>
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
  const { depth, leftColumnWidth, indentSizePx } = React.useContext(VFormContext)
  const leftColumnStyle: React.CSSProperties = {
    flexBasis: `calc(${leftColumnWidth ?? '28rem'} - ${depth * (indentSizePx ?? 24)}px)`,
  }

  return <>
    {wide && label && (
      <div className={`flex flex-wrap items-center gap-2 p-1 border-t border-color-5 ${className}`}>
        <LabelText>{label}</LabelText>
        {labelSide}
      </div >
    )}
    <div className={`flex align-center w-full overflow-x-hidden border-t border-color-5 ${className}`}>
      {!wide && (
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
  leftColumnWidth?: string
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

import React from "react"

/** 0個以上のItemまたはContainerを内部に含む入れ物。Containerに内包された要素はインデントが1段下がる。 */
const Container = ({
  label,
  labelSide,
  rightColumnWidth: propsRightColWidth,
  indentSizePx: propsIndentSizePx,
  children,
  className,
}: {
  label?: string
  labelSide?: React.ReactNode
  rightColumnWidth?: string
  indentSizePx?: number
  children?: React.ReactNode
  className?: string
}) => {
  const { depth, indentSizePx, rightColumnWidth } = React.useContext(VFormContext)
  const innerContextValue = React.useMemo<VFormContextValue>(() => ({
    depth: depth + 1,
    rightColumnWidth: propsRightColWidth ?? rightColumnWidth,
    indentSizePx: propsIndentSizePx ?? indentSizePx,
  }), [depth, propsRightColWidth, rightColumnWidth, propsIndentSizePx, indentSizePx])

  const isRoot = depth === 0

  return (
    <VFormContext.Provider value={innerContextValue}>
      <div className={`flex flex-col justify-start items-stretch ${className} ${!isRoot ? 'border-t first:border-none border-color-5' : ''}`}>
        {(label || labelSide) && (
          <div className={`flex flex-wrap gap-2 items-center ${isRoot ? 'py-1' : 'p-1'}`}>
            <LabelText>{label}</LabelText>
            {labelSide}
          </div>)}
        <div className="flex-1 flex bg-color-3">
          {!isRoot && <Indent className="bg-color-3" />}
          <div className={`flex-1 border-t border-l ${isRoot ? 'border-r border-b' : ''} border-color-5`}>
            {children}
          </div>
        </div>
      </div>
    </VFormContext.Provider>
  )
}

/** 名前と値のペア */
const Item = ({ label, labelSide, wide, children }: {
  label?: string
  labelSide?: React.ReactNode
  wide?: boolean
  children?: React.ReactNode
}) => {
  const { rightColumnWidth } = React.useContext(VFormContext)
  const valueColumnStyle: React.CSSProperties = {
    flex: wide ? '1' : undefined,
    flexBasis: wide ? undefined : (rightColumnWidth ?? '28rem'),
  }

  return <>
    {wide && label && (
      <div className="flex flex-wrap items-center gap-2 p-1 border-t first:border-none border-color-5">
        <LabelText>{label}</LabelText>
        {labelSide}
      </div >
    )}
    <div className="flex align-center border-t first:border-none border-color-5">
      {!wide && (
        <div className="flex-1 flex flex-col items-start gap-2 p-1 bg-color-3 border-r border-color-5">
          <LabelText>{label}</LabelText>
          {labelSide}
        </div>
      )}
      <div className="p-1 bg-color-0" style={valueColumnStyle}>
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
  rightColumnWidth?: string
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

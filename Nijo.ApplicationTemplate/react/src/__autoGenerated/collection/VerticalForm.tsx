import React, { useContext } from "react"

/** 0個以上のItemまたはContainerを内部に含む入れ物。Containerに内包された要素はインデントが1段下がる。 */
const Container = ({
  label,
  labelSide,
  leftColumnMinWidth: propsLeftColMinWidth,
  children,
  className,
}: {
  label?: string
  labelSide?: React.ReactNode
  leftColumnMinWidth?: string
  children?: React.ReactNode
  className?: string
}) => {
  const { depth, leftColumnMinWidth: contextLeftColumnMinWidth } = React.useContext(VFormContext)
  const leftColumnMinWidth = propsLeftColMinWidth ?? contextLeftColumnMinWidth

  const innerContextValue = React.useMemo<VFormContextValue>(() => ({
    depth: depth + 1,
    leftColumnMinWidth,
  }), [depth, leftColumnMinWidth])

  const gridStyle: React.CSSProperties = {
    gridTemplateColumns: leftColumnMinWidth
      ? `repeat(auto-fit, minmax(calc(16rem + ${leftColumnMinWidth?.trim()}), 1fr))`
      : `repeat(auto-fit, minmax(16rem, 1fr))`,
  }

  return (
    <VFormContext.Provider value={innerContextValue}>
      {depth <= 1 ? (

        <div className={`col-span-full flex flex-col ${depth === 1 ? 'mt-4' : ''} ${className ?? ''}`}>
          {(label || labelSide) && (
            <div className="p-1 flex flex-wrap items-center gap-1">
              <LabelText>{label}</LabelText>
              {labelSide}
            </div>
          )}
          <div className="grid gap-px flex-1" style={gridStyle}>
            {children}
          </div>
        </div>

      ) : (

        <div className={`col-span-full flex flex-col bg-color-2 border-vform ${className ?? ''}`}>
          <div className="p-1 flex flex-wrap items-center gap-1">
            <LabelText>{label}</LabelText>
            {labelSide}
          </div>
          <div className="grid gap-px flex-1 ml-[2rem]" style={gridStyle}>
            {children}
          </div>
        </div>

      )}
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
  const { leftColumnMinWidth } = useContext(VFormContext)

  return wide ? (
    <div className="flex flex-col col-span-full border-vform">
      {(label || labelSide) && (
        <Label>
          <LabelText>{label}</LabelText>
          {labelSide}
        </Label>
      )}
      <div className={`flex-1 bg-color-0 ${className ?? ''}`}>
        {children}
      </div>
    </div>
  ) : (
    <div className="flex border-vform">
      {(label || labelSide) && (
        <Label flexBasis={leftColumnMinWidth}>
          <LabelText>{label}</LabelText>
          {labelSide}
        </Label>
      )}
      <div className={`flex-1 bg-color-0 p-1 ${className ?? ''}`}>
        {children}
      </div>
    </div>
  )
}

const Label = ({ flexBasis, children }: { flexBasis?: string, children?: React.ReactNode }) => {
  return (
    <div className="flex flex-wrap items-start gap-1 bg-color-2 p-1" style={{ flexBasis }}>
      {children}
    </div>
  )
}
const LabelText = ({ children }: {
  children?: React.ReactNode
}) => {
  return children && (
    <span className="select-none text-color-7 text-sm font-semibold">
      {children}
    </span>
  )
}

type VFormContextValuePublic = {
  leftColumnMinWidth?: string
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

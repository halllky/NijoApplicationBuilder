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
  const { depth, leftColumnMinWidth } = React.useContext(VFormContext)
  const innerContextValue = React.useMemo<VFormContextValue>(() => ({
    depth: depth + 1,
    leftColumnMinWidth: propsLeftColMinWidth ?? leftColumnMinWidth,
  }), [depth, propsLeftColMinWidth, leftColumnMinWidth])

  return (
    <VFormContext.Provider value={innerContextValue}>
      {depth <= 1 ? (

        <div className={`col-span-full flex flex-col ${depth > 0 && 'mt-4'} ${className ?? ''}`}>
          {(label || labelSide) && (
            <div className="p-1 flex flex-wrap items-center gap-1">
              <LabelText>{label}</LabelText>
              {labelSide}
            </div>
          )}
          <div className="flex-1 grid gap-[1px] grid-cols-[repeat(auto-fill,minmax(16rem,1fr))]">
            {children}
          </div>
        </div>

      ) : (

        <div className={`col-span-full flex flex-col bg-color-2 ${className ?? ''}`} style={SHADOWBORDER}>
          <div className="p-1 flex flex-wrap items-center gap-1">
            <LabelText>{label}</LabelText>
            {labelSide}
          </div>
          <div className="flex-1 ml-[2rem] grid gap-[1px] grid-cols-[repeat(auto-fill,minmax(16rem,1fr))]">
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
    <div className={`flex flex-col col-span-full ${className ?? ''}`} style={SHADOWBORDER}>
      {(label || labelSide) && (
        <Label>
          <LabelText>{label}</LabelText>
          {labelSide}
        </Label>
      )}
      <Value>{children}</Value>
    </div>
  ) : (
    <div className={`flex ${className ?? ''}`} style={SHADOWBORDER}>
      {(label || labelSide) && (
        <Label flexBasis={leftColumnMinWidth}>
          <LabelText>{label}</LabelText>
          {labelSide}
        </Label>
      )}
      <Value>{children}</Value>
    </div>
  )
}

const Label = ({ flexBasis, children }: { flexBasis?: string, children?: React.ReactNode }) => {
  return (
    <div className="flex flex-wrap items-center gap-1 bg-color-2 p-1" style={{ flexBasis }}>
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

const Value = ({ children }: { children?: React.ReactNode }) => {
  return (
    <div className="flex-1 p-1 bg-color-0">
      {children}
    </div>
  )
}

const SHADOWBORDER: React.CSSProperties = {
  boxShadow: '0 0 0 1px #ddd'
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

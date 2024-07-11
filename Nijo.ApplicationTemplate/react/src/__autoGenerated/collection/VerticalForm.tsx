import React, { useContext } from "react"

export type ContainerProps = {
  label?: React.ReactNode
  labelPosition?: 'top' | 'left'
  estimatedLabelWidth?: string
  estimatedValueWidth?: string
  children?: React.ReactNode
  className?: string
}

/** 0個以上のItemまたはContainerを内部に含む入れ物。Containerに内包された要素はインデントが1段下がる。 */
const Container = ({
  label,
  labelPosition,
  estimatedLabelWidth: propsEstimatedLabelWidth,
  estimatedValueWidth: propsEstimatedValueWidth,
  children,
  className,
}: ContainerProps) => {
  const {
    depth,
    estimatedLabelWidth: contextEstimatedLabelWidth,
    estimatedValueWidth: contextEstimatedValueWidth,
  } = React.useContext(VFormContext)

  const estimatedLabelWidth = propsEstimatedLabelWidth ?? contextEstimatedLabelWidth
  const estimatedValueWidth = propsEstimatedValueWidth ?? contextEstimatedValueWidth

  const innerContextValue = React.useMemo<VFormContextValue>(() => ({
    depth: depth + 1,
    estimatedLabelWidth,
    estimatedValueWidth,
  }), [depth, estimatedLabelWidth, estimatedValueWidth])

  // container
  const background = depth >= 1 ? 'bg-color-2 border-vform' : ''
  const flexDirection = labelPosition === 'left' ? '' : 'flex-col'

  // label
  const labelPadding = (depth >= 1 && labelPosition !== 'left') ? 'px-1 py-px' : ''

  // contents
  const indentLeft = (depth >= 1 && labelPosition !== 'left') ? 'ml-[2rem]' : ''
  const gridStyle: React.CSSProperties = {
    gridTemplateColumns: `repeat(auto-fit, minmax(calc((${estimatedLabelWidth}) + (${estimatedValueWidth})), 1fr))`,
    // gridTemplateColumns: `repeat(1, minmax(0, 1fr))`, // 折り返しなし縦一直線
  }

  return (
    <VFormContext.Provider value={innerContextValue}>
      <div className={`col-span-full flex ${flexDirection} ${background} ${className ?? ''}`}>
        {(label !== undefined) && (
          <div className={`flex justify-start items-start ${labelPadding}`}>
            {renderLabel(label)}
          </div>
        )}
        <div className={`grid gap-px flex-1 ${indentLeft}`} style={gridStyle}>
          {children}
        </div>
      </div>
    </VFormContext.Provider>
  )
}

/** 名前と値のペア */
const Item = ({ label, wide, children, className }: {
  label?: React.ReactNode
  wide?: boolean
  children?: React.ReactNode
  className?: string
}) => {
  const { estimatedLabelWidth } = useContext(VFormContext)

  return wide ? (
    <div className="flex flex-col col-span-full border-vform">
      {(label !== undefined) && (
        <ItemLabel>{renderLabel(label)}</ItemLabel>
      )}
      <div className={`flex-1 min-w-0 bg-color-0 ${className ?? ''}`}>
        {children}
      </div>
    </div>
  ) : (
    <div className="flex border-vform">
      {(label !== undefined) && (
        <ItemLabel flexBasis={estimatedLabelWidth}>{renderLabel(label)}</ItemLabel>
      )}
      <div className={`flex-1 min-w-0 bg-color-0 px-1 py-px ${className ?? ''}`}>
        {children}
      </div>
    </div>
  )
}

const ItemLabel = ({ flexBasis, children }: { flexBasis?: string, children?: React.ReactNode }) => {
  return (
    <div className="flex flex-wrap items-start gap-1 bg-color-2 px-1 py-px" style={{ flexBasis }}>
      {children}
    </div>
  )
}

const renderLabel = (label: React.ReactNode): React.ReactNode => {
  const t = typeof label
  if (t === 'string' || t === 'number' || t === 'bigint') {
    return <LabelText>{label}</LabelText>
  } else {
    return label
  }
}

const LabelText = ({ children }: {
  children?: React.ReactNode
}) => {
  return (
    <span className="select-none text-color-7 text-sm font-semibold">
      {children}
    </span>
  )
}

const DEFAULT_LABEL_WIDTH = '6rem'
const DEFAULT_VALUE_WIDTH = '12rem'

type VFormContextValue = {
  depth: number
  estimatedLabelWidth: string
  estimatedValueWidth: string
}
const VFormContext = React.createContext<VFormContextValue>({
  depth: 0,
  estimatedLabelWidth: DEFAULT_LABEL_WIDTH,
  estimatedValueWidth: DEFAULT_VALUE_WIDTH,
})

export const VerticalForm = {
  Container,
  Item,
  LabelText,
}

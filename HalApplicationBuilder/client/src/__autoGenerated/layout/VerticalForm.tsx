import React, { HTMLAttributes, useContext, useMemo } from "react";
import { forwardRefEx } from "../util";

const Root = forwardRefEx<HTMLDivElement, HTMLAttributes<HTMLDivElement> & RootContextValue & {
  label?: React.ReactNode
}>((props, ref) => {
  const {
    label,
    children,
    indentSizePx,
    leftColumnWidth,
    ...rest
  } = props

  const contextValue: RootContextValue = useMemo(() => ({
    leftColumnWidth: leftColumnWidth ?? '240px',
    indentSizePx: indentSizePx ?? 24,
  }), [leftColumnWidth, indentSizePx])
  const nestedContextValue: NearestSectionContextValue = useMemo(() => ({
    depth: 0,
    tableStartDepth: undefined,
  }), [])

  return (
    <FormLayoutContext.Provider value={contextValue}>
      <NearestSectionContext.Provider value={nestedContextValue}>
        <div ref={ref} {...rest} className={`flex flex-col items-stretch ${rest.className}`}>
          {label && (
            <div className="font-bold mb-2">
              {label}
            </div>
          )}
          {children}
        </div>
      </NearestSectionContext.Provider>
    </FormLayoutContext.Provider>
  )
})


const Section = ((props: {
  label?: React.ReactNode
  table?: boolean
  children?: React.ReactNode
  hidden?: boolean
}) => {
  const { depth: parentDepth, tableStartDepth } = useContext(NearestSectionContext)
  const parentIsTable = tableStartDepth !== undefined
  const thisContextValue: NearestSectionContextValue = useMemo(() => {
    let currentDepth: number
    if (!props.label) {
      currentDepth = parentDepth
    } else if (!parentIsTable && props.table) {
      currentDepth = parentDepth
    } else {
      currentDepth = parentDepth + 1
    }
    return {
      depth: currentDepth,
      tableStartDepth: tableStartDepth ?? (props.table ? currentDepth : undefined),
    }
  }, [props.label, parentDepth, props.table, tableStartDepth, parentIsTable])

  return (
    <NearestSectionContext.Provider value={thisContextValue}>
      <div className={`flex flex-row ${props.hidden && 'hidden'}`}>
        <Indent depth={parentDepth} tableStartDepth={thisContextValue.tableStartDepth} />
        <div className={parentIsTable
          ? `flex gap-1 flex-1 text-sm font-semibold p-1 ${BG_COLOR_HEADER} border ${BORDER_COLOR} mt-[-1px]`
          : `flex gap-1 flex-1 text-sm font-semibold p-1`}>
          {props.label}
        </div>
      </div>
      {props.children}
    </NearestSectionContext.Provider>
  )
})

const Row = ((props: {
  label?: React.ReactNode
  fullWidth?: boolean
  children?: React.ReactNode
  hidden?: boolean
}) => {
  const { depth, tableStartDepth } = useContext(NearestSectionContext)

  return props.fullWidth
    ? <>
      {(!props.hidden && props.label) && (
        <IndentAndLabel fullWidth>
          {props.label}
        </IndentAndLabel>
      )}
      <div className={`flex flex-row ${props.hidden && 'hidden'}`}>
        <Indent depth={depth} tableStartDepth={tableStartDepth} />
        <BodyCell table={tableStartDepth !== undefined} fullWidth>
          {props.children}
        </BodyCell>
      </div>
    </>

    : (
      <div className={`flex flex-row items-stretch ${props.hidden && 'hidden'}`}>
        <IndentAndLabel>
          {props.label}
        </IndentAndLabel>
        <BodyCell table={tableStartDepth !== undefined}>
          {props.children}
        </BodyCell>
      </div>
    )
})

const BodyCell = ({ table, fullWidth, children }: {
  table?: boolean
  fullWidth?: boolean
  children?: React.ReactNode
}) => {
  return (
    <div className={table
      ? `bg-color-base flex-1 flex p-[1px] border ${BORDER_COLOR} ${!fullWidth && 'ml-[-1px]'} mt-[-1px]`
      : `bg-color-base flex-1 flex p-[1px] `}>
      {children}
    </div>
  )
}


const IndentAndLabel = ({ fullWidth, children }: {
  fullWidth?: boolean
  children?: React.ReactNode
}) => {
  const { leftColumnWidth } = useContext(FormLayoutContext)
  const { depth, tableStartDepth } = useContext(NearestSectionContext)
  return (
    <div
      style={{ flexBasis: fullWidth ? undefined : leftColumnWidth }}
      className="flex flex-row items-stretch"
    >
      <Indent depth={depth} tableStartDepth={tableStartDepth} />
      <div className={tableStartDepth === undefined
        ? `text-sm font-semibold p-1 flex-1 flex items-center`
        : `text-sm font-semibold p-1 flex-1 flex items-center border ${BORDER_COLOR} ${BG_COLOR_HEADER} mt-[-1px]`}>
        {children}
      </div>
    </div>
  )
}

const Indent = ({ depth, tableStartDepth }: {
  depth: number
  tableStartDepth: number | undefined
}) => {
  const { indentSizePx } = useContext(FormLayoutContext)

  return <>
    {Array.from({ length: depth }).map((_, index) => (
      <div
        key={index}
        style={{ width: `${indentSizePx}px` }}
        className={(tableStartDepth !== undefined && index >= tableStartDepth)
          ? `inline-block ${BG_COLOR_HEADER} border-l border-b ${BORDER_COLOR} mt-[-1px]`
          : `inline-block`}
      >
      </div>
    ))}
  </>
}

const Spacer = () => {
  return <div className="m-3"></div>
}
// ----------------------------------------
const BG_COLOR_HEADER = 'bg-color-3'
const BORDER_COLOR = 'border-color-5'

// ----------------------------------------

type RootContextValue = {
  leftColumnWidth?: string
  indentSizePx?: number
}
const FormLayoutContext = React.createContext<RootContextValue>({})

// ----------------------------------------

type NearestSectionContextValue = {
  depth: number
  tableStartDepth: number | undefined
}
const NearestSectionContext = React.createContext<NearestSectionContextValue>({
  depth: 0,
  tableStartDepth: undefined,
})

// ----------------------------------------

export const VerticalForm = {
  Root,
  Section,
  Row,
  Spacer,
}

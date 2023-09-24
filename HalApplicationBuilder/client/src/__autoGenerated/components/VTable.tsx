import React, { createContext, useContext, useMemo } from 'react'

// 左列にキー、右列に値が並ぶテーブル

const VTableContext = createContext({ maxIndent: 0 })

const Table = ({ children, maxIndent, headerWidth }: {
  children?: React.ReactNode
  maxIndent?: number
  headerWidth?: string
}) => {
  const contextValue = useMemo(() => ({
    maxIndent: Math.max(maxIndent || 0, 0)
  }), [maxIndent])

  return (
    <VTableContext.Provider value={contextValue}>
      <table className="w-full">
        <colgroup>
          {Array.from({ length: maxIndent || 0 }).map((_, i) => (
            <col key={i} className="w-6" />
          ))}
          <col style={({ width: headerWidth })} />
          <col />
        </colgroup>
        <tbody>
          {children}
        </tbody>
      </table>
    </VTableContext.Provider>
  )
}

type RowProp = {
  indent?: number
  label?: string
  children?: React.ReactNode
  borderless?: boolean
  className?: string
}
const BORDER_OPTION = 'border-neutral-300'

const Row = ({ indent, label, children, borderless, className }: RowProp) => {
  const border = borderless ? '' : `border ${BORDER_OPTION}`
  const bg = borderless ? '' : 'bg-neutral-200'

  const { maxIndent } = useContext(VTableContext)
  const indentCount = useMemo(() => {
    return Math.min(indent || 0, maxIndent)
  }, [maxIndent, indent])
  const thColSpan = useMemo(() => {
    return Math.min(maxIndent - indentCount, maxIndent) + 1
  }, [maxIndent, indentCount])

  return (
    <tr className={className}>
      {Array.from({ length: indentCount }).map((_, i) => (
        <Indent key={i} />
      ))}
      <th colSpan={thColSpan} className={`text-left align-top ${bg} ${border}`}>
        <PropName>{label}</PropName>
      </th>
      <td className={border}>
        {children}
      </td>
    </tr>
  )
}

const NestedName = ({ indent, label, children, className }: RowProp) => {
  const { maxIndent } = useContext(VTableContext)
  const indentCount = useMemo(() => {
    return Math.min(indent || 0, maxIndent)
  }, [maxIndent, indent])
  const thColSpan = useMemo(() => {
    return Math.min(maxIndent - indentCount, maxIndent) + 1
  }, [maxIndent, indentCount])

  return (
    <tr className={className}>
      {Array.from({ length: indentCount }).map((_, i) => (
        <Indent key={i} />
      ))}
      <th colSpan={thColSpan} className={`text-left align-top border-l border-t ${BORDER_OPTION}`}>
        <PropName>{label}</PropName>
      </th>
      <td className={`border-t border-r ${BORDER_OPTION}`}>
        {children}
      </td>
    </tr>
  )
}

const Indent = () => {
  return <th className={`border-l ${BORDER_OPTION}`}></th>
}
const ArrayItemDeleteButtonRow = ({ indent, children, className }: {
  indent?: number
  children?: React.ReactNode
  className?: string
}) => {
  const { maxIndent } = useContext(VTableContext)
  const indentCount = useMemo(() => {
    return Math.min(indent || 0, maxIndent)
  }, [maxIndent, indent])
  const thTdColSpan = useMemo(() => {
    return Math.min(maxIndent + 1 - indentCount, maxIndent + 1) + 1
  }, [maxIndent, indentCount])

  return (
    <tr className={className}>
      {Array.from({ length: indentCount }).map((_, i) => (
        <Indent key={i} />
      ))}
      <td colSpan={thTdColSpan} className={`border-r ${(indentCount == 0 ? 'border-l' : '')} ${BORDER_OPTION}`}>
        <div className="h-2"></div>
        {children}
      </td>
    </tr>
  )
}
const PropName = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  return (
    <span className={`text-sm font-semibold select-none opacity-50 ${className}`}>
      {children}
    </span>
  )
}

export const VTable = {
  VTableContext,
  Table,
  Row,
  NestedName,
  ArrayItemDeleteButtonRow,
}

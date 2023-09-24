import React, { createContext, useContext, useMemo } from 'react'

// 左列にキー、右列に値が並ぶテーブル

const VTableContext = createContext({ maxIndent: 0 })

const Table = ({ children, maxIndent }: {
  children?: React.ReactNode
  maxIndent?: number
}) => {
  const contextValue = useMemo(() => ({
    maxIndent: maxIndent || 0
  }), [maxIndent])

  return (
    <VTableContext.Provider value={contextValue}>
      <table className="w-full">
        <tbody>
          {children}
        </tbody>
      </table>
    </VTableContext.Provider>
  )
}

const Indent = ({ indent }: { indent: number }) => {
  return <th colSpan={indent} className={`w-${indent * 4}`}></th>
}

const Row = ({ indent, label, children, wide, borderless, className }: {
  indent?: number
  label?: string
  children?: React.ReactNode
  wide?: boolean
  borderless?: boolean
  className?: string
}) => {
  const border = borderless ? '' : 'border border-neutral-300'

  const { maxIndent } = useContext(VTableContext)
  const indentColSpan = useMemo(() => {
    return Math.min(indent || 0, maxIndent)
  }, [maxIndent, indent])
  const thColSpan = useMemo(() => {
    return wide
      ? Math.min(maxIndent - indentColSpan, maxIndent) + 2
      : Math.min(maxIndent - indentColSpan, maxIndent) + 1
  }, [maxIndent, indentColSpan, wide])
  const tdColSpan = useMemo(() => {
    return wide
      ? Math.max(maxIndent - indentColSpan, maxIndent) + 1
      : 1
  }, [maxIndent, indent, wide])

  if (wide) {
    return <>
      <tr className={className}>
        {indentColSpan >= 1 && (
          <Indent indent={indentColSpan} />
        )}
        {thColSpan >= 1 && (
          <th colSpan={thColSpan} className={`text-left ${border}`}>
            <PropName>{label}</PropName>
          </th>
        )}
      </tr>
      <tr className={className}>
        {indentColSpan >= 1 && (
          <Indent indent={indentColSpan} />
        )}
        {tdColSpan >= 1 && (
          <td colSpan={tdColSpan} className={border}>
            {children}
          </td>
        )}
      </tr>
    </>

  } else {
    return (
      <tr className={className}>
        {indentColSpan >= 1 && (
          <Indent indent={indentColSpan} />
        )}
        {thColSpan >= 1 && (
          <th colSpan={thColSpan} className={`text-left align-top bg-neutral-200 ${border}`}>
            <PropName>{label}</PropName>
          </th>
        )}
        {tdColSpan >= 1 && (
          <td colSpan={tdColSpan} className={border}>
            {children}
          </td>
        )}
      </tr>
    )
  }
}
const PropName = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  return (
    <span className={`text-sm font-semibold select-none opacity-80 ${className}`}>
      {children}
    </span>
  )
}

export const VTable = {
  VTableContext,
  Table,
  Row,
}

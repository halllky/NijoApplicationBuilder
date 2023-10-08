import React, { createContext, useContext, useMemo } from 'react'

// 左列にキー、右列に値が並ぶテーブル

const VALUE_COLUMN_MAX_LENGTH = '480px'
const BORDER_OPTION = 'border-color-4'
const BG_HEADER = 'bg-color-ridge'

const VTableContext = createContext({ maxIndent: 0 })

const Table = ({ children, maxIndent, headerWidth }: {
  children?: React.ReactNode
  maxIndent?: number
  headerWidth?: string
}) => {
  const contextValue = useMemo(() => ({
    maxIndent: Math.max(maxIndent || 0, 0)
  }), [maxIndent])
  const tableStyle = useMemo(() => ({
    maxWidth: `calc(${headerWidth} + ${VALUE_COLUMN_MAX_LENGTH})`,
  }), [headerWidth])

  return (
    <VTableContext.Provider value={contextValue}>
      <table style={tableStyle} className={`border-b ${BORDER_OPTION}`}>
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

const Row = (props: {
  indent?: number
  label?: string
  children?: React.ReactNode
  keyOnly?: boolean
  valueOnly?: boolean
  className?: string
}) => {

  const indentCount = useMemo(() => {
    return props.indent || 0
  }, [props.indent])

  const { maxIndent } = useContext(VTableContext)
  const thColSpan = useMemo(() => {
    return props.keyOnly
      ? (maxIndent - indentCount + 1) + 1
      : (maxIndent - indentCount + 1)
  }, [props.keyOnly, maxIndent, indentCount])

  const tdColSpan = useMemo(() => {
    return props.valueOnly
      ? 1 + thColSpan
      : 1
  }, [props.valueOnly, thColSpan])

  const noContents = !props.label && !props.children

  return (
    <tr className={props.className}>

      {Array.from({ length: indentCount }).map((_, i) => (
        <th key={i} className={`border-l ${BORDER_OPTION} ${BG_HEADER}`}></th>
      ))}

      {!props.valueOnly && (
        <th colSpan={thColSpan} className={`text-left align-top ${BG_HEADER} border-t border-l border-r ${BORDER_OPTION}`}>
          <div className='flex gap-2 flex-wrap align-middle'>
            <span className={`text-sm font-semibold select-none text-color-7`}>
              {props.label}
            </span>
            {props.keyOnly && props.children}
          </div>
        </th>
      )}

      {!props.keyOnly && (
        <td colSpan={tdColSpan} className={`border-t border-l border-r ${BORDER_OPTION} ${(noContents ? 'h-4' : '')}`}>
          {props.children}
        </td>
      )}

    </tr>
  )
}

const Spacer = (props: {
  indent?: number
}) => {

  const indentCount = useMemo(() => {
    return props.indent || 0
  }, [props.indent])

  const { maxIndent } = useContext(VTableContext)
  const thColSpan = useMemo(() => {
    return (maxIndent - indentCount + 1) + 1
  }, [maxIndent, indentCount])

  return indentCount === 0
    ? (
      <tr>
        <th colSpan={thColSpan} className={`h-6 border-t ${BORDER_OPTION}`}></th>
      </tr>
    ) : (
      <tr>
        {Array.from({ length: indentCount }).map((_, i) => (
          <th key={i} className={`border-l ${BORDER_OPTION} ${BG_HEADER}`}></th>
        ))}
        <th colSpan={thColSpan} className={`h-6 ${BG_HEADER} border-t border-r ${BORDER_OPTION}`}></th>
      </tr>
    )
}

export const VTable = {
  Table,
  Row,
  Spacer,
}

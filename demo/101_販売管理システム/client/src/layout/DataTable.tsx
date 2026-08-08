import React from "react"

export type DataTableProps<TRow> = {
  rows: TRow[]
  columns: DataTableColumn<TRow>[]
  className?: string
}

export type DataTableColumn<TRow> = {
  header: React.ReactNode
  render: (row: TRow) => React.ReactNode
  widthPx?: number
}

/**
 * シンプルなテーブルコンポーネント。
 * 編集の仕組みは特に用意していない。
 * 行選択やセル選択、列幅変更などの機能もない。
 * 列ヘッダは固定表示（sticky）される。
 */
export function DataTable<TRow>(props: DataTableProps<TRow>) {

  const [columnWidths, tableWidth] = React.useMemo(() => {
    const columnWidths: string[] = []
    let totalWidth = 0
    for (const col of props.columns) {
      if (col.widthPx !== undefined) {
        columnWidths.push(`${col.widthPx}px`)
        totalWidth += col.widthPx
      } else {
        columnWidths.push("80px") // デフォルト幅
        totalWidth += 80
      }
    }
    return [columnWidths, totalWidth === 0 ? undefined : `${totalWidth}px`]
  }, [props.columns])

  return (
    // テーブルのコンテナ。 横スクロール可能。
    <div className={`overflow-auto bg-gray-100 ${props.className ?? ""}`}>

      <table
        className="table-fixed border-separate border-spacing-0 border-b border-r border-gray-200"
        style={{ width: tableWidth }}
      >
        <colgroup>
          {columnWidths.map((width, index) => (
            <col key={index} style={{ width }} />
          ))}
        </colgroup>

        <thead className="bg-gray-100 sticky top-0">
          <tr>
            {props.columns.map((col, colIndex) => (
              <th
                key={colIndex}
                className="border-b border-gray-200 px-1 py-px text-left"
                style={col.widthPx ? { width: `${col.widthPx}px` } : undefined}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {props.rows.map((row, rowIndex) => (
            <tr key={rowIndex} className="even:bg-white odd:bg-gray-50">
              {props.columns.map((col, colIndex) => (
                <td key={colIndex} className="px-1 py-px align-top">
                  {(() => {
                    const rendered = col.render(row)
                    if (typeof rendered === 'string' || typeof rendered === 'number') {
                      return (
                        <div className="py-px px-1 truncate">
                          {rendered}
                        </div>
                      )
                    } else {
                      return rendered
                    }
                  })()}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>

    </div>
  )
}

/** グリッドの数値セル。右寄せ・3桁カンマ区切りで表示する。 */
export const NumericCell = ({ value, suffix }: { value: unknown, suffix?: string }) => {
  let displayValue = value
  const num = Number(value)
  if (value !== null && value !== undefined && value !== '' && Number.isFinite(num)) {
    displayValue = num.toLocaleString()
  }
  return (
    <span className="block w-full py-px px-1 truncate text-right">
      {displayValue as React.ReactNode}{suffix}
    </span>
  )
}

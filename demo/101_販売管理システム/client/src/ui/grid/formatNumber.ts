/**
 * 数値を右寄せ表示用に整形する。3桁カンマ区切り・接尾辞つき（旧 ui/DataTable.tsx の NumericCell 相当）。
 * 数値として解釈できない場合は元の値をそのまま文字列化して返す。
 */
export function formatNumber(value: unknown, suffix?: string): string {
  const num = Number(value)
  let displayValue: string
  if (value !== null && value !== undefined && value !== '' && Number.isFinite(num)) {
    displayValue = num.toLocaleString()
  } else {
    displayValue = value === null || value === undefined ? '' : String(value)
  }
  return suffix ? `${displayValue}${suffix}` : displayValue
}

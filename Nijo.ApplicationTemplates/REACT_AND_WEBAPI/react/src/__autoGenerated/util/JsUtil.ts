import dayjs from 'dayjs'

export const groupBy = <TItem, TKey>(arr: TItem[], fn: (t: TItem) => TKey): Map<TKey, TItem[]> => {
  return arr.reduce((map, curr) => {
    const key = fn(curr)
    const group = map.get(key)
    if (group) {
      group.push(curr)
    } else {
      map.set(key, [curr])
    }
    return map
  }, new Map<TKey, TItem[]>())
}

export const crossJoin = <T1, T2, TKey>(
  left: T1[], getKeyLeft: (t: T1) => TKey,
  right: T2[], getKeyRight: (t: T2) => TKey
): CrossJoinResult<T1, T2, TKey>[] => {

  const sortedLeft = [...left]
  sortedLeft.sort((a, b) => {
    const keyA = getKeyLeft(a)
    const keyB = getKeyLeft(b)
    if (keyA < keyB) return -1
    if (keyA > keyB) return 1
    return 0
  })
  const sortedRight = [...right]
  sortedRight.sort((a, b) => {
    const keyA = getKeyRight(a)
    const keyB = getKeyRight(b)
    if (keyA < keyB) return -1
    if (keyA > keyB) return 1
    return 0
  })
  const result: CrossJoinResult<T1, T2, TKey>[] = []
  let cursorLeft = 0
  let cursorRight = 0
  while (true) {
    const left = sortedLeft[cursorLeft]
    const right = sortedRight[cursorRight]
    if (left === undefined && right === undefined) {
      break
    }
    if (left === undefined && right !== undefined) {
      result.push({ key: getKeyRight(right), right })
      cursorRight++
      continue
    }
    if (left !== undefined && right === undefined) {
      result.push({ key: getKeyLeft(left), left })
      cursorLeft++
      continue
    }
    const keyLeft = getKeyLeft(left)
    const keyRight = getKeyRight(right)
    if (keyLeft === keyRight) {
      result.push({ key: keyLeft, left, right })
      cursorLeft++
      cursorRight++
    } else if (keyLeft < keyRight) {
      result.push({ key: keyLeft, left })
      cursorLeft++
    } else if (keyLeft > keyRight) {
      result.push({ key: keyRight, right })
      cursorRight++
    }
  }
  return result
}
type CrossJoinResult<T1, T2, TKey>
  = { key: TKey, left: T1, right: T2 }
  | { key: TKey, left: T1, right?: never }
  | { key: TKey, left?: never, right: T2 }

/** 日付や数値などの表記ゆれを補正する */
export const normalize = (str: string) => str
  .replace(/(\s|　|,|、|，)/gm, '') // 空白、カンマを除去
  .replace('。', '.') // 句点は日本語入力時にピリオドと同じ位置にあるキーなので
  .replace('ー', '-') // NFKCで正規化されないので手動で正規化
  .normalize('NFKC') // 全角を半角に変換

/** 文字列をC#やDBで扱える実数として解釈します。 */
export const tryParseAsNumberOrEmpty = (value: string | undefined)
  : { ok: true, num: number | undefined, formatted: string }
  | { ok: false, num: undefined, formatted: string } => {

  if (value === undefined) return { ok: true, num: undefined, formatted: '' }

  const normalized = normalize(value).replace(',', '') // 桁区切りのカンマを無視
  if (normalized === '') return { ok: true, num: undefined, formatted: '' }

  const num = Number(normalized)
  if (isNaN(num)) return { ok: false, num: undefined, formatted: value }
  if (num === Infinity) return { ok: false, num: undefined, formatted: value }
  return { ok: true, num, formatted: num.toString() }
}

/** 文字列を整数として解釈します。 */
export const tryParseAsIntegerOrEmpty = (value: string | undefined)
  : { ok: true, num: number | undefined, formatted: string }
  | { ok: false, num: undefined, formatted: string } => {

  const { ok, num, formatted } = tryParseAsNumberOrEmpty(value)
  if (!ok) return { ok: false, num, formatted }

  if (!Number.isSafeInteger(num)) return { ok: false, num: undefined, formatted: value ?? '' }
  return { ok: true, num, formatted }
}

/** 文字列を西暦として解釈します。 */
export const tryParseAsYearOrEmpty = (value: string | undefined)
  : { ok: true, year: number | undefined, formatted: string }
  | { ok: false, year: undefined, formatted: string } => {

  const { ok, num, formatted } = tryParseAsIntegerOrEmpty(value)
  if (!ok) return { ok: false, year: num, formatted }
  if (num === undefined) return { ok: true, year: undefined, formatted: '' }

  if (num < 0 || num > 9999) return { ok: false, year: undefined, formatted: value ?? '' }
  return { ok: true, year: num, formatted: num.toString().padStart(4, '0') }
}

/** 文字列を年月として解釈します。 */
export const tryParseAsYearMonthOrEmpty = (value: string | undefined)
  : { ok: true, yyyymm: number | undefined, formatted: string }
  | { ok: false, yyyymm: undefined, formatted: string } => {

  const { ok, num, formatted } = tryParseAsIntegerOrEmpty(value)
  if (!ok) return { ok: false, yyyymm: undefined, formatted }
  if (num === undefined) return { ok: true, yyyymm: undefined, formatted: '' }

  const yyyy = Math.floor(num / 100)
  const mm = num % 100
  if (yyyy < 0 || yyyy > 9999) return { ok: false, yyyymm: undefined, formatted: value ?? '' }
  if (mm < 0 || mm > 12) return { ok: false, yyyymm: undefined, formatted: value ?? '' }

  return { ok: true, yyyymm: num, formatted: `${yyyy.toString().padStart(4, '0')}-${mm.toString().padStart(2, '0')}` }
}

/** 文字列を年月日として解釈します。 */
export const tryParseAsDateOrEmpty = (value: string | undefined)
  : { ok: true, result: string | undefined }
  | { ok: false, result: string | undefined } => {

  if (value === undefined) return { ok: true, result: undefined }
  const normalized = normalize(value)
  if (normalized === '') return { ok: true, result: undefined }

  let parsed = dayjs(normalized, { format: 'YYYY-MM-DD', locale: 'ja' })
  if (!parsed.isValid()) return { ok: false, result: value }

  // 年が未指定の場合、2001年ではなくシステム時刻の年と解釈する
  if (parsed.year() == 2001 && !normalized.includes('2001')) {
    parsed = parsed.set('year', dayjs().year())
  }
  return { ok: true, result: parsed.format('YYYY-MM-DD') }
}

/** 文字列を日付時刻として解釈します。 */
export const tryParseAsDateTimeOrEmpty = (value: string | undefined)
  : { ok: true, result: string | undefined }
  | { ok: false, result: string | undefined } => {

  if (value === undefined) return { ok: true, result: undefined }
  const normalized = normalize(value)
  if (normalized === '') return { ok: true, result: undefined }

  let parsed = dayjs(normalized, { format: 'YYYY-MM-DD HH:mm:ss', locale: 'ja' })
  if (!parsed.isValid()) return { ok: false, result: value }
  return { ok: true, result: parsed.format('YYYY-MM-DD HH:mm:ss') }
}

// ---------------------------------
// TSV変換関数

export function toTsvString(arr: string[][]): string {
  const lines: string[] = []
  for (const row of arr) {
    const line: string[] = []
    for (const cell of row) {
      line.push(`"${cell.replace('"', '""')}"`)
    }
    lines.push(line.join('\t'))
  }
  // Excelへの貼り付けを想定しているので改行はCRLF
  return lines.join('\r\n')
}

export function fromTsvString(tsv: string): string[][] {
  // 改行コードのパターンを正規表現で定義
  const newlinePattern = /\r\n|\n/

  // TSVを行ごとに分割
  const lines = tsv.split(newlinePattern)

  // 各行を値ごとに分割し、ダブルクォーテーションで囲まれている値を取り除く
  return lines.map(line => {
    const values = []
    let currentValue = ''
    let inQuote = false

    for (let i = 0; i < line.length; i++) {
      const char = line[i];

      if (char === '"') {
        inQuote = !inQuote
      } else if (char === '\t' && !inQuote) {
        values.push(currentValue.replace(/""/g, '"'))
        currentValue = ''
      } else {
        currentValue += char
      }
    }

    if (currentValue) {
      values.push(currentValue.replace(/""/g, '"'))
    }

    // 値の途中で改行が含まれている場合の処理
    const mergedValues = []
    let currentMergedValue = ''
    let inQuoteForMerge = false

    for (const value of values) {
      if (value.startsWith('"') && value.endsWith('"')) {
        if (inQuoteForMerge) {
          currentMergedValue += value.slice(1, -1)
        } else {
          if (currentMergedValue) {
            mergedValues.push(currentMergedValue)
          }
          currentMergedValue = value.slice(1, -1)
          inQuoteForMerge = false
        }
      } else if (value.startsWith('"')) {
        if (inQuoteForMerge) {
          currentMergedValue += '\n' + value.slice(1)
        } else {
          if (currentMergedValue) {
            mergedValues.push(currentMergedValue)
          }
          currentMergedValue = value.slice(1)
          inQuoteForMerge = true
        }
      } else if (value.endsWith('"')) {
        currentMergedValue += value.slice(0, -1)
        mergedValues.push(currentMergedValue)
        currentMergedValue = ''
        inQuoteForMerge = false
      } else {
        mergedValues.push(value)
      }
    }

    if (currentMergedValue) {
      mergedValues.push(currentMergedValue)
    }

    return mergedValues
  })
}

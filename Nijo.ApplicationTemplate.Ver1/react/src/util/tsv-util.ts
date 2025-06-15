
/**
 * 2次元配列のデータをTSV文字列に変換する。
 *
 * @param arr 2次元配列のデータ
 * @returns TSV文字列
 */
export function toTsvString(arr: string[][]): string {
  const lines: string[] = []
  for (const row of arr) {
    const line: string[] = []
    for (const cell of row) {
      if (cell.includes('"') || cell.includes('\n')) {
        // 文字列中にダブルクォーテーションや改行コードが出てくる場合はダブルクォーテーション囲み
        const escaped = cell.replace(/"/g, '""')
        line.push(`"${escaped}"`)
      } else {
        line.push(cell)
      }
    }
    lines.push(line.join('\t'))
  }
  // Excelへの貼り付けを想定しているので改行はCRLF
  return lines.join('\r\n')
}

/**
 * TSV文字列を2次元配列のデータに変換する。
 *
 * @param tsv TSV文字列
 * @returns 2次元配列のデータ
 */
export function fromTsvString(tsv: string): string[][] {
  // 改行コードの混在への対策
  const newLineReplaced = tsv.replace(/\r\n/g, '\n')

  const NEWLINE = '\n'
  const SEPARATOR = '\t'
  const QUOTE = '"'

  // 前から1文字ずつ処理して地道にパースする
  const result: string[][] = []
  let currentRow: string[] = []
  let currentValue = ''
  let mode: 'outOfValue' | 'quotedValue' | 'valueWithoutQuote' = 'outOfValue'
  let i = 0
  while (i < newLineReplaced.length) {
    const char = newLineReplaced[i]

    if (mode === 'quotedValue') {
      if (char === QUOTE) {
        // 2文字連続でクォートが出てくる場合はエスケープ
        i++
        const nextChar = newLineReplaced[i]
        if (nextChar === QUOTE) {
          currentValue += nextChar
          i++
          continue
        }

        // クォートで囲まれた値の終わり
        currentRow.push(currentValue)
        currentValue = ''
        mode = 'outOfValue'
        continue // 先読みしているので i++ はやらない

      } else {
        // クォートで囲まれた値に1文字追加
        currentValue += char
        i++
        continue
      }

    } else if (mode === 'valueWithoutQuote') {
      if (char === SEPARATOR) {
        // クォートで囲まれていない値がセパレータの出現により終わる
        currentRow.push(currentValue)
        currentValue = ''
        i++
        continue

      } else if (char === NEWLINE) {
        // クォートで囲まれていない値が改行により終わる
        currentRow.push(currentValue)
        result.push(currentRow)
        currentValue = ''
        currentRow = []
        i++
        continue

      } else {
        // クォートで囲まれていない値に1文字追加
        currentValue += char
        i++
        continue
      }

    } else {
      if (char === SEPARATOR) {
        // 値と値の間
        i++
        continue

      } else if (char === NEWLINE) {
        // 行の終わり
        currentRow.push(currentValue)
        result.push(currentRow)
        currentValue = ''
        currentRow = []
        i++
        continue

      } else if (char === QUOTE) {
        // クォートで囲まれた値の始まり
        mode = 'quotedValue'
        i++
        continue

      } else {
        // クォートで囲まれていない値の始まり
        mode = 'valueWithoutQuote'
        currentValue = char
        i++
        continue
      }

    }
  }

  // TSV文字列の末尾の値の処理
  if (currentValue !== '') {
    currentRow.push(currentValue)
  }
  if (currentRow.length > 0) {
    result.push(currentRow)
  }

  return result
}

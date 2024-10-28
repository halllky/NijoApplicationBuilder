import React, { useCallback } from 'react'
import * as RT from '@tanstack/react-table'
import { DataTableProps } from './DataTable.Public'
import * as Util from '../util'
import { RTColumnDefEx } from './DataTable.Parts'

export const useCopyPaste = <T>(
  api: RT.Table<T>,
  getSelectedRows: () => RT.Row<T>[],
  getSelectedColumns: () => RTColumnDefEx<T>[],
  onChangeRow: DataTableProps<T>['onChangeRow'] | undefined,
  editing: boolean,
) => {
  const [, dispatchToast] = Util.useToastContext()


  /** グリッドの値をクリップボードにコピー */
  const onCopy: React.ClipboardEventHandler = useCallback(e => {
    // セル編集中なら中断
    if (editing) return
    // 選択されているセルの値をstringで取得
    const rows = getSelectedRows()
    const columns = getSelectedColumns()
    const valueTable: string[][] = []
    for (const row of rows) {
      const valueArray: string[] = []
      for (const column of columns) {
        let value: string
        value = column.ex.onClipboardCopy(row.original, row.index)
        valueArray.push(value)
      }
      valueTable.push(valueArray)
    }
    // クリップボードにコピー
    const tsv = Util.toTsvString(valueTable)
    e.clipboardData.setData('Text', tsv)
    e.preventDefault()
  }, [editing, getSelectedRows, getSelectedColumns])


  const executePaste = useCallback((valueTable: string[][]) => {
    // セル編集中なら中断
    if (editing) return
    // 行変更イベントがない場合は貼り付けても意味がないので中断
    if (onChangeRow === undefined) return
    // 貼り付け元の値がない場合は中断
    if (valueTable.length === 0) return
    // 選択範囲がなければ中断
    const selectedRows = getSelectedRows()
    const selectedColumns = getSelectedColumns()
    const topRowIndex = selectedRows[0]?.index
    const leftColumn = selectedColumns[0]
    if (topRowIndex === undefined || leftColumn === undefined) return
    const allColumns = api.getAllLeafColumns()
    const leftColumnIndex = allColumns.findIndex(c => c.id === leftColumn.id)
    if (topRowIndex === -1 || leftColumnIndex === -1) return

    // 選択範囲の左上のセルから順番に値をセットしていく
    const allRows = api.getRowModel().flatRows
    const loopSizeY = Math.max(valueTable.length, selectedRows.length)
    for (let y = 0; y < loopSizeY; y++) {
      const row = allRows[topRowIndex + y]
      if (row === undefined) continue // テーブルの一番下の行を超えたら中断

      // 剰余をとっているのは選択範囲の縦幅がTSVの行数より大きい場合にTSVの先頭からループさせるため
      const valueArray = valueTable[y % valueTable.length]
      if (valueArray.length === 0) continue

      const loopSizeX = Math.max(valueArray.length, selectedColumns.length)
      for (let x = 0; x < loopSizeX; x++) {
        // 剰余をとっているのは選択範囲の横幅がTSVのその行の値の数より大きい場合にTSVの左端からループさせるため
        const strValue = valueArray.length === 0 ? '' : valueArray[x % valueArray.length]

        const column = allColumns[leftColumnIndex + x]
        if (column === undefined) continue // 貼り付け先の列がなければ中断
        const columnDef = column.columnDef as RTColumnDefEx<T>

        // 読み取り専用列なら中断
        if (columnDef.ex.editSetting === undefined || columnDef.ex.editSetting.readOnly?.(row.original, row.index)) continue

        // 列の定義に従って値を設定
        columnDef.ex.editSetting.onClipboardPaste(row.original, strValue, row.index)
      }
      onChangeRow(row.index, row.original)
    }
  }, [editing, onChangeRow, api, getSelectedRows, getSelectedColumns])


  /**
   * クリップボードの値をグリッドに貼り付ける処理。
   * セキュリティ上の問題からnavigator経由での値取得ができないのでonPasteイベントのハンドリングの形で行う。
   */
  const onPaste: React.ClipboardEventHandler = useCallback(e => {
    // クリップボードからTSVを読み取る
    let tsv: string
    try {
      tsv = e.clipboardData.getData('Text')
    } catch {
      dispatchToast(msg => msg.warn('クリップボードからテキストを読み取れませんでした。'))
      return
    }

    // 貼り付け実行
    const valueTable = Util.fromTsvString(tsv)
    executePaste(valueTable)
  }, [executePaste, dispatchToast])


  /**
   * 選択範囲内の値をクリアする。
   * 空文字をペーストするという形で実現しているのでコピペ処理の近くにある。
   */
  const clearSelectedRange = useCallback(() => {
    executePaste([['']])
  }, [executePaste])


  return { onCopy, onPaste, clearSelectedRange }
}

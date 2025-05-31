import React from 'react'
import useEvent from 'react-use-event-hook'
import * as ReactHookForm from 'react-hook-form'
import * as RT from '@tanstack/react-table'
import { useOutsideClick } from '../../util/useOutsideClick'
import { useIME } from '../../util/useIME'
import { ColumnMetadataInternal } from './EditableGrid'
import { CellPosition, EditableGridColumnDef, EditableGridProps } from './types'
import { Virtualizer } from '@tanstack/react-virtual'

/** CellEditorのprops */
export type CellEditorProps<T extends ReactHookForm.FieldValues> = {
  api: RT.Table<T>
  caretCell: CellPosition | undefined
  getPixel: GetPixelFunction
  onChangeEditing: (editing: boolean) => void
  onChangeRow: EditableGridProps<T>['onChangeRow']
}

/** CellEditorのref */
export type CellEditorRef<T> = {
  focus: () => void
  setEditorInitialValue: (value: string | undefined) => void
  startEditing: (cell: RT.Cell<T, unknown>) => void
  textarea: HTMLTextAreaElement | null
}

/**
 * セルの編集を行うコンポーネント。
 * 通常時は透明で表示される。編集モードになると可視化される。
 *
 * キーボードでIME変換が必要な文字が入力された場合、
 * 最初の1文字目がIME変換候補状態で表示されるという動きを実現するため、
 * EditableGrid にフォーカスが当たっているうちは、見えないだけで、必ずこのコンポーネントにフォーカスが当たる。
 */
export const CellEditor = React.forwardRef(<T extends ReactHookForm.FieldValues>({
  api,
  caretCell,
  getPixel,
  onChangeEditing,
  onChangeRow,
}: CellEditorProps<T>,
  ref: React.ForwardedRef<CellEditorRef<T>>
) => {

  // 編集中セルの情報。undefined以外の値が入っているときは編集モード。
  const [editingCellInfo, setEditingCellInfo] = React.useState<{
    row: T
    rowIndex: number
    cellId: string
  } | undefined>(undefined)

  // エディタの値
  const [uncomittedText, setUnComittedText] = React.useState<string>()
  const handleChangeUncomittedText = useEvent((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setUnComittedText(e.target.value)
  })

  // エディタ設定。caretセルが移動するたびに更新される。
  const [caretCellEditingInfo, setCaretCellEditingInfo] = React.useState<EditableGridColumnDef<T> | undefined>()

  const containerRef = React.useRef<HTMLLabelElement>(null)
  const editorRef = React.useRef<HTMLTextAreaElement>(null)

  React.useEffect(() => {
    if (caretCell) {
      const columnDef = (api.getAllLeafColumns()[caretCell.colIndex]?.columnDef.meta as ColumnMetadataInternal<T> | undefined)?.originalColDef
      setCaretCellEditingInfo(columnDef)

      // エディタを編集対象セルの位置に移動させる
      if (containerRef.current) {
        const left = getPixel({ position: 'left', colIndex: caretCell.colIndex })
        const right = getPixel({ position: 'right', colIndex: caretCell.colIndex })
        const top = getPixel({ position: 'top', rowIndex: caretCell.rowIndex })
        const bottom = getPixel({ position: 'bottom', rowIndex: caretCell.rowIndex })
        containerRef.current.style.left = `${left}px`
        containerRef.current.style.top = `${top}px`
        containerRef.current.style.minWidth = `${right - left + 1}px`
        containerRef.current.style.minHeight = `${bottom - top + 1}px`
      }
      // 前のセルで入力した値をクリアする
      // setUnComittedText('')
    } else {
      setCaretCellEditingInfo(undefined)
    }
  }, [caretCell, api, containerRef])
  React.useEffect(() => {
    if (caretCellEditingInfo) editorRef.current?.focus()
  }, [caretCellEditingInfo, getPixel])

  /** 編集開始 */
  const startEditing = useEvent((cell: RT.Cell<T, unknown>) => {
    const columnDef = (cell.column.columnDef.meta as ColumnMetadataInternal<T>)?.originalColDef
    // 値が編集されてもコミットできないので編集開始しない
    if (!onChangeRow) {
      return;
    }

    // 編集不可のセル
    if (!columnDef?.onStartEditing) return
    if (columnDef?.isReadOnly === true) return
    if (typeof columnDef?.isReadOnly === 'function' && columnDef.isReadOnly(cell.row.original, cell.row.index)) return

    setEditingCellInfo({
      cellId: cell.id,
      rowIndex: cell.row.index,
      row: cell.row.original,
    })
    onChangeEditing(true)

    // 現在のセルの値をエディタに渡す
    columnDef.onStartEditing({
      rowIndex: cell.row.index,
      row: cell.row.original,
      setEditorInitialValue: (value: string) => {
        setUnComittedText(value)
      },
    })

    containerRef.current?.scrollIntoView({
      behavior: 'instant',
      block: 'nearest',
      inline: 'nearest',
    })
    editorRef.current?.focus({ preventScroll: true }); // 編集開始時にエディタにフォーカスを当てる
  })

  /** 編集確定 */
  const commitEditing = useEvent(() => {
    if (editingCellInfo === undefined) {
      return;
    }

    let columnDef = caretCellEditingInfo;
    if (columnDef === undefined && editingCellInfo) {
      const cellIdParts = editingCellInfo.cellId.split('_');
      if (cellIdParts.length >= 2) {
        const colId = cellIdParts[1];
        const column = api.getAllLeafColumns().find(col => col.id === colId);
        if (column) {
          columnDef = (column.columnDef.meta as ColumnMetadataInternal<T> | undefined)?.originalColDef;
        }
      }
    }

    if (columnDef === undefined) {
      setEditingCellInfo(undefined);
      onChangeEditing(false);
      return;
    }

    let newRow: T | undefined = undefined
    columnDef.onEndEditing?.({
      rowIndex: editingCellInfo.rowIndex,
      row: editingCellInfo.row,
      value: editorRef.current?.value ?? '',
      setEditedRow: (row: T) => {
        newRow = row
      },
    })
    if (newRow !== undefined) {
      onChangeRow?.({
        changedRows: [{
          rowIndex: editingCellInfo.rowIndex,
          oldRow: editingCellInfo.row,
          newRow,
        }],
      })
    }

    setEditingCellInfo(undefined)
    onChangeEditing(false)
  })

  /** 編集キャンセル */
  const cancelEditing = useEvent(() => {
    setEditingCellInfo(undefined)
    onChangeEditing(false)
  })

  // ----------------------------------
  // イベント
  const { isComposing: isImeOpen } = useIME()
  const handleKeyDown: React.KeyboardEventHandler<HTMLLabelElement> = useEvent(e => {
    if (editingCellInfo) {
      // 編集を終わらせる
      if (e.key === 'Enter' || e.key === 'Tab') {
        if (isImeOpen) return // IMEが開いているときのEnterやTabでは編集終了しないようにする

        // セル内改行のため普通のEnterでは編集終了しないようにする
        if (e.ctrlKey || e.metaKey) {
          commitEditing()
          e.stopPropagation()
          e.preventDefault()
        }

      } else if (e.key === 'Escape') {
        cancelEditing()
        e.preventDefault()
        e.stopPropagation()
      }
    } else {
      // 編集を始める
      if (caretCell && (
        e.key === 'F2'

        // クイック編集（編集モードでない状態でいきなり文字入力して編集を開始する）
        || isImeOpen
        || e.key === 'Process' // IMEが開いている場合のkeyはこれになる
        || !e.ctrlKey && !e.metaKey && e.key.length === 1 /*文字や数字や記号の場合*/
      )) {
        const row = api.getCoreRowModel().flatRows[caretCell.rowIndex]
        const cell = row.getAllCells()[caretCell.colIndex]
        if (cell) startEditing(cell)
        return
      }
    }
  })

  useOutsideClick(containerRef, () => {
    commitEditing()
  }, [commitEditing])

  React.useImperativeHandle(ref, () => ({
    focus: () => editorRef.current?.focus({ preventScroll: true }),
    setEditorInitialValue: setUnComittedText,
    startEditing,
    textarea: editorRef.current,
  }), [startEditing, editorRef, setUnComittedText])

  return (
    <label ref={containerRef}
      className="absolute min-w-4 min-h-4 flex items-stretch outline-none"
      style={{
        zIndex: 30,
        // クイック編集のためCellEditor自体は常に存在し続けるが、セル編集モードでないときは見えないようにする
        opacity: editingCellInfo === undefined ? 0 : undefined,
        pointerEvents: editingCellInfo === undefined ? 'none' : undefined,
        //初期位置
        left: 0,
        top: 0,
      }}
      onKeyDown={handleKeyDown}
      tabIndex={0}
    >

      <textarea
        ref={editorRef}
        value={uncomittedText}
        onChange={handleChangeUncomittedText}
        className="flex-1 bg-white resize-none outline-none border border-gray-950"
      />
    </label>
  )
}) as (<T extends ReactHookForm.FieldValues>(props: CellEditorProps<T> & { ref?: React.ForwardedRef<CellEditorRef<T>> }) => React.ReactNode);

// ----------------------------------
// 座標計算

/**
 * rowIndexやcolIndexから、スクロールエリア内でのx, y座標のピクセルを導出する関数。
 * 列幅変更や行の仮想化を考慮している。
 */
type GetPixelFunction = (args
  : { position: 'top', rowIndex: number, colIndex?: never }
  | { position: 'bottom', rowIndex: number, colIndex?: never }
  | { position: 'left', colIndex: number, rowIndex?: never }
  | { position: 'right', colIndex: number, rowIndex?: never }
) => number

// x,y座標を返す関数
export const useGetPixel = (
  tdRefs: React.RefObject<{ [rowIndexInPropsData: number]: React.RefObject<HTMLTableCellElement>[] }>,
  rowVirtualizer: Virtualizer<HTMLDivElement, Element>,
  estimatedRowHeight: number,
  getColWidthByIndex: (colIndex: number) => number,
): GetPixelFunction => {
  return useEvent(args => {
    // 左右のpxを導出するのに必要な情報は列幅変更フックが持っている
    if (args.position === 'left') {
      let sum = 0
      for (let i = 0; i < args.colIndex; i++) sum += getColWidthByIndex(i)
      return sum
    } else if (args.position === 'right') {
      let sum = 0
      for (let i = 0; i <= args.colIndex; i++) sum += getColWidthByIndex(i)
      return sum
    }

    // 上下の計算について、描画範囲内にあるセルの場合はtd要素のclientRectで、範囲外は計算で導出する
    if (rowVirtualizer.range
      && args.rowIndex >= rowVirtualizer.range.startIndex
      && args.rowIndex <= rowVirtualizer.range.endIndex) {
      const td = tdRefs.current
        ?.[args.rowIndex]
        ?.[0] // ここではtdの縦幅さえ採れればよいので先頭列決め打ち
        ?.current
      if (td) {
        return args.position === 'top'
          ? td.offsetTop
          : (td.offsetTop + td.offsetHeight)
      }
    }
    if (args.position === 'top') {
      return args.rowIndex * estimatedRowHeight
    } else {
      return (args.rowIndex + 1) * estimatedRowHeight
    }
  })

}

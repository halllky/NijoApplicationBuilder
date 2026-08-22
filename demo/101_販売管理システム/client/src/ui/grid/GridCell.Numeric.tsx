import React from "react"
import * as EG2 from "@halllky/react-editable-grid"
import { NumericTextBox } from "../NumericTextBox"

export type NumericCellEditorOptions = {
  integerDigit?: number
  decimalDigit?: number
  commaSeparated?: boolean
}

/**
 * 数値セル用のエディタをキャッシュして返す。
 * プロジェクトの NumericTextBox をそのままオーバーレイの中に描画することで、
 * 桁数制限・IME正規化・ペーストフィルタといった既存の入力挙動を複製せずに再利用する。
 * キャッシュキーは props の組み合わせ（同じ組み合わせなら同一の識別子を返す）。
 */
const numericEditorCache = new Map<string, EG2.EditableGridCellEditor>()

export function getNumericCellEditor(options: NumericCellEditorOptions): EG2.EditableGridCellEditor {
  const key = JSON.stringify(options)
  const cached = numericEditorCache.get(key)
  if (cached) return cached

  const Editor: EG2.EditableGridCellEditor = React.forwardRef(function NumericCellEditor({ style, isEditing, requestCommit, requestCancel }, ref) {
    const [value, setValue] = React.useState('')
    const inputRef = React.useRef<HTMLInputElement>(null)

    const handleChange: React.ChangeEventHandler<HTMLInputElement> = e => {
      setValue(e.target.value)
    }
    const handleKeyDown: React.KeyboardEventHandler<HTMLInputElement> = e => {
      if (!isEditing) return
      if (e.nativeEvent.isComposing) return
      if (e.key === 'Enter') {
        requestCommit(normalizeNumericString(inputRef.current?.value ?? value, options))
        e.preventDefault()
      } else if (e.key === 'Escape') {
        requestCancel()
        e.preventDefault()
      }
    }

    React.useImperativeHandle(ref, () => ({
      getCurrentValue: () => normalizeNumericString(inputRef.current?.value ?? value, options),
      setValueAndSelectAll: (v, timing) => {
        setValue(v)
        if (timing === 'move-focus' || timing === 'edit-end') {
          setTimeout(() => inputRef.current?.select(), 0)
        }
      },
      getDomElement: () => inputRef.current,
    }), [])

    return (
      <div style={style} className="bg-white">
        <NumericTextBox
          ref={inputRef}
          value={value}
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          integerDigit={options.integerDigit}
          decimalDigit={options.decimalDigit}
          commaSeparated={options.commaSeparated}
          className="w-full h-full border border-black"
        />
      </div>
    )
  })

  numericEditorCache.set(key, Editor)
  return Editor
}

/**
 * NumericTextBox の入力フィルタ（onBeforeInput / onCompositionEnd）を経由しない経路
 * （プログラムによる setValueAndSelectAll、および Ctrl+V によるグリッド全体貼り付け）のための
 * 正規化・桁クランプ。NumericTextBox.tsx の normalize / checkDigits と同じルールを適用する。
 */
export function normalizeNumericString(raw: string, options: NumericCellEditorOptions): string {
  let val = raw
    .replace(/[０-９]/g, s => String.fromCharCode(s.charCodeAt(0) - 0xFEE0))
    .replace(/[．。]/g, '.')
    .replace(/,/g, '')
    .replace(/[−－ー‐–—―]/g, '-')

  if (val.endsWith('.')) val = val.slice(0, -1)

  if (!/^-?[0-9]*(\.[0-9]*)?$/.test(val)) {
    val = val.replace(/[^0-9.\-]/g, '')
  }

  const allowDecimal = options.decimalDigit !== undefined && options.decimalDigit > 0
  const parts = val.split('.')
  let intPart = (parts[0] ?? '').replace('-', '')
  let decPart = allowDecimal ? parts[1] : undefined

  if (options.integerDigit !== undefined) {
    intPart = intPart.slice(0, options.integerDigit)
  }
  if (options.decimalDigit !== undefined && decPart !== undefined) {
    decPart = decPart.slice(0, options.decimalDigit)
  }

  let result = (val.startsWith('-') ? '-' : '') + intPart
  if (decPart !== undefined) {
    result += '.' + decPart
  }
  return result
}

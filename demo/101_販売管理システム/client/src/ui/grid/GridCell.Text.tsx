import React from "react"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"

/**
 * テキストセル用のエディタをキャッシュして返す。
 *
 * CellEditor はエディタコンポーネントの識別子（参照）を state に保持するため、
 * 列定義を再生成するたびに新しいコンポーネントを作ってしまうと、
 * セル編集中でなくても再マウントが発生してしまう。
 * maxLength ごとにモジュールスコープでキャッシュすることで識別子を安定させる。
 *
 * 単一行・複数行のどちらも同じ textarea ベースのエディタを使う
 * （EditableGrid2 側の wrap:true 指定でサイズの伸び方が変わるだけで、
 * 表示・編集コンポーネント自体を分ける必要はない）。
 */
const textEditorCache = new Map<number | undefined, EG2.EditableGridCellEditor>()

export function getTextCellEditor(maxLength: number | undefined): EG2.EditableGridCellEditor {
  const cached = textEditorCache.get(maxLength)
  if (cached) return cached

  const Editor: EG2.EditableGridCellEditor = React.forwardRef(function TextCellEditor({ style, isEditing, requestCommit, requestCancel }, ref) {
    const [value, setValue] = React.useState('')
    const textareaRef = React.useRef<HTMLTextAreaElement>(null)

    // WordTextBox / DescriptionTextArea と同様、前後の空白削除 + Unicode正規化(NFKC) を確定時に行う
    const normalize = (v: string) => {
      let val = v.trim().normalize('NFKC')
      if (maxLength !== undefined && val.length > maxLength) {
        val = val.slice(0, maxLength)
      }
      return val
    }

    const handleChange: React.ChangeEventHandler<HTMLTextAreaElement> = e => {
      setValue(e.target.value)
    }

    const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = e => {
      if (!isEditing) return
      if (e.nativeEvent.isComposing) return // 日本語入力中は無視

      if (e.key === 'Enter') {
        if (e.shiftKey) return // セル内改行のため、Shift無しのEnterでのみ編集終了する
        requestCommit(normalize(value))
        e.preventDefault()
      } else if (e.key === 'Escape') {
        requestCancel()
        e.preventDefault()
      }
    }

    React.useImperativeHandle(ref, () => ({
      getCurrentValue: () => normalize(textareaRef.current?.value ?? ''),
      setValueAndSelectAll: (v, timing) => {
        setValue(v)
        if (timing === 'move-focus' || timing === 'edit-end') {
          setTimeout(() => textareaRef.current?.select(), 0)
        }
      },
      getDomElement: () => textareaRef.current,
    }), [])

    return (
      <textarea
        ref={textareaRef}
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        maxLength={maxLength}
        spellCheck={false}
        autoComplete="off"
        className="px-[3px] resize-none field-sizing-content outline-none border border-black bg-white"
        style={style}
      />
    )
  })

  textEditorCache.set(maxLength, Editor)
  return Editor
}

import React, { useState, useRef, useEffect, useLayoutEffect, useImperativeHandle } from "react"
import { createPortal } from "react-dom"
import * as ReactHookForm from "react-hook-form"
import * as EG2 from "@nijo/ui-components/layout/EditableGrid2"
import { useSchemaCandidates } from "../ProjectPage/SchemaCandidatesContext"
import { ATTR_TYPE, EditingMember, EditingMemberType } from "../backend"
import { useFieldValidationError } from "../ProjectPage/useValidation"

export type CreateComboBoxCellFunction = <TRow>(
  header: string,
  options?: Partial<EG2.EditableGrid2LeafColumn<TRow>>
) => EG2.EditableGrid2LeafColumn<TRow>

/**
 * {@link EditingMemberType} を、種類コンボボックスが扱うフラットな文字列に変換する。
 * C#側の EditingMemberType.ToAttributeValue (Nijo/WebService/SchemaEditor2/EditingMember.cs) と対になる。
 */
function memberTypeToText(type: EditingMemberType): string {
  switch (type.kind) {
    case 'child': return 'child'
    case 'children': return 'children'
    case 'ref-to': return `ref-to:${type.refToPath.join('/')}`
    case 'value': return type.valueTypeName
    case 'unknown': return type.rawValue ?? ''
  }
}

/**
 * 種類コンボボックスに入力されたフラットな文字列を {@link EditingMemberType} に変換する。
 * C#側の EditingMemberType.FromAttributeValue と対になる。
 *
 * "このプロジェクトで定義済みの値の種類名かどうか"（kind:'value' か 'unknown' か）の判定はここでは行わない。
 * どちらの kind でもテキストへの復元結果 (`memberTypeToText`) は同じであり、
 * 保存時にサーバーへ送るXML属性値も同じになるため、判別を誤っても実害が無い。
 * 保存・再読み込みを経ればサーバー側の権威ある判定で上書きされる。
 */
function textToMemberType(text: string): EditingMemberType {
  const trimmed = text.trim()
  if (trimmed === 'child') return { kind: 'child' }
  if (trimmed === 'children') return { kind: 'children' }
  if (trimmed.startsWith('ref-to:')) return { kind: 'ref-to', refToPath: trimmed.slice('ref-to:'.length).split('/') }
  return { kind: 'unknown', rawValue: trimmed }
}

/**
 * ノード種別のコンボボックス列（テキスト入力可 + ドロップダウン選択）。
 * 一応、選択肢に存在しない値も入力可能。
 * ドロップダウンの候補は React Context 経由で取得する。
 */
export function createComboBoxCellHelper(
  getValues: ReactHookForm.UseFormGetValues<ReactHookForm.FieldValues>,
  setValue: ReactHookForm.UseFormSetValue<ReactHookForm.FieldValues>,
  control: ReactHookForm.Control<ReactHookForm.FieldValues>,
  arrayName: string,
  skipFirstRow: boolean | undefined,
): CreateComboBoxCellFunction {

  return (header, options) => {
    return {
      renderHeader: () => (
        <div className="px-1 py-px truncate text-sm text-gray-700">
          {header}
        </div>
      ),
      renderBody: ({ context }) => {
        const fieldRowIndex = skipFirstRow ? context.row.index + 1 : context.row.index
        const rowData: EditingMember = ReactHookForm.useWatch({ control, name: `${arrayName}.${fieldRowIndex}` })
        const { hasError, errorMessages } = useFieldValidationError(rowData.uniqueId, ATTR_TYPE)
        const text = memberTypeToText(rowData.type)

        // 論理名
        const { items } = useSchemaCandidates()
        const displayText = items.find(item => item.value === text)?.text

        // ComboBoxなので、値そのものを表示する
        return (
          <div
            title={errorMessages.join('\n')}
            className={`w-full self-start flex items-center gap-2 px-1 truncate ${hasError ? 'bg-amber-300/50' : ''}`}
          >
            <span className="truncate">
              {text}
            </span>
            {displayText && displayText !== text && (
              <span className="flex-1 text-gray-400 text-sm truncate">
                {displayText}
              </span>
            )}
          </div>
        )
      },
      editor: TypeComboEditor,
      getValueForEditor: ({ rowIndex }) => {
        const fieldRowIndex = skipFirstRow ? rowIndex + 1 : rowIndex
        const type: EditingMemberType | undefined = getValues(`${arrayName}.${fieldRowIndex}.type`)
        return type ? memberTypeToText(type) : ''
      },
      setValueFromEditor: ({ rowIndex, value }) => {
        const fieldRowIndex = skipFirstRow ? rowIndex + 1 : rowIndex
        setValue(
          `${arrayName}.${fieldRowIndex}.type`,
          textToMemberType(value) as ReactHookForm.PathValue<ReactHookForm.FieldValues, 'type'>,
          { shouldDirty: true }
        )
      },
      onCellKeyDown: ({ event, requestEditStart }) => {
        const alt = event.altKey || event.metaKey
        const upDown = event.key === 'ArrowUp' || event.key === 'ArrowDown'
        // 文字入力でも編集開始したいので、シングルキャラクターなどのチェックも入れたほうが親切だが、
        // 最低限 Dropdown と合わせるなら Enter or Alt+Arrow で開始。
        // ただし ComboBox なので任意のキー入力で編集開始してほしい場合が多い。
        // ここでは Dropdown に合わせつつ、任意の文字キー入力は EditableGrid2 のデフォルト挙動に任せる。
        if (event.key === 'Enter' || (alt && upDown)) {
          requestEditStart()
          event.preventDefault()
        }
      },
      ...options,
    }
  }
}

/**
 * ComboBox列のセルエディタ
 */
const TypeComboEditor: EG2.EditableGridCellEditor = React.forwardRef((props, ref) => {
  const inputRef = useRef<HTMLInputElement>(null)
  const listRef = useRef<HTMLUListElement>(null)
  const wrapperRef = useRef<HTMLDivElement>(null)

  // テキストボックスの値
  const [value, setVal] = useState('')

  // 選択肢
  const { items, isLoading } = useSchemaCandidates()
  const [inputted, setInputted] = useState(false) // 入力されたかどうか（ドロップダウンを開いた瞬間は全選択肢を表示したいので）
  const filteredItems = React.useMemo(() => {
    if (!value) return items
    if (!inputted) return items
    const lowerValue = value.toLowerCase()
    return items.filter(item => item.value.toLowerCase().includes(lowerValue))
  }, [items, value, inputted])

  // ドロップダウンの状態
  const [selectedIndex, setSelectedIndex] = useState(-1)
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({})

  // 外部からの制御
  useImperativeHandle(ref, () => ({
    getCurrentValue: () => inputRef.current?.value ?? '',
    setValueAndSelectAll: (v, timing) => {
      setVal(v)
      setInputted(false)
      if (timing === 'move-focus' || timing === 'edit-end') {
        setTimeout(() => inputRef.current?.select(), 0)
      }
    },
    getDomElement: () => inputRef.current,
  }))

  // 選択肢に自動フォーカス
  React.useEffect(() => {
    if (!props.isEditing) return
    if (filteredItems.length === 0) return
    if (selectedIndex === -1 || selectedIndex >= filteredItems.length) {
      setSelectedIndex(0)
    }
  }, [props.isEditing, filteredItems])

  // ドロップダウン位置計算
  useLayoutEffect(() => {
    if (props.isEditing && wrapperRef.current) {
      // Gridのエディタは position: fixed ではなく absolute で配置されていることが多いが、
      // 画面端の判定には getBoundingClientRect を使う
      const rect = wrapperRef.current.getBoundingClientRect()
      const windowHeight = window.innerHeight
      const spaceBelow = windowHeight - rect.bottom
      const spaceAbove = rect.top

      const newStyle: React.CSSProperties = {
        position: 'fixed',
        left: rect.left,
        minWidth: '320px',
        maxWidth: window.innerWidth - rect.left - 10,
        zIndex: 9999,
      }

      // 下が狭くて上が広いなら上に出す (AsyncComboBoxのロジック参考)
      if (spaceBelow < 250 && spaceAbove > spaceBelow) {
        newStyle.bottom = windowHeight - rect.top + 4
        newStyle.maxHeight = Math.min(spaceAbove - 10, 240) // max-h-60 相当
      } else {
        newStyle.top = rect.bottom + 4
        newStyle.maxHeight = Math.min(spaceBelow - 10, 240) // max-h-60 相当
      }

      setDropdownStyle(newStyle)
    }
  }, [props.isEditing, filteredItems.length])

  // キーボード操作でリストスクロール
  useEffect(() => {
    if (props.isEditing && listRef.current && selectedIndex >= 0) {
      const selectedElement = listRef.current.children[selectedIndex] as HTMLElement
      if (selectedElement) {
        selectedElement.scrollIntoView({ block: 'nearest' })
      }
    }
  }, [selectedIndex, props.isEditing])

  const handleKeyDown: React.KeyboardEventHandler<HTMLInputElement> = (e) => {
    if (!props.isEditing) return
    if (e.nativeEvent.isComposing) return // 日本語入力中は無視

    // 候補選択操作
    if (filteredItems.length > 0) {
      if (e.key === 'ArrowDown') {
        e.preventDefault()
        setSelectedIndex(prev => (prev < filteredItems.length - 1 ? prev + 1 : 0))
        return
      }
      if (e.key === 'ArrowUp') {
        e.preventDefault()
        setSelectedIndex(prev => (prev > 0 ? prev - 1 : filteredItems.length - 1))
        return
      }
    }

    // 編集確定
    if (e.key === 'Enter') {
      e.preventDefault()
      props.requestCommit(selectedIndex >= 0 && selectedIndex < filteredItems.length
        ? filteredItems[selectedIndex].value
        : (e.target as HTMLInputElement).value)
      return
    }

    // 編集キャンセル
    if (e.key === 'Escape') {
      // 編集自体をキャンセル
      props.requestCancel()
      e.preventDefault()
    }
  }

  return (
    <div style={props.style} ref={wrapperRef} className="relative">
      <input
        ref={inputRef}
        value={value}
        onChange={ev => { setVal(ev.target.value); setInputted(true) }}
        onKeyDown={handleKeyDown}
        className="w-full max-h-full px-[3px] border border-gray-700 outline-none bg-white"
      />

      {props.isEditing && createPortal(
        <ul
          ref={listRef}
          style={dropdownStyle}
          className="fixed bg-white border border-gray-700 overflow-auto shadow-lg list-none p-0 m-0 text-left"
        >
          {isLoading && filteredItems.length === 0 && (
            <li className="p-2 text-gray-500 text-sm">検索中...</li>
          )}

          {filteredItems.length === 0 && !isLoading && (
            <li className="p-2 text-gray-500 text-sm">該当なし</li>
          )}

          {filteredItems.map((item, index) => (
            <li
              key={item.value}
              className={`px-1 py-px cursor-pointer border-b border-gray-100 last:border-none ${index === selectedIndex ? 'bg-blue-100' : 'hover:bg-blue-50'}`}
              onClick={() => props.requestCommit(item.value)}
              onMouseDown={e => e.stopPropagation()} // EditableGrid2 が外側クリックで編集終了扱いにしないようにする
              onMouseMove={() => setSelectedIndex(index)}
            >
              {item.value}
              {item.text !== item.value && (
                <span className="text-gray-400 text-sm ml-2">
                  {item.text}
                </span>
              )}
            </li>
          ))}
        </ul>,
        document.body
      )}
    </div>
  )
})


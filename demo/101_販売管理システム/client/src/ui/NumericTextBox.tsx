import React from "react"

export type NumericTextBoxProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, "maxLength" | "prefix"> & {
  /** 整数部の最大桁数 */
  integerDigit?: number
  /** 小数部の最大桁数 */
  decimalDigit?: number
  /** 3桁カンマ区切り表示 */
  commaSeparated?: boolean
  /** 接頭辞 */
  prefix?: string
  /** 接尾辞 */
  suffix?: string
}

/**
 * 数値入力テキストボックス。
 * 通常の `input type="text"` に加え、以下の機能を持つ。
 *
 * - 基本的な共通レイアウト（以下、例）
 *   - 右寄せ
 *   - 入力可能なら枠と背景色を変更
 *   - 入力不可能なら枠なし背景色なし
 *   - spellcheck や autocomplete を無効化
 * - 3桁カンマ区切り表示オプション
 * - フォーカスアウト時に半角数値への正規化を自動実行
 * - 入力時に数値以外の入力をブロック
 * - integerDigit, decimalDigit による桁数制御
 */
export const NumericTextBox = React.forwardRef<HTMLInputElement, NumericTextBoxProps>((props, ref) => {
  const {
    integerDigit,
    decimalDigit,
    commaSeparated,
    className,
    readOnly,
    disabled,
    onBlur,
    onFocus,
    onChange,
    onPaste,
    onDrop,
    onCompositionEnd,
    onBeforeInput,
    prefix,
    suffix,
    ...rest
  } = props

  const innerRef = React.useRef<HTMLInputElement>(null)
  React.useImperativeHandle(ref, () => innerRef.current!)

  // フォーカス時にカンマを除去
  const handleFocus = (e: React.FocusEvent<HTMLInputElement>) => {
    if (commaSeparated && !readOnly && !disabled) {
      const val = e.target.value.replace(/,/g, '')
      if (val !== e.target.value) {
        setNativeValue(e.target, val)
      }
    }
    onFocus?.(e)
    e.target.select()
  }

  // フォーカスアウト時に正規化＆カンマ付与
  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    let val = e.target.value
    // 全角→半角
    val = normalize(val)

    // 末尾のドットを削除
    if (val.endsWith('.')) {
      val = val.slice(0, -1)
    }

    if (commaSeparated) {
      const numStr = val.replace(/,/g, '')
      const num = Number(numStr)
      if (!isNaN(num) && numStr !== '' && numStr !== '-') {
        const parts = numStr.split('.')
        parts[0] = Number(parts[0]).toLocaleString()
        val = parts.join('.')
      }
    }

    if (val !== e.target.value) {
      setNativeValue(e.target, val)
    }
    onBlur?.(e)
  }

  const handleBeforeInput: React.InputEventHandler<HTMLInputElement> = (e) => {
    // IME入力中はチェックしない（compositionEndで処理する）
    if (e.nativeEvent.isComposing) {
      onBeforeInput?.(e)
      return
    }

    // ペーストやドロップは専用のハンドラで処理するため、ここではチェックしない
    if (e.nativeEvent.inputType?.startsWith('insertFrom')) {
      onBeforeInput?.(e)
      return
    }

    // data が null の場合は削除操作などの可能性があるため、ここではチェックしない
    if (!e.nativeEvent.data) {
      onBeforeInput?.(e)
      return
    }

    // 許可されていない文字を挿入しようとした場合はブロック
    const allowDecimal = decimalDigit !== undefined && decimalDigit > 0
    const allowedPattern = allowDecimal ? /^[0-9\-\.]$/ : /^[0-9\-]$/
    if (!allowedPattern.test(e.nativeEvent.data)) {
      e.preventDefault()
      onBeforeInput?.(e)
      return
    }

    // 入力後の文字列が正しい数値形式かチェックする。
    const input = e.currentTarget
    const start = input.selectionStart ?? 0
    const end = input.selectionEnd ?? 0

    const currentValue = input.value
    const nextValue = currentValue.slice(0, start) + e.nativeEvent.data + currentValue.slice(end)

    // 「負の符号 + 数値」という形式でなければブロック
    if (!/^-?[0-9]*(\.[0-9]*)?$/.test(nextValue)) {
      e.preventDefault()
      onBeforeInput?.(e)
      return
    }

    // 桁数チェック
    if (!checkDigits(nextValue, integerDigit, decimalDigit)) {
      e.preventDefault()
      onBeforeInput?.(e)
      return
    }

    onBeforeInput?.(e)
  }

  const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
    e.preventDefault()
    const text = e.clipboardData.getData('text')
    const normalized = normalize(text)
    insertTextSafety(e.currentTarget, normalized, integerDigit, decimalDigit)

    onPaste?.(e)
  }

  const handleDrop = (e: React.DragEvent<HTMLInputElement>) => {
    e.preventDefault()
    const text = e.dataTransfer.getData('text')
    const normalized = normalize(text)
    insertTextSafety(e.currentTarget, normalized, integerDigit, decimalDigit)

    onDrop?.(e)
  }

  const handleCompositionEnd = (e: React.CompositionEvent<HTMLInputElement>) => {
    const input = e.currentTarget
    const val = input.value

    let normalized = normalize(val)
    // 許可文字以外除去
    const allowDecimal = decimalDigit !== undefined && decimalDigit > 0
    if (allowDecimal) {
      normalized = normalized.replace(/[^0-9\-\.]/g, '')
    } else {
      normalized = normalized.replace(/[^0-9\-]/g, '')
    }

    // 形式チェック & 修正
    if (!/^-?[0-9]*(\.[0-9]*)?$/.test(normalized) || !checkDigits(normalized, integerDigit, decimalDigit)) {
      // 1. 先頭以外のマイナスを消す
      if (normalized.lastIndexOf('-') > 0) {
        normalized = normalized.replace(/(?!^)-/g, '')
      }
      // 2. 最初のドット以外を消す
      const firstDotIndex = normalized.indexOf('.')
      if (firstDotIndex !== -1) {
        normalized = normalized.slice(0, firstDotIndex + 1) + normalized.slice(firstDotIndex + 1).replace(/\./g, '')
      }

      // 3. 桁数制限
      const parts = normalized.split('.')
      let intPart = parts[0].replace('-', '')
      let decPart = parts[1]

      if (integerDigit !== undefined && intPart.length > integerDigit) {
        intPart = intPart.slice(0, integerDigit)
      }
      if (decimalDigit !== undefined && decPart !== undefined && decPart.length > decimalDigit) {
        decPart = decPart.slice(0, decimalDigit)
      }

      let newVal = (normalized.startsWith('-') ? '-' : '') + intPart
      if (decPart !== undefined) {
        newVal += '.' + decPart
      }

      if (val !== newVal) {
        setNativeValue(input, newVal)
      }
    } else {
      if (val !== normalized) {
        setNativeValue(input, normalized)
      }
    }

    onCompositionEnd?.(e)
  }

  const wrapperBaseStyle = "inline-flex items-center border overflow-hidden"
  const wrapperEditableStyle = "border-gray-700 bg-white focus-within:ring-1 focus-within:ring-blue-500 focus-within:border-blue-500"
  const wrapperReadOnlyStyle = "border-transparent bg-transparent"

  const inputStyle = "flex-1 min-w-0 py-px px-1 bg-transparent border-none outline-none text-right disabled:cursor-not-allowed"

  const isEditable = !readOnly && !disabled

  return (
    <div
      className={`${wrapperBaseStyle} ${isEditable ? wrapperEditableStyle : wrapperReadOnlyStyle} ${className ?? ''}`}
      onClick={() => {
        if (isEditable) innerRef.current?.focus()
      }}
    >
      {prefix && (
        <span className="pl-1 select-none whitespace-nowrap">
          {prefix}
        </span>
      )}
      <input
        type="text"
        inputMode={decimalDigit && decimalDigit > 0 ? "decimal" : "numeric"}
        ref={innerRef}
        readOnly={readOnly}
        disabled={disabled}
        spellCheck={false}
        autoComplete="off"
        className={inputStyle}
        onFocus={handleFocus}
        onBlur={handleBlur}
        onChange={onChange}
        onBeforeInput={handleBeforeInput}
        onPaste={handlePaste}
        onDrop={handleDrop}
        onCompositionEnd={handleCompositionEnd}
        {...rest}
      />
      {suffix && (
        <span className="pr-1 select-none whitespace-nowrap">
          {suffix}
        </span>
      )}
    </div>
  )
})

/**
 * 文字列を正規化する。許可されない文字の除去はしない。
 */
const normalize = (str: string) => {
  // 全角→半角
  let val = str.replace(/[０-９]/g, (s) => String.fromCharCode(s.charCodeAt(0) - 0xFEE0))
  // 全角ドット→半角ドット
  val = val.replace(/[．。]/g, '.')
  // カンマ除去
  val = val.replace(/,/g, '')
  // マイナス記号の正規化
  val = val.replace(/[−－ー‐–—―]/g, '-')
  return val
}

/**
 * プログラム上で正規化された値をDOM要素にセットし、inputイベントを発火させる。
 */
const setNativeValue = (element: HTMLInputElement, value: string) => {
  const nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value")?.set;
  nativeInputValueSetter?.call(element, value);
  const event = new Event('input', { bubbles: true });
  element.dispatchEvent(event);
}

/**
 * 桁数チェック
 */
const checkDigits = (value: string, integerDigit?: number, decimalDigit?: number) => {
  const parts = value.replace('-', '').split('.')
  const integerPart = parts[0]
  const decimalPart = parts[1]

  if (integerDigit !== undefined && integerPart.length > integerDigit) {
    return false
  }
  if (decimalDigit !== undefined && decimalPart !== undefined && decimalPart.length > decimalDigit) {
    return false
  }
  if ((decimalDigit === undefined || decimalDigit === 0) && decimalPart !== undefined) {
    return false
  }

  return true
}

/**
 * 安全にテキストを挿入する。数値以外の文字列は無視される。
 */
const insertTextSafety = (input: HTMLInputElement, normalizedText: string, integerDigit?: number, decimalDigit?: number) => {
  // 挿入可能な文字が一切無い場合は以降の処理をスキップ
  if (!/^[0-9\-\.]*$/.test(normalizedText)) return

  const start = input.selectionStart ?? 0
  const end = input.selectionEnd ?? 0
  const currentValue = input.value

  // 挿入後の文字列をシミュレーション
  const newValue = currentValue.slice(0, start) + normalizedText + currentValue.slice(end)

  // 形式チェック
  if (!/^-?[0-9]*(\.[0-9]*)?$/.test(newValue)) return

  // 桁数チェック
  if (!checkDigits(newValue, integerDigit, decimalDigit)) return

  setNativeValue(input, newValue)

  const newCursorPos = start + normalizedText.length
  requestAnimationFrame(() => {
    input.setSelectionRange(newCursorPos, newCursorPos)
  })
}

import React, { useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState } from "react"
import { useIMEOpened } from "../util"
import { DropDownApi, TextInputBase } from "./TextInputBase"
import { CustomComponentProps, CustomComponentRef, defineCustomComponent, normalize } from "./InputBase"

export const ComboBoxBase = defineCustomComponent(<T extends {}>(
  props2: CustomComponentProps<T, {
    options: T[]
    keySelector: (item: T) => string
    textSelector: (item: T) => string
    onKeywordChanged?: (keyword: string | undefined) => void
  }>,
  ref: React.ForwardedRef<CustomComponentRef<T>>
) => {
  const {
    options,
    keySelector,
    textSelector,
    value,
    onChange,
    onBlur,
    readOnly,
    onKeywordChanged,
    name,
  } = props2

  const dropdownRef = useRef<DropDownApi>(null)

  // フィルタリング
  const [keyword, setKeyword] = useState<string | undefined>(undefined) // フォーカスを当ててから何か入力された場合のみundefinedでなくなる
  useEffect(() => {
  }, [options, keyword])
  const filtered = useMemo(() => {
    if (keyword === undefined) return [...options]
    const normalized = normalize(keyword)
    if (!normalized) return [...options]
    return options.filter(item => textSelector(item).includes(normalized))
  }, [options, keyword, textSelector])

  // リストのカーソル移動
  const [highlighted, setHighlightItem] = useState('')
  const highlightAnyItem = useCallback(() => {
    if (value) {
      setHighlightItem(keySelector(value))
    } else if (filtered.length > 0) {
      setHighlightItem(keySelector(filtered[0]))
    } else {
      setHighlightItem('')
    }
  }, [value, filtered, keySelector])
  const highlightUpItem = useCallback(() => {
    const currentIndex = filtered.findIndex(item => keySelector(item) === highlighted)
    if (currentIndex === -1) {
      if (filtered.length > 0) setHighlightItem(keySelector(filtered[0]))
    } else if (currentIndex > 0) {
      setHighlightItem(keySelector(filtered[currentIndex - 1]))
    }
  }, [filtered, highlighted, keySelector])
  const highlightDownItem = useCallback(() => {
    const currentIndex = filtered.findIndex(item => keySelector(item) === highlighted)
    if (currentIndex === -1) {
      if (filtered.length > 0) setHighlightItem(keySelector(filtered[0]))
    } else if (currentIndex < (filtered.length - 1)) {
      setHighlightItem(keySelector(filtered[currentIndex + 1]))
    }
  }, [filtered, highlighted, keySelector])

  // 選択
  const selectItemByValue = useCallback((value: string | undefined) => {
    const foundItem = options.find(item => keySelector(item) === value)
    setHighlightItem(foundItem ? keySelector(foundItem) : '')
    setKeyword(undefined)
    onChange?.(foundItem)
  }, [options, onChange, keySelector])

  // 入力中のテキストに近い最も適当な要素を取得する
  const getHighlightedOrAnyItem = useCallback(() => {
    if (highlighted && dropdownRef.current?.isOpened) {
      const found = filtered.find(item => keySelector(item) === highlighted)
      if (found) return found
    }
    if (keyword === undefined && value) return value
    if (dropdownRef.current?.isOpened === false) return undefined
    if (keyword && normalize(keyword) === '') return undefined
    if (filtered.length > 0) return filtered[0]
    return undefined
  }, [value, keyword, filtered, highlighted, keySelector])

  // events
  const onChangeKeyword = useCallback((value: string | undefined) => {
    dropdownRef.current?.open()
    setKeyword(value)
    onKeywordChanged?.(value)
  }, [onKeywordChanged])

  const handleBlur: React.FocusEventHandler<HTMLInputElement> = useCallback(e => {
    const anyItem = getHighlightedOrAnyItem()
    onChange?.(anyItem)
    onBlur?.(e)
    setKeyword(undefined)
    setHighlightItem(anyItem ? keySelector(anyItem) : '')
  }, [getHighlightedOrAnyItem, onChange, onBlur, keySelector])

  const [{ isImeOpen }] = useIMEOpened()
  const onKeyDown: React.KeyboardEventHandler<HTMLInputElement> = useCallback(e => {
    if ((e.key === 'ArrowUp' || e.key === 'ArrowDown') && !isImeOpen) {
      if (!dropdownRef.current?.isOpened) {
        dropdownRef.current?.open()
        highlightAnyItem()
      } else if (e.key === 'ArrowUp') {
        highlightUpItem()
      } else {
        highlightDownItem()
      }
      e.preventDefault()
    }
    // ドロップダウン中のハイライトが当たっている要素の選択を確定する
    if (e.key === 'Enter' && !isImeOpen) {
      const anyItem = getHighlightedOrAnyItem()
      onChange?.(anyItem)
      setKeyword(undefined)
      setHighlightItem('')
      dropdownRef.current?.close()
      e.preventDefault()
    }
  }, [isImeOpen, getHighlightedOrAnyItem, highlightAnyItem, highlightUpItem, highlightDownItem, onChange])

  const onClickItem: React.MouseEventHandler<HTMLLIElement> = useCallback(e => {
    selectItemByValue((e.target as HTMLLIElement).getAttribute('value') as string)
    setHighlightItem('')
    dropdownRef.current?.close()
  }, [selectItemByValue])

  const textBaseRef = useRef<CustomComponentRef>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => getHighlightedOrAnyItem(),
    focus: () => textBaseRef.current?.focus(),
  }), [getHighlightedOrAnyItem])

  const displayText = useMemo(() => {
    if (keyword !== undefined) return keyword
    if (value !== undefined) return textSelector(value)
    return ''
  }, [keyword, value, textSelector])

  return (
    <TextInputBase
      ref={textBaseRef}
      readOnly={readOnly}
      name={name}
      value={displayText}
      onBlur={handleBlur}
      onChange={onChangeKeyword}
      onKeyDown={onKeyDown}
      dropdownRef={dropdownRef}
      dropdownBody={() => (
        <ul>
          {filtered.length === 0 && (
            <ListItem className="text-color-6">データなし</ListItem>
          )}
          {filtered.map(item => (
            <ListItem
              key={keySelector(item)}
              value={keySelector(item)}
              active={keySelector(item) === highlighted}
              onClick={onClickItem}
            >
              {textSelector(item)}
            </ListItem>
          ))}
        </ul>
      )}
    />
  )
})

const ListItem = (props: React.LiHTMLAttributes<HTMLLIElement> & {
  active?: boolean
}) => {
  const {
    active,
    children,
    className,
    ...rest
  } = props

  const lighlight = active ? 'bg-color-4' : ''

  return (
    <li {...rest} className={`cursor-pointer ${lighlight} ${className}`}>
      {children}
    </li>
  )
}

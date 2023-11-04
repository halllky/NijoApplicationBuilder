import React, { InputHTMLAttributes, useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState } from "react"
import { DropDownApi, TextInputBase } from "./TextInputBase"
import { CustomComponentRef, SelectionItem, defineCustomComponent, normalize, useIMEOpened } from "./util"

export const ComboBoxBase = defineCustomComponent<SelectionItem, {
  options: SelectionItem[]
  onKeywordChanged?: (keyword: string | undefined) => void
}, InputHTMLAttributes<HTMLInputElement>>((props, ref) => {

  const dropdownRef = useRef<DropDownApi>(null)

  // フィルタリング
  const [keyword, setKeyword] = useState<string | undefined>(undefined) // フォーカスを当ててから何か入力された場合のみundefinedでなくなる
  useEffect(() => {
  }, [props.options, keyword])
  const filtered = useMemo(() => {
    if (keyword === undefined) return [...props.options]
    const normalized = normalize(keyword)
    if (!normalized) return [...props.options]
    return props.options.filter(item => item.text.includes(normalized))
  }, [props.options, keyword])

  // リストのカーソル移動
  const [highlighted, setHighlightItem] = useState('')
  const highlightAnyItem = useCallback(() => {
    if (props.value) {
      setHighlightItem(props.value.value)
    } else if (filtered.length > 0) {
      setHighlightItem(filtered[0].value)
    } else {
      setHighlightItem('')
    }
  }, [props.value, filtered])
  const highlightUpItem = useCallback(() => {
    const currentIndex = filtered.findIndex(item => item.value === highlighted)
    if (currentIndex === -1) {
      if (filtered.length > 0) setHighlightItem(filtered[0].value)
    } else if (currentIndex > 0) {
      setHighlightItem(filtered[currentIndex - 1].value)
    }
  }, [filtered, highlighted])
  const highlightDownItem = useCallback(() => {
    const currentIndex = filtered.findIndex(item => item.value === highlighted)
    if (currentIndex === -1) {
      if (filtered.length > 0) setHighlightItem(filtered[0].value)
    } else if (currentIndex < (filtered.length - 1)) {
      setHighlightItem(filtered[currentIndex + 1].value)
    }
  }, [filtered, highlighted])

  // 選択
  const selectItemByValue = useCallback((value: string | undefined) => {
    const foundItem = props.options.find(item => item.value === value)
    setHighlightItem(foundItem?.value ?? '')
    setKeyword(undefined)
    props.onChange?.(foundItem)
  }, [props.options, props.onChange])

  // 入力中のテキストに近い最も適当な要素を取得する
  const getHighlightedOrAnyItem = useCallback(() => {
    if (highlighted) {
      const found = filtered.find(item => item.value === highlighted)
      if (found) return found
    }
    if (keyword === undefined && props.value) return props.value
    if (dropdownRef.current?.isOpened === false) return undefined
    if (keyword && normalize(keyword) === '') return undefined
    if (filtered.length > 0) return filtered[0]
    return undefined
  }, [props.value, keyword, filtered, highlighted])

  // events
  const onChangeKeyword = useCallback((value: string | undefined) => {
    dropdownRef.current?.open()
    setKeyword(value)
    props.onKeywordChanged?.(value)
  }, [props.onKeywordChanged])

  const onBlur: React.FocusEventHandler<HTMLInputElement> = useCallback(e => {
    const anyItem = getHighlightedOrAnyItem()
    props.onChange?.(anyItem)
    props.onBlur?.(e)
    setKeyword(undefined)
    setHighlightItem(anyItem?.value ?? '')
  }, [getHighlightedOrAnyItem, props.onChange, props.onBlur])

  const ime = useIMEOpened()
  const onKeyDown: React.KeyboardEventHandler<HTMLInputElement> = useCallback(e => {
    if ((e.key === 'ArrowUp' || e.key === 'ArrowDown') && !ime) {
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
    if (e.key === 'Enter' && !ime) {
      const anyItem = getHighlightedOrAnyItem()
      props.onChange?.(anyItem)
      setKeyword(undefined)
      setHighlightItem(anyItem?.value ?? '')
      dropdownRef.current?.close()
      e.preventDefault()
    }
  }, [ime, getHighlightedOrAnyItem, highlightAnyItem, highlightUpItem, highlightDownItem])

  const onClickItem: React.MouseEventHandler<HTMLLIElement> = useCallback(e => {
    selectItemByValue((e.target as HTMLLIElement).getAttribute('value') as string)
    dropdownRef.current?.close()
  }, [selectItemByValue])

  const textBaseRef = useRef<CustomComponentRef>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => getHighlightedOrAnyItem(),
    focus: () => textBaseRef.current?.focus(),
  }), [getHighlightedOrAnyItem])

  const displayText = useMemo(() => {
    if (keyword !== undefined) return keyword
    if (props.value !== undefined) return props.value.text
    return ''
  }, [keyword, props.value])

  return (
    <TextInputBase
      ref={textBaseRef}
      readOnly={props.readOnly}
      value={displayText}
      onBlur={onBlur}
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
              key={item.value}
              value={item.value}
              active={item.value === highlighted}
              onClick={onClickItem}
            >
              {item.text}
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

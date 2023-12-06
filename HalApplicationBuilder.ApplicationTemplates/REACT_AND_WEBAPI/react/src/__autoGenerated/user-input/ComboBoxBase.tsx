import React, { useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState } from "react"
import { DropDownApi, TextInputBase } from "./TextInputBase"
import { CustomComponentProps, CustomComponentRef, defineCustomComponent, normalize, useIMEOpened } from "./util"

export const ComboBoxBase = defineCustomComponent(<T extends {}>(
  props: CustomComponentProps<T, {
    options: T[]
    keySelector: (item: T) => string
    textSelector: (item: T) => string
    onKeywordChanged?: (keyword: string | undefined) => void
  }>,
  ref: React.ForwardedRef<CustomComponentRef<T>>
) => {

  const dropdownRef = useRef<DropDownApi>(null)

  // フィルタリング
  const [keyword, setKeyword] = useState<string | undefined>(undefined) // フォーカスを当ててから何か入力された場合のみundefinedでなくなる
  useEffect(() => {
  }, [props.options, keyword])
  const filtered = useMemo(() => {
    if (keyword === undefined) return [...props.options]
    const normalized = normalize(keyword)
    if (!normalized) return [...props.options]
    return props.options.filter(item => props.textSelector(item).includes(normalized))
  }, [props.options, keyword, props.textSelector])

  // リストのカーソル移動
  const [highlighted, setHighlightItem] = useState('')
  const highlightAnyItem = useCallback(() => {
    if (props.value) {
      setHighlightItem(props.keySelector(props.value))
    } else if (filtered.length > 0) {
      setHighlightItem(props.keySelector(filtered[0]))
    } else {
      setHighlightItem('')
    }
  }, [props.value, filtered, props.keySelector])
  const highlightUpItem = useCallback(() => {
    const currentIndex = filtered.findIndex(item => props.keySelector(item) === highlighted)
    if (currentIndex === -1) {
      if (filtered.length > 0) setHighlightItem(props.keySelector(filtered[0]))
    } else if (currentIndex > 0) {
      setHighlightItem(props.keySelector(filtered[currentIndex - 1]))
    }
  }, [filtered, highlighted, props.keySelector])
  const highlightDownItem = useCallback(() => {
    const currentIndex = filtered.findIndex(item => props.keySelector(item) === highlighted)
    if (currentIndex === -1) {
      if (filtered.length > 0) setHighlightItem(props.keySelector(filtered[0]))
    } else if (currentIndex < (filtered.length - 1)) {
      setHighlightItem(props.keySelector(filtered[currentIndex + 1]))
    }
  }, [filtered, highlighted, props.keySelector])

  // 選択
  const selectItemByValue = useCallback((value: string | undefined) => {
    const foundItem = props.options.find(item => props.keySelector(item) === value)
    setHighlightItem(foundItem ? props.keySelector(foundItem) : '')
    setKeyword(undefined)
    props.onChange?.(foundItem)
  }, [props.options, props.onChange, props.keySelector])

  // 入力中のテキストに近い最も適当な要素を取得する
  const getHighlightedOrAnyItem = useCallback(() => {
    if (highlighted && dropdownRef.current?.isOpened) {
      const found = filtered.find(item => props.keySelector(item) === highlighted)
      if (found) return found
    }
    if (keyword === undefined && props.value) return props.value
    if (dropdownRef.current?.isOpened === false) return undefined
    if (keyword && normalize(keyword) === '') return undefined
    if (filtered.length > 0) return filtered[0]
    return undefined
  }, [props.value, keyword, filtered, highlighted, props.keySelector])

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
    setHighlightItem(anyItem ? props.keySelector(anyItem) : '')
  }, [getHighlightedOrAnyItem, props.onChange, props.onBlur, props.keySelector])

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
      setHighlightItem('')
      dropdownRef.current?.close()
      e.preventDefault()
    }
  }, [ime, getHighlightedOrAnyItem, highlightAnyItem, highlightUpItem, highlightDownItem, props.keySelector])

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
    if (props.value !== undefined) return props.textSelector(props.value)
    return ''
  }, [keyword, props.value, props.textSelector])

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
              key={props.keySelector(item)}
              value={props.keySelector(item)}
              active={props.keySelector(item) === highlighted}
              onClick={onClickItem}
            >
              {props.textSelector(item)}
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

import React, { useState, useRef, useEffect, useLayoutEffect } from "react"
import { WordTextBox, WordTextBoxProps } from "./WordTextBox"

export type AsyncComboBoxProps<T> = Omit<WordTextBoxProps, 'value' | 'onChange'> & {
  value?: string
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void
  onSearch: (keyword: string) => Promise<T[]>
  onSelectItem: (item: T) => void
  renderItem: (item: T) => React.ReactNode
  itemKey: (item: T) => string | number
}

export function AsyncComboBox<T>(props: AsyncComboBoxProps<T>) {
  const { value, onChange, onSearch, onSelectItem, renderItem, itemKey, className, ...rest } = props

  const [items, setItems] = useState<T[]>([])
  const [isOpen, setIsOpen] = useState(false)
  const [loading, setLoading] = useState(false)
  const [searchTerm, setSearchTerm] = useState(value || '')
  const [selectedIndex, setSelectedIndex] = useState(-1)
  const [dropdownPosition, setDropdownPosition] = useState<'bottom' | 'top'>('bottom')
  const wrapperRef = useRef<HTMLDivElement>(null)
  const listRef = useRef<HTMLUListElement>(null)

  // valueの変更をsearchTermに反映
  useEffect(() => {
    setSearchTerm(value || '')
  }, [value])

  // 外側クリックで閉じる
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => {
      document.removeEventListener("mousedown", handleClickOutside)
    }
  }, [])

  // 検索実行（デバウンス）
  useEffect(() => {
    if (!searchTerm) {
      setItems([])
      setIsOpen(false)
      setLoading(false)
      return
    }

    setLoading(true)

    const timer = setTimeout(async () => {
      try {
        const result = await onSearch(searchTerm)
        setItems(result)
        if (document.activeElement === wrapperRef.current?.querySelector('input')) {
          setIsOpen(true)
        }
      } catch (error) {
        console.error(error)
        setItems([])
      } finally {
        setLoading(false)
      }
    }, 500)

    return () => clearTimeout(timer)
  }, [searchTerm, onSearch])

  // アイテムリストが更新されたら選択状態をリセット
  useEffect(() => {
    setSelectedIndex(-1)
  }, [items])

  // ドロップダウンの表示位置制御
  useLayoutEffect(() => {
    if (isOpen && wrapperRef.current) {
      const rect = wrapperRef.current.getBoundingClientRect()
      const windowHeight = window.innerHeight
      const spaceBelow = windowHeight - rect.bottom
      const spaceAbove = rect.top

      // 下側のスペースが250px未満かつ上側のスペースの方が広い場合は上に表示
      if (spaceBelow < 250 && spaceAbove > spaceBelow) {
        setDropdownPosition('top')
      } else {
        setDropdownPosition('bottom')
      }
    }
  }, [isOpen, items.length])

  // キーボード操作時のスクロール制御
  useEffect(() => {
    if (isOpen && listRef.current && selectedIndex >= 0) {
      const selectedElement = listRef.current.children[selectedIndex] as HTMLElement
      if (selectedElement) {
        selectedElement.scrollIntoView({ block: 'nearest' })
      }
    }
  }, [selectedIndex, isOpen])

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value)
    onChange?.(e)
    // 入力があったら開く準備
    setIsOpen(true)
  }

  const handleSelect = (item: T) => {
    onSelectItem(item)
    setIsOpen(false)
    // 選択したら入力欄もそのアイテムの表示名などにしたいが、
    // それは親コンポーネントが value を更新することで実現されるはず。
  }

  const handleFocus = () => {
    if (searchTerm && items.length > 0) {
      setIsOpen(true)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (!isOpen) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault()
        setIsOpen(true)
      }
      return
    }

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault()
        setSelectedIndex(prev => (prev < items.length - 1 ? prev + 1 : prev))
        break
      case 'ArrowUp':
        e.preventDefault()
        setSelectedIndex(prev => (prev > 0 ? prev - 1 : prev))
        break
      case 'Enter':
        if (selectedIndex >= 0 && selectedIndex < items.length) {
          e.preventDefault()
          handleSelect(items[selectedIndex])
        }
        break
      case 'Escape':
        e.preventDefault()
        setIsOpen(false)
        break
    }
  }

  return (
    <div className={`relative ${className ?? ''}`} ref={wrapperRef}>
      <WordTextBox
        value={searchTerm}
        onChange={handleInputChange}
        onFocus={handleFocus}
        onKeyDown={handleKeyDown}
        className="w-full"
        autoComplete="off"
        {...rest}
      />
      {isOpen && (items.length > 0 || loading) && (
        <ul
          ref={listRef}
          className={`absolute z-10 w-full bg-white border border-gray-300 max-h-60 overflow-auto shadow-lg list-none p-0 m-0 text-left ${dropdownPosition === 'top' ? 'bottom-full mb-1' : 'mt-1'
            }`}
        >
          {loading && items.length === 0 && (
            <li className="p-2 text-gray-500">検索中...</li>
          )}
          {items.map((item, index) => (
            <li
              key={itemKey(item)}
              className={`p-2 cursor-pointer border-b border-gray-100 last:border-none ${index === selectedIndex ? 'bg-blue-100' : 'hover:bg-blue-50'
                }`}
              onClick={() => handleSelect(item)}
              onMouseEnter={() => setSelectedIndex(index)}
            >
              {renderItem(item)}
            </li>
          ))}
        </ul>
      )}
      {isOpen && !loading && searchTerm && items.length === 0 && (
        <div className={`absolute z-10 w-full bg-white border border-gray-300 p-2 text-gray-500 shadow-lg ${dropdownPosition === 'top' ? 'bottom-full mb-1' : 'mt-1'
          }`}>
          該当なし
        </div>
      )}
    </div>
  )
}

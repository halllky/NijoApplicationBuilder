import React, { useEffect, useRef, useState } from "react"
import * as Icon from "@heroicons/react/24/solid"

export type MultiSelectProps<T> = {
  /** 選択された値の配列 */
  value: T[]
  /** 値が変更されたときのコールバック */
  onChange: (value: T[]) => void
  /** 選択肢の配列 */
  options: T[]
  /** 各選択肢の表示ラベルを取得する関数。省略時は String(item) が使用されます。 */
  getLabel?: (item: T) => string
  /** 各選択肢の一意なキーを取得する関数。省略時は getLabel の結果が使用されます。 */
  getKey?: (item: T) => string | number
  /** 何も選択されていないときに表示するテキスト */
  placeholder?: string
  /** コンテナのクラス名 */
  className?: string
  /** 無効化フラグ */
  disabled?: boolean
}

/**
 * 複数選択可能なドロップダウンリスト。
 * 汎用的な型 T を扱うことができます。
 */
export function MultiSelect<T>(props: MultiSelectProps<T>) {
  const {
    value,
    onChange,
    options,
    getLabel = (item: T) => String(item),
    getKey = (item: T) => getLabel(item),
    placeholder = "選択してください",
    className,
    disabled,
  } = props

  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  // クリック外で閉じる処理
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => {
      document.removeEventListener("mousedown", handleClickOutside)
    }
  }, [])

  const handleToggle = () => {
    if (!disabled) {
      setIsOpen(!isOpen)
    }
  }

  const handleSelect = (item: T) => {
    const key = getKey(item)
    const isSelected = value.some(v => getKey(v) === key)

    let newValue: T[]
    if (isSelected) {
      newValue = value.filter(v => getKey(v) !== key)
    } else {
      newValue = [...value, item]
    }
    onChange(newValue)
  }

  return (
    <div className={`flex flex-wrap gap-1 ${className ?? ""}`}>
      <div className="relative inline-block text-left border border-gray-600" ref={containerRef}>
        <div>
          <button
            type="button"
            className={`
            inline-flex w-full justify-between items-center gap-x-1.5 bg-white px-1 py-px text-sm font-semibold text-gray-900 hover:bg-gray-50
            ${disabled ? "opacity-50 cursor-not-allowed" : "cursor-pointer"}
          `}
            onClick={handleToggle}
            disabled={disabled}
          >
            <span className="truncate max-w-[200px] text-left font-normal">
              {placeholder}
            </span>
            <Icon.ChevronDownIcon className="-mr-1 h-5 w-5 text-gray-400" aria-hidden="true" />
          </button>
        </div>

        {isOpen && (
          <div className="absolute left-0 z-10 mt-2 w-56 origin-top-right bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none max-h-60 overflow-y-auto">
            <div className="py-px">
              {options.map((option) => {
                const key = getKey(option)
                const label = getLabel(option)

                return (
                  <div
                    key={key}
                    className="px-1 py-px text-sm text-gray-700 hover:bg-gray-100 truncate select-none cursor-pointer"
                    onClick={() => handleSelect(option)}
                  >
                    {label}
                  </div>
                )
              })}
              {options.length === 0 && (
                <div className="px-4 py-2 text-sm text-gray-500">
                  選択肢がありません
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* 選択中の値 */}
      {value.map(item => (
        <button
          key={getKey(item)}
          type="button"
          className="flex items-center py-px px-1 text-sm text-teal-700 bg-white border border-teal-700 rounded cursor-pointer"
          onClick={() => handleSelect(item)}
        >
          {getLabel(item)}
          <Icon.XMarkIcon
            className="inline-block ml-1 h-4 w-4"
          />
        </button>
      ))}
    </div>
  )
}

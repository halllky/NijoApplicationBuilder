import React from "react"
import { ChevronDownIcon } from "@heroicons/react/24/outline"

/** ドロップダウン内の個別操作1件 */
export type SplitButtonOption = {
  /** React の key として使う識別子 */
  key: string
  label: string
  onClick: () => void
  disabled?: boolean
}

/**
 * 主操作ボタンと、個別操作をまとめたドロップダウンを持つボタン。
 * 主操作（例: 「全部まとめて開始」）はボタン本体のクリックで実行し、
 * 個別操作（例: 「vite だけ開始」）はシェブロン部分をクリックして開くドロップダウンから選ぶ。
 */
export const SplitButton = ({ icon: Icon, children, onClick, options, disabled, loading, className }: {
  icon?: React.ElementType
  /** 主操作ボタンのラベル */
  children: React.ReactNode
  /** 主操作 */
  onClick: () => void
  /** ドロップダウンに並ぶ個別操作 */
  options: SplitButtonOption[]
  disabled?: boolean
  loading?: boolean
  className?: string
}) => {

  const [open, setOpen] = React.useState(false)
  const containerRef = React.useRef<HTMLDivElement>(null)

  // ドロップダウンの外側クリックで閉じる（documentへのクリック監視という外部システムとの同期）
  React.useEffect(() => {
    if (!open) return
    const handleClick = (e: MouseEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [open])

  const handleOptionClick = (option: SplitButtonOption) => {
    setOpen(false)
    option.onClick()
  }

  return (
    <div ref={containerRef} className={`relative inline-flex ${className ?? ''}`}>

      {/* 主操作ボタン */}
      <button
        type="button"
        onClick={onClick}
        disabled={disabled || loading}
        className="flex items-center gap-1 px-2 py-1 rounded-l bg-sky-600 text-white text-sm hover:bg-sky-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {Icon && <Icon className="w-4 h-4" />}
        {loading
          ? <span className="animate-spin h-3 w-3 border-2 rounded-full border-white border-t-transparent" />
          : <span className="whitespace-nowrap">{children}</span>}
      </button>

      {/* ドロップダウン開閉ボタン */}
      <button
        type="button"
        onClick={() => setOpen(v => !v)}
        disabled={disabled || loading}
        className="flex items-center px-1 rounded-r bg-sky-600 text-white border-l border-sky-700 hover:bg-sky-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <ChevronDownIcon className="w-3 h-3" />
      </button>

      {/* ドロップダウン本体 */}
      {open && (
        <div className="absolute top-full left-0 mt-1 min-w-full bg-white border border-gray-300 rounded shadow-lg z-20 overflow-hidden">
          {options.map(option => (
            <button
              key={option.key}
              type="button"
              onClick={() => handleOptionClick(option)}
              disabled={option.disabled}
              className="block w-full px-3 py-1.5 text-left text-sm text-gray-800 whitespace-nowrap hover:bg-sky-50 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {option.label}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

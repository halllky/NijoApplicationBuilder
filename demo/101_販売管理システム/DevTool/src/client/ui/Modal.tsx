import React from "react"
import { XMarkIcon } from "@heroicons/react/24/outline"

/**
 * シェード＋中央パネルの形をしたモーダルダイアログの外枠。
 * ヘッダー（タイトル・閉じるボタン）までを提供し、本体の中身は children に委ねる。
 * 閉じる操作（シェードクリック・閉じるボタン）は onClose を呼ぶだけで、
 * 実際に閉じるかどうかの判断はこのコンポーネントを呼ぶ側が行う。
 */
export const Modal = ({ open, onClose, title, panelClassName, children }: {
  open: boolean
  onClose: () => void
  title: string
  /** パネルの幅・高さなど、呼び出し側ごとに異なるサイズ指定を渡す */
  panelClassName?: string
  children: React.ReactNode
}) => {
  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">

      {/* シェード */}
      <div className="absolute inset-0 bg-black/25" onClick={onClose} />

      {/* パネル */}
      <div className={`relative bg-white rounded shadow-lg flex flex-col ${panelClassName ?? ''}`}>

        {/* ヘッダ */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-gray-200">
          <h2 className="text-base font-bold">{title}</h2>
          <button type="button" onClick={onClose} className="p-1 text-gray-500 hover:text-gray-800">
            <XMarkIcon className="w-5 h-5" />
          </button>
        </div>

        {children}
      </div>
    </div>
  )
}

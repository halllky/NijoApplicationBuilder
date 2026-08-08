import React from 'react'
import { createPortal } from 'react-dom'
import { XMarkIcon } from '@heroicons/react/24/outline'

type ModalProps = {
  isOpen: boolean
  onClose: () => void
  title?: React.ReactNode
  children: React.ReactNode
  className?: string
  widthClass?: string
  hideCloseButton?: boolean
}

/**
 * 汎用モーダルダイアログ
 */
export function Modal({
  isOpen,
  onClose,
  title,
  children,
  className,
  widthClass = "max-w-4xl",
  hideCloseButton = false
}: ModalProps) {
  if (!isOpen) return null

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-8">
      {/* ダイアログ本体 */}
      <div className={`flex flex-col rounded bg-white shadow-lg overflow-hidden max-h-full ${widthClass} ${className ?? ''}`}>

        {/* ヘッダ */}
        {(title || !hideCloseButton) && (
          <div className="flex items-center px-4 py-2 border-b border-gray-100 flex-none bg-white">
            <div className="flex-1 font-bold text-lg select-none overflow-hidden text-ellipsis whitespace-nowrap">
              {title}
            </div>
            {!hideCloseButton && (
              <button type="button" onClick={onClose} className="rounded p-1 hover:bg-gray-200 cursor-pointer flex-none ml-2">
                <XMarkIcon className="h-6 w-6" />
              </button>
            )}
          </div>
        )}

        {children}
      </div>
    </div>,
    document.body
  )
}

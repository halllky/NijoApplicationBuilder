import React, { useCallback, useLayoutEffect, useRef } from 'react'
import { IconButton } from '../input/IconButton'

export const ModalDialog = ({ title, open, onClose, children }: {
  title?: string
  open: boolean
  onClose: () => void
  children?: React.ReactNode
}) => {

  const dialogRef = useRef<HTMLDialogElement>(null)
  useLayoutEffect(() => {
    if (open) {
      dialogRef.current?.showModal()
      dialogRef.current?.focus()
    } else {
      dialogRef.current?.close()
    }
  }, [dialogRef, open])

  const handleDialogClick: React.MouseEventHandler = useCallback(e => {
    if (e.target === dialogRef.current) onClose() // ダイアログ自身がクリックされたときのみダイアログを閉じる
  }, [dialogRef, onClose])
  const handleTitleClick: React.MouseEventHandler = useCallback(e => {
    e.stopPropagation() // 閉じるボタン以外のタイトル部分クリックでダイアログが閉じられるのを防ぐ
  }, [])

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      onClick={handleDialogClick}
      className="absolute inset-12 w-auto h-auto border border-color-5 outline-none"
    >
      <div className="w-full h-full flex flex-col">

        <div className="flex items-center p-1 border-b border-color-4" onClick={handleTitleClick}>
          {title && (
            <span className="font-medium select-none">{title}</span>
          )}
          <div className="flex-1"></div>
          <IconButton onClick={onClose}>閉じる</IconButton>
        </div>

        <div className="flex-1 p-1 overflow-auto">
          {children}
        </div>

      </div>
    </dialog>
  )
}

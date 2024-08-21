import React, { useLayoutEffect, useRef } from 'react'
import useEvent from 'react-use-event-hook'
import { IconButton } from '../input/IconButton'
import { UUID } from 'uuidjs'
import { defineContext2 } from '../util/ReactUtil'

/** モーダルダイアログの枠 */
const ModalDialog = ({ title, open, onClose, children, className }: {
  title?: string
  open: boolean
  onClose: () => void
  children?: React.ReactNode
  className?: string
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

  const handleDialogClick: React.MouseEventHandler = useEvent(e => {
    if (e.target === dialogRef.current) onClose() // ダイアログ自身がクリックされたときのみダイアログを閉じる
  })
  const handleTitleClick: React.MouseEventHandler = useEvent(e => {
    e.stopPropagation() // 閉じるボタン以外のタイトル部分クリックでダイアログが閉じられるのを防ぐ
  })

  return (
    <dialog
      ref={dialogRef}
      onClick={handleDialogClick}
      onClose={onClose}
      onCancel={onClose}
      className={`absolute inset-12 w-auto h-auto border border-color-5 outline-none ${className ?? ''}`}
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

// -----------------------
// ダイアログの枠とダイアログを呼び出す各画面は React Context を使って接続する ここから

type DialogContextState = {
  stack: { id: string, contents: DialogContents }[]
}
type DialogContents = (props: {
  closeDialog: () => void
}) => React.ReactNode

const initialize = (): DialogContextState => ({
  stack: []
})
const { reducer, ContextProvider, useContext: useDialogContext } = defineContext2(
  initialize,
  state => ({
    pushDialog: (contents: DialogContents) => ({
      stack: [{ id: UUID.generate(), contents }, ...state.stack],
    }),
    removeDialog: (id: string) => {
      return ({
        stack: state.stack.filter(d => d.id !== id),
      })
    },
  }),
)

const DialogContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const reducerReturns = React.useReducer(reducer, undefined, initialize)
  const [{ stack }, dispatch] = reducerReturns
  const handleCancel = useEvent((id: string) => {
    return () => dispatch(state => state.removeDialog(id))
  })

  return (
    <ContextProvider value={reducerReturns}>
      {children}
      {stack.map(({ id, contents }) => (
        <ModalDialog key={id} open onClose={handleCancel(id)}>
          {React.createElement(contents, {
            closeDialog: handleCancel(id),
          })}
        </ModalDialog>
      ))}
    </ContextProvider>
  )
}

// ダイアログの枠とダイアログを呼び出す各画面は React Context を使って接続する ここまで
// -----------------------

export {
  useDialogContext,
  DialogContextProvider,
}

import React, { useLayoutEffect, useRef } from 'react'
import useEvent from 'react-use-event-hook'
import { IconButton } from '../input/IconButton'
import { UUID } from 'uuidjs'
import { defineContext2, useOutsideClick } from '../util/ReactUtil'
import { MsgContextProvider, InlineMessageList } from '../util/Notification'

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
    <MsgContextProvider>
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
          <InlineMessageList />

          <div className="flex-1 p-1 overflow-auto">
            {children}
          </div>

        </div>
      </dialog>

    </MsgContextProvider>
  )
}

/** ポップアップの枠 */
const PopupFrame = ({ target, onClose, children }: {
  /** この要素の脇にポップアップが表示されます。 */
  target: HTMLElement | null | undefined
  onClose: () => void
  children?: React.ReactNode
}) => {
  const divRef = useRef<HTMLDivElement>(null)

  // targetの脇に表示する
  React.useEffect(() => {
    if (divRef.current && target) {
      const rect = target.getBoundingClientRect();
      const popupWidth = divRef.current.offsetWidth;
      const popupHeight = divRef.current.offsetHeight;

      // 右下に表示する場合の位置
      const top = rect.bottom + 1 // ターゲットの下に1pxの隙間を開ける
      const left = rect.right - popupWidth // ターゲットの右に表示

      // 画面内に収まるか確認
      if (top + popupHeight > window.innerHeight) {
        // 画面内に収まらない場合、右上に表示
        divRef.current.style.top = `${rect.top - popupHeight - 1}px`
        divRef.current.style.left = `${left}px`
      } else {
        // 画面内に収まる場合、右下に表示
        divRef.current.style.top = `${top}px`
        divRef.current.style.left = `${left}px`
      }
    }
  }, [divRef, target]);

  return (
    <div ref={divRef} className="fixed bg-color-ridge border border-color-5">
      {children}
    </div>
  )
}

// -----------------------
// ダイアログやポップアップの枠とそれらを呼び出す各画面は React Context を使って接続する ここから

type DialogContextState = {
  stack: { id: string, title: string, contents: DialogOrPopupContents }[]
  popup: { target: HTMLElement | null | undefined, contents: DialogOrPopupContents } | undefined
}
type DialogOrPopupContents = (props: {
  closeDialog: () => void
}) => React.ReactNode

const initialize = (): DialogContextState => ({
  stack: [],
  popup: undefined,
})
const { reducer, ContextProvider, useContext: useDialogContext } = defineContext2(
  initialize,
  state => ({
    /** 新しいダイアログを開きます。一度に複数のダイアログが開かれる可能性を考慮し、新しいダイアログはスタックに積まれます。 */
    pushDialog: (title: string, contents: DialogOrPopupContents) => ({
      stack: [{ id: UUID.generate(), title, contents }, ...state.stack],
      popup: state.popup,
    }),
    /** ダイアログを閉じます（スタックからダイアログを除去します）。 */
    removeDialog: (id: string) => {
      return ({
        stack: state.stack.filter(d => d.id !== id),
        popup: state.popup,
      })
    },
    /** ポップアップを開きます。既存のポップアップは閉じられます。 */
    openPopup: (target: HTMLElement | null | undefined, contents: DialogOrPopupContents) => ({
      stack: state.stack,
      popup: { target, contents },
    }),
    /** 現在開かれているポップアップを閉じます。ポップアップが開かれていない場合は何も起きません。 */
    closePopup: () => ({
      stack: state.stack,
      popup: undefined,
    }),
  }),
)

/** コンテキスト */
const DialogContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const reducerReturns = React.useReducer(reducer, undefined, initialize)
  const [{ stack, popup }, dispatch] = reducerReturns
  const handleCancel = useEvent((id: string) => {
    return () => dispatch(state => state.removeDialog(id))
  })
  const handleClosePopup = useEvent(() => {
    dispatch(state => state.closePopup())
  })

  return (
    <ContextProvider value={reducerReturns}>
      {children}

      {/* ダイアログ。ダイアログが一度に複数開かれている場合は後にスタックに積まれた方が手前に表示される。 */}
      {stack.map(({ id, title, contents }) => (
        <ModalDialog key={id} open title={title} onClose={handleCancel(id)}>
          {React.createElement(contents, {
            closeDialog: handleCancel(id),
          })}
        </ModalDialog>
      ))}

      {/* ポップアップ。ポップアップは一度に複数表示できないので常に最大1個。 */}
      {popup && (
        <PopupFrame target={popup.target} onClose={handleClosePopup}>
          {React.createElement(popup.contents)}
        </PopupFrame>
      )}

    </ContextProvider>
  )
}

// ダイアログやポップアップの枠とそれらを呼び出す各画面は React Context を使って接続する ここまで
// -----------------------

export {
  useDialogContext,
  DialogContextProvider,
}

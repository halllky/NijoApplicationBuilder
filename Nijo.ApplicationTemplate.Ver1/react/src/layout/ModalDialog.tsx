import React, { useLayoutEffect, useRef } from 'react'
import useEvent from 'react-use-event-hook'
import { IconButton } from '../input/IconButton'
import { UUID } from 'uuidjs'
import { useOutsideClick } from '../util/useOutsideClick'
import { InlineMessageContextProvider, InlineMessage } from '../util/useMsgContext'

/** モーダルダイアログの枠 */
const ModalDialog = ({ title, onClose, disableConfirm, children }: {
  title?: string
  onClose: () => void
  disableConfirm: boolean
  children?: React.ReactNode
}) => {
  // 閉じる
  const handleClose = useEvent(() => {
    if (!disableConfirm && !confirm('入力内容が破棄されます。よろしいでしょうか？')) {
      return
    }
    onClose()
  })

  const dialogRef = useRef<HTMLDivElement>(null)
  useLayoutEffect(() => {
    dialogRef.current?.focus()
  }, [dialogRef])

  // 画面離脱（ブラウザ閉じるorタブ閉じる）アラート設定
  React.useEffect(() => {
    const handleBeforeUnload: OnBeforeUnloadEventHandler = e => {
      e.preventDefault()
      return null
    }
    window.addEventListener('beforeunload', handleBeforeUnload, false)
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload, false)
    }
  }, [dialogRef])

  // Escapeキーでダイアログを閉じる
  const handleKeyDown: React.KeyboardEventHandler = useEvent(e => {
    if (e.target === dialogRef.current && e.key === 'Escape') handleClose()
  })

  // ダイアログの外にフォーカスが出るのを防ぐ
  const preventFocusOut = useEvent(() => {
    dialogRef.current?.focus()
  })

  return (
    <InlineMessageContextProvider>
      {/* 画面全体 */}
      <div
        className="fixed z-0 inset-0 w-screen h-screen"
      >
        {/* シェード */}
        <div
          onMouseDown={handleClose} // 背景のシェードが押されたらダイアログを閉じる
          onFocus={preventFocusOut} // Shift + Tab でダイアログの外にフォーカスを当てることができてしまうのを防ぐ
          tabIndex={0} // onFocusが発火されるために必要
          className="absolute inset-0 bg-black opacity-25"
        ></div>

        {/* ダイアログ本体 */}
        <div
          ref={dialogRef}
          onKeyDown={handleKeyDown}
          tabIndex={0} // Escapeキーが押されたときにダイアログ自身がフォーカスされているかどうかの判定に必要
          className="absolute flex flex-col inset-16 bg-color-0 border border-color-5 outline-none"
        >
          {/* ヘッダ */}
          <div className="flex items-center p-1 border-b border-color-4">
            {title && (
              <span className="font-medium select-none">{title}</span>
            )}
            <div className="flex-1"></div>
            <IconButton onClick={handleClose}>閉じる</IconButton>
          </div>

          {/* ダイアログ内部で発生したメッセージの表示箇所 */}
          <InlineMessage />

          {/* ボディ */}
          <div className="flex-1 p-1 overflow-auto">
            {children}
          </div>
        </div>

        {/* Tabキーでダイアログの外にフォーカスを当てることができてしまうのを防ぐ */}
        <div onFocus={preventFocusOut} tabIndex={0}></div>

      </div>
    </InlineMessageContextProvider>
  )
}

/** ポップアップの枠 */
const PopupFrame = ({ target, onClose, elementRef, children }: {
  /** この要素の脇にポップアップが表示されます。 */
  target: HTMLElement | null | undefined
  /** ポップアップが閉じられるときのイベント */
  onClose: () => void
  /** html要素への参照 */
  elementRef: React.RefObject<HTMLElement | null>
  children?: React.ReactNode
}) => {
  const divRef = useRef<HTMLDivElement | null>(null)
  const divRefCallback = React.useCallback((div: HTMLDivElement | null) => {
    elementRef.current = divRef.current = div
  }, [divRef, elementRef])

  // targetの脇に表示する
  React.useLayoutEffect(() => {
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

      // ポップアップの横幅
      divRef.current.style.minWidth = `${rect.width}px`
    }
  }, [divRef, target])

  // 外側クリックで閉じる
  useOutsideClick(divRef, () => {
    onClose?.()
  }, [onClose])

  return (
    // ポップアップクリック時、コンボボックスのblurでフォーカス離脱先がポップアップか否かで分岐する必要がある。
    // tabIndexを設定しないとFocusEventのrelatedTargetにならない
    <div ref={divRefCallback} tabIndex={0} className="z-10 fixed max-h-64 overflow-y-auto bg-color-ridge border border-color-5 outline-none">
      {children}
    </div>
  )
}

// -----------------------
// ダイアログやポップアップの枠とそれらを呼び出す各画面は React Context を使って接続する ここから

/** 開かれているダイアログ。一度に複数開くことができ、そして後から開かれたダイアログが前面に表示されるので、スタックの形をとっている。 */
type DialogStack = {
  id: string
  option: { title: string, disableConfirm?: boolean }
  contents: DialogOrPopupContents
}

/** 開かれているポップアップ。一度に1つしか開けない。 */
type PopupState = {
  target: HTMLElement | null | undefined
  contents: DialogOrPopupContents
  onClose: (() => void) | undefined
}

/** ダイアログまたはポップアップの内容 */
export type DialogOrPopupContents = (props: {
  /** ダイアログまたはポップアップを閉じる */
  closeDialog: () => void
}) => React.ReactNode

/** useDialogContext の返り値の型 */
export type UseDialogContextReturn = {
  /** 新しいダイアログを開きます。一度に複数のダイアログが開かれる可能性を考慮し、新しいダイアログはスタックに積まれます。 */
  pushDialog: (option: { title: string, disableConfirm?: boolean }, contents: DialogOrPopupContents) => void
  /** ダイアログを閉じます（スタックからダイアログを除去します）。 */
  removeDialog: (id: string) => void
  /** ポップアップを開きます。既存のポップアップは閉じられます。 */
  openPopup: (target: HTMLElement | null | undefined, contents: DialogOrPopupContents, onClose?: () => void) => void
  /** 現在開かれているポップアップを閉じます。ポップアップが開かれていない場合は何も起きません。 */
  closePopup: () => void
}

const DialogContext = React.createContext<UseDialogContextReturn>({
  pushDialog: () => { },
  removeDialog: () => { },
  openPopup: () => { },
  closePopup: () => { },
})

export const useDialogContext = () => React.useContext(DialogContext)

/** コンテキスト */
export const DialogContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {

  const [dialogStack, setDialogStack] = React.useState<DialogStack[]>([])
  const [popup, setPopup] = React.useState<PopupState | undefined>(undefined)
  const popupElementRef = React.useRef<HTMLElement | null>(null)

  const pushDialog = React.useCallback((option: { title: string, disableConfirm?: boolean }, contents: DialogOrPopupContents) => {
    setDialogStack(prev => [...prev, { id: UUID.generate(), option, contents }])
  }, [setDialogStack])

  const removeDialog = React.useCallback((id: string) => {
    setDialogStack(prev => prev.filter(d => d.id !== id))
  }, [setDialogStack])

  const openPopup = React.useCallback((target: HTMLElement | null | undefined, contents: DialogOrPopupContents, onClose?: () => void) => {
    setPopup({ target, contents, onClose })
  }, [setPopup])

  const closePopup = React.useCallback(() => {
    setPopup(undefined)
    popup?.onClose?.()
  }, [setPopup, popup])

  const contextValue: UseDialogContextReturn = React.useMemo(() => ({
    pushDialog,
    removeDialog,
    openPopup,
    closePopup,
  }), [pushDialog, removeDialog, openPopup, closePopup])

  return (
    <DialogContext.Provider value={contextValue}>
      {children}

      {/* ダイアログ。ダイアログが一度に複数開かれている場合は後にスタックに積まれた方が手前に表示される。 */}
      {dialogStack.map(({ id, option, contents }) => (
        <ModalDialog key={id} title={option.title} onClose={() => removeDialog(id)} disableConfirm={option.disableConfirm ?? false}>
          {React.createElement(contents, {
            closeDialog: () => removeDialog(id),
          })}
        </ModalDialog>
      ))}

      {/* ポップアップ。ポップアップは一度に複数表示できないので常に最大1個。 */}
      {popup && (
        <PopupFrame target={popup.target} onClose={closePopup} elementRef={popupElementRef}>
          {React.createElement(popup.contents, {
            closeDialog: closePopup,
          })}
        </PopupFrame>
      )}

    </DialogContext.Provider>
  )
}

// ダイアログやポップアップの枠とそれらを呼び出す各画面は React Context を使って接続する ここまで

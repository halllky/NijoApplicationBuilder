import React from "react"
import { UUID } from "uuidjs"

/**
 * トーストメッセージを表示するコンテキストを返す。
 * トーストは画面右上に表示され、数秒後に自動的に消える。
 * トーストメッセージはマウスイベントを受け付けない。
 */
export const useToastContext = () => {
  return React.useContext(ToastContext)
}

/**
 * トーストメッセージを表示するコンテキストを提供する。
 * トーストメッセージは画面右上に表示され、数秒後に自動的に消える。
 * トーストメッセージはマウスイベントを受け付けない。
 */
export const ToastContextProvider = (props: { children: React.ReactNode }) => {
  const [state, dispatch] = React.useReducer(toastContextReducer, { toastMessages: undefined, error: () => { }, warn: () => { }, info: () => { }, remove: () => { } })

  // コンテキストの値の変更と、setTimeoutによる一定時間後の自動削除を設定する。
  // メッセージは画面右上の画面外からスライドインして表示される。
  // 一定時間後に画面外にスライドアウトし、完全に画面外に出た後一定時間後にコンテキストからも削除される。
  const showToastMessage = React.useCallback((message: ToastMessage) => {
    dispatch({ type: 'addToastMessage', toastMessage: message })
    setTimeout(() => {
      dispatch({ type: 'removeToastMessage', id: message.id })
    }, 3000)
  }, [dispatch])

  /** コンテキストの値 */
  const value = React.useMemo(() => ({
    toastMessages: state.toastMessages,
    error: (message: string) => showToastMessage({ id: UUID.generate(), message, type: 'error' }),
    warn: (message: string) => showToastMessage({ id: UUID.generate(), message, type: 'warn' }),
    info: (message: string) => showToastMessage({ id: UUID.generate(), message, type: 'info' }),
    remove: (id: string) => dispatch({ type: 'removeToastMessage', id }),
  }), [state, showToastMessage])

  return (
    <ToastContext.Provider value={value}>
      {props.children}

      {state.toastMessages?.map(message => (
        <div key={message.id} className={`toast ${getToastMessageClassName(message.type)}`}>
          {message.message}
        </div>
      ))}
    </ToastContext.Provider>
  )
}

// -----------------------------------------------
const getToastMessageClassName = (type: ToastMessage['type']) => {
  switch (type) {
    case 'info':
      return 'text-blue-500 bg-blue-50'
    case 'warn':
      return 'text-amber-500 bg-amber-50'
    case 'error':
      return 'text-rose-500 bg-rose-50'
  }
}

// -----------------------------------------------

export const ToastContext = React.createContext<ToastContextState>({
  toastMessages: undefined,
  error: () => { },
  warn: () => { },
  info: () => { },
  remove: () => { },
})

const toastContextReducer = (state: ToastContextState, action: ToastContextAction): ToastContextState => {
  switch (action.type) {
    case 'addToastMessage':
      return { ...state, toastMessages: [...(state.toastMessages ?? []), action.toastMessage] }
    case 'removeToastMessage':
      return { ...state, toastMessages: state.toastMessages?.filter(message => message.id !== action.id) }
  }
}


type ToastContextState = {
  toastMessages: ToastMessage[] | undefined
  error: (message: string) => void
  warn: (message: string) => void
  info: (message: string) => void
  remove: (id: string) => void
}

type ToastMessage = {
  id: string
  message: string
  type: 'info' | 'warn' | 'error'
}

type ToastContextAction = {
  type: 'addToastMessage'
  toastMessage: ToastMessage
} | {
  type: 'removeToastMessage'
  id: string
}

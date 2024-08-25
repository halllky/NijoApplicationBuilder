import React, { useCallback, useEffect, useMemo, useState } from "react"
import * as Icon from "@heroicons/react/24/outline"
import { UUID } from "uuidjs"
import * as ReactHookUtil from "./ReactUtil"
import { useUserSetting } from "./UserSetting"
import * as Components from "../input"

type Msg = {
  id: string
  name?: string
  message: string
  type: 'error' | 'warn' | 'info'
}
type State = {
  messages: Msg[]
}
const notificationReducer = (state: State) => {
  const push = (type: Msg['type'], ...messages: unknown[]) => {
    if (type === 'error') console.error(...messages)

    const flatten = messages.flatMap(m => Array.isArray(m) ? m : [m])
    const addedMessages = flatten.map<Msg>(m => {
      const id = UUID.generate()
      if (typeof m === 'string') return { id, type, message: m }
      const asErrMsg = m as Omit<Msg, 'id'>
      if (typeof asErrMsg.message === 'string') return { id, type, message: asErrMsg.message, name: asErrMsg.name }
      return { id, type, message: m?.toString() ?? '' }
    })
    return { messages: [...state.messages, ...addedMessages] }
  }
  const clear = (nameOrItem?: string | Msg) => {
    if (!nameOrItem) {
      return { messages: [] }
    }
    let filterFn: (msg: Msg) => boolean
    if (typeof nameOrItem === 'string') {
      const name = nameOrItem
      filterFn = m => !m.name?.startsWith(name)
    } else {
      const id = nameOrItem.id
      filterFn = m => m.id !== id
    }
    return { messages: state.messages.filter(filterFn) }
  }

  return {
    clear,
    error: (...messages: unknown[]) => push('error', ...messages),
    warn: (...messages: unknown[]) => push('warn', ...messages),
    info: (...messages: unknown[]) => push('info', ...messages),
  }
}

export const [MsgContextProvider, useMsgContext] = ReactHookUtil.defineContext((): State => ({ messages: [] }), notificationReducer)
export const [ToastContextProvider, useToastContext] = ReactHookUtil.defineContext((): State => ({ messages: [] }), notificationReducer)

export const InlineMessageList = ({ type, name, className }: {
  type?: Msg['type']
  name?: string
  className?: string
}) => {
  const { data: { darkMode } } = useUserSetting()
  const getBorderColor = useCallback((msg?: Msg) => {
    if (msg?.type === 'warn') {
      return darkMode ? 'border-amber-900' : 'border-amber-200'
    } else if (msg?.type === 'info') {
      return darkMode ? 'border-sky-900' : 'border-sky-200'
    } else {
      return darkMode ? 'border-rose-900' : 'border-rose-200'
    }
  }, [darkMode])
  const getBgColor = useCallback((msg?: Msg) => {
    if (msg?.type === 'warn') {
      return darkMode ? 'bg-amber-800' : 'bg-amber-100'
    } else if (msg?.type === 'info') {
      return darkMode ? 'bg-sky-800' : 'bg-sky-100'
    } else {
      return darkMode ? 'bg-rose-800' : 'bg-rose-100'
    }
  }, [darkMode])
  const getTextColor = useCallback((msg?: Msg) => {
    if (msg?.type === 'warn') {
      return darkMode ? 'text-amber-200' : 'text-amber-700'
    } else if (msg?.type === 'info') {
      return darkMode ? 'text-sky-200' : 'text-sky-700'
    } else {
      return darkMode ? 'text-rose-200' : 'text-rose-600'
    }
  }, [darkMode])

  const [{ messages }, dispatch] = useMsgContext()
  const filtered = useMemo(() => {
    let arr = [...messages]
    if (type) arr = arr.filter(m => m.type === type)
    if (name) arr = arr.filter(m => m.name?.startsWith(name))
    return arr
  }, [messages, name, type])

  if (filtered.length === 0) return <></>

  return (
    <div className={`flex flex-col gap-1 ${className}`}>
      <ul className="flex-1 flex flex-col overflow-y-scroll max-h-36">
        {filtered.map(msg => (
          <li key={msg.id} className={`flex gap-1 items-center border border-1 ${getBorderColor(msg)} ${getBgColor(msg)}`}>
            <span title={msg.message} className={`flex-1 ${getTextColor(msg)} overflow-hidden text-nowrap overflow-ellipsis whitespace-pre select-all`}>
              {msg.message}
            </span>
            <Components.IconButton
              onClick={() => dispatch(state => state.clear(msg))}
              icon={Icon.XMarkIcon}
            />
          </li>
        ))}
      </ul>
      {filtered.length > 0 && (
        <div className={`flex gap-8 text-sm select-none items-center ${getTextColor()}`}>
          {filtered.length}件のメッセージ
          <Components.IconButton onClick={() => dispatch(msg => msg.clear(name))}>
            すべてクリアする
          </Components.IconButton>
          <div className="flex-1"></div>
        </div>
      )}
    </div>
  )
}

export const Toast = ({ type, name }: {
  type?: Msg['type']
  name?: string
}) => {
  const [{ messages },] = useToastContext()
  const { data: { darkMode } } = useUserSetting()
  const filtered = useMemo(() => {
    let arr = [...messages]
    if (type) arr = arr.filter(m => m.type === type)
    if (name) arr = arr.filter(m => m.name?.startsWith(name))
    return arr
  }, [messages, name, type])

  return <>
    {filtered.map(msg => (
      <ToastMessage key={msg.id} msg={msg} darkMode={darkMode} />
    ))}
  </>
}
const ToastMessage = ({ msg, darkMode }: {
  msg: Msg
  darkMode: boolean | undefined
}) => {
  const [, dispatch] = useToastContext()
  const [visible, setVisible] = useState(true)
  useEffect(() => {
    // infoのトーストは勝手に消えてほしいのでタイマーをかけて消す
    if (msg.type === 'info') {
      const timer1 = setTimeout(() => {
        setVisible(false)
      }, 3000)
      const timer2 = setTimeout(() => {
        dispatch(state => state.clear(msg))
      }, 5000)
      return () => {
        clearTimeout(timer1)
        clearTimeout(timer2)
      }
    }
  }, [dispatch, msg])

  const colors = useMemo(() => {
    if (msg.type === 'warn') {
      return darkMode
        ? 'border-amber-500 bg-amber-800 text-amber-50'
        : 'border-amber-500 bg-amber-200 text-amber-900'
    } else if (msg.type === 'info') {
      return darkMode
        ? 'border-sky-500 bg-sky-800 text-sky-50'
        : 'border-sky-500 bg-sky-200 text-sky-900'
    } else {
      return darkMode
        ? 'border-rose-500 bg-rose-800 text-rose-50'
        : 'border-rose-500 bg-rose-200 text-rose-900'
    }
  }, [msg, darkMode])

  const animate = visible
    ? 'animate-slideIn'
    : 'animate-slideOut translate-x-[calc(-100%-1rem)]'

  return (
    <div className={`z-[300] flex flex-col fixed left-4 bottom-12 w-64 h-24 overflow-hidden border border-1 ${colors} ${animate}`}>
      <Components.IconButton
        inline
        icon={Icon.XMarkIcon}
        onClick={() => dispatch(state => state.clear(msg))}
        className="self-end"
      />
      <span className="inline-block w-full h-full px-2 overflow-y-auto select-all break-words">
        {msg.message}
      </span>
    </div>
  )
}

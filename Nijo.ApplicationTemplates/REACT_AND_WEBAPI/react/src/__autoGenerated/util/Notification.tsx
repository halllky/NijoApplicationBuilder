import { useEffect, useMemo, useState } from "react"
import * as Icon from "@heroicons/react/24/outline"
import { UUID } from "uuidjs"
import * as ReactHookUtil from "./ReactUtil"
import * as Components from "../components"

type Msg = {
  id: string
  name?: string
  message: string
  type: 'error' | 'warn' | 'info'
}
type State = {
  inline: Msg[]
  toast: Msg[]
}

export const [
  MsgContextProvider,
  useMsgContext,
] = ReactHookUtil.defineContext((): State => ({
  inline: [] as Msg[],
  toast: [] as Msg[],
}), state => {
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
    if (type === 'info') {
      return { ...state, toast: [...state.toast, ...addedMessages] }
    } else {
      return { ...state, inline: [...state.inline, ...addedMessages] }
    }
  }
  const clear = (nameOrItem?: string | Msg) => {
    if (!nameOrItem) {
      return { ...state, inline: [], toast: [] }
    }
    let filterFn: (msg: Msg) => boolean
    if (typeof nameOrItem === 'string') {
      const name = nameOrItem
      filterFn = m => !m.name?.startsWith(name)
    } else {
      const id = nameOrItem.id
      filterFn = m => m.id !== id
    }
    return {
      ...state,
      inline: state.inline.filter(filterFn),
      toast: state.toast.filter(filterFn),
    }
  }

  return {
    clear,
    error: (...messages: unknown[]) => push('error', ...messages),
    warn: (...messages: unknown[]) => push('warn', ...messages),
    info: (...messages: unknown[]) => push('info', ...messages),
  }
})

export const InlineMessageList = ({ type, name, className }: {
  type?: Msg['type']
  name?: string
  className?: string
}) => {
  const [{ inline }, dispatch] = useMsgContext()
  const filtered = useMemo(() => {
    let arr = [...inline]
    if (type) arr = arr.filter(m => m.type === type)
    if (name) arr = arr.filter(m => m.name?.startsWith(name))
    return arr
  }, [inline, name])

  return (
    <div className={`flex flex-col ${className}`}>
      <ul className="flex-1 flex flex-col overflow-auto max-h-32">
        {filtered.map(msg => (
          <li key={msg.id} className={`
            flex gap-1 items-center
            border border-1
            ${msg.type === 'warn' ? 'border-amber-200' : 'border-rose-200'}
            ${msg.type === 'warn' ? 'bg-amber-100' : 'bg-rose-100'}`}>
            <span title={msg.message} className={`
              flex-1
              ${msg.type === 'warn' ? 'text-amber-700' : 'text-rose-600'}
              overflow-hidden text-nowrap overflow-ellipsis
              select-all`}>
              {msg.message}
            </span>
            <Components.IconButton
              onClick={() => dispatch(state => state.clear(msg))}
              icon={Icon.XMarkIcon}
            />
          </li>
        ))}
      </ul>
      {filtered.length > 5 && (
        <div className="flex text-sm select-none items-center">
          {filtered.length}件の警告とエラー
          <div className="flex-1"></div>
          <Components.IconButton onClick={() => dispatch(msg => msg.clear(name))}>
            すべてクリア
          </Components.IconButton>
        </div>
      )}
    </div>
  )
}

export const Toast = ({ type, name, className }: {
  type?: Msg['type']
  name?: string
  className?: string
}) => {
  const [{ toast },] = useMsgContext()
  const filtered = useMemo(() => {
    let arr = [...toast]
    if (type) arr = arr.filter(m => m.type === type)
    if (name) arr = arr.filter(m => m.name?.startsWith(name))
    return arr
  }, [toast, name])

  return <>
    {filtered.map(msg => (
      <ToastMessage key={msg.id} msg={msg} className={className} />
    ))}
  </>
}
const ToastMessage = ({ msg, className }: {
  msg: Msg
  className?: string
}) => {
  const [, dispatch] = useMsgContext()
  const [visible, setVisible] = useState(true)
  useEffect(() => {
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
  }, [])

  return (
    <div
      onClick={() => dispatch(state => state.clear(msg))}
      className={`
        z-[300] flex select-none cursor-pointer overflow-hidden
        ${(visible ? 'animate-slideIn' : 'animate-slideOut translate-x-[calc(-100%-1rem)]')}
        fixed left-4 bottom-4 p-2 w-64 h-24
        bg-sky-950 text-sky-50 border border-1 boder-sky-500
        ${className}`}>
      <span className="flex-1">
        {msg.message}
      </span>
      <Components.IconButton
        inline
        icon={Icon.XMarkIcon}
        className="self-start"
      />
    </div>
  )
}

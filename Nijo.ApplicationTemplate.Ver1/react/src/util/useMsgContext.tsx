import React from "react"
import { UUID } from "uuidjs"

/**
 * どこにも表示する箇所が無いメッセージの表示を行う
 */
export const useMsgContext = () => {
  return React.useContext(InlineMessageContext)
}

/**
 * インラインメッセージのコンテキストのプロバイダー。
 * 画面ルートなど、どこにも表示する箇所が無いメッセージの表示より外側に配置する必要がある。
 */
export const InlineMessageContextProvider = (props: { children: React.ReactNode }) => {
  const [state, dispatch] = React.useReducer(inlineMessageContextReducer, {
    displayMessages: [],
    error: () => { },
    warn: () => { },
    info: () => { },
    clear: () => { },
  })

  const value = React.useMemo(() => ({
    displayMessages: state.displayMessages,
    error: (msg: string) => dispatch({ type: 'add', msg: { msg, type: 'error' } }),
    warn: (msg: string) => dispatch({ type: 'add', msg: { msg, type: 'warn' } }),
    info: (msg: string) => dispatch({ type: 'add', msg: { msg, type: 'info' } }),
    clear: (deleteIdList?: string[]) => dispatch({ type: 'clear', deleteIdList }),
  }), [state, dispatch])

  return (
    <InlineMessageContext.Provider value={value}>
      {props.children}
    </InlineMessageContext.Provider>
  )
}

/**
 * インラインメッセージ。
 * エラーは赤、警告は黄色、情報は青色で表示する。
 *
 * メッセージ1件ごとに削除ボタンがついており、ユーザーが削除ボタンをクリックするとそのメッセージが消える。
 * 2件以上のメッセージがある場合、末尾に「n件のメッセージ」と総数が表示される。その左に「すべてクリアする」ボタンがあり、これをクリックすると全てのメッセージが消える。
 *
 * 同じ種類・同じ文言のメッセージが連続して表示された場合、表示上は1件にまとめられたうえで、 "②" など何件同じメッセージが現れているかが表示される。
 */
export const InlineMessage = (props: {
  className?: string
}) => {
  const state = useMsgContext()

  // 同じ種類・同じ文言のメッセージが連続して表示された場合、表示上は1件にまとめられたうえで、 "②" など何件同じメッセージが現れているかが表示される。
  const groupedMessages = React.useMemo(() => {
    return state.displayMessages.reduce((acc, msg) => {
      const previous = acc[acc.length - 1]
      if (previous && previous.type === msg.type && previous.msg === msg.msg) {
        previous.idList.push(msg.id)
      } else {
        acc.push({ type: msg.type, msg: msg.msg, idList: [msg.id] })
      }
      return acc
    }, [] as { type: 'info' | 'warn' | 'error', msg: string, idList: string[] }[])
  }, [state.displayMessages])

  const handleClearAll = React.useCallback(() => {
    state.clear()
  }, [state])

  if (state.displayMessages.length === 0) {
    return undefined
  }

  return (
    <div className={`${props.className ?? ''} flex flex-col`}>
      {groupedMessages.map((msg, index) => (
        <div key={index} className={getInlineMessageClassName(msg.type)}>
          {msg.msg}
          {msg.idList.length > 1 && ` (${msg.idList.length})`}
        </div>
      ))}

      {state.displayMessages.length > 1 && (
        <div className="flex justify-start gap-2">
          <button className="text-sm text-gray-500">{state.displayMessages.length}件のメッセージ</button>
          <button className="text-sm text-gray-500" onClick={handleClearAll}>すべてクリアする</button>
        </div>
      )}
    </div>
  )
}

// --------------------------------------------

const getInlineMessageClassName = (type: 'info' | 'warn' | 'error') => {
  switch (type) {
    case 'info':
      return 'border border-blue-500 text-blue-500 bg-blue-50'
    case 'warn':
      return 'border border-amber-500 text-amber-500 bg-amber-50'
    case 'error':
      return 'border border-rose-500 text-rose-500 bg-rose-50'
  }
}

// --------------------------------------------

const InlineMessageContext = React.createContext<InlineMessageContextType>({
  displayMessages: [],
  error: () => { },
  warn: () => { },
  info: () => { },
  clear: () => { },
})

/** インラインメッセージのコンテキストの状態変更処理 */
const inlineMessageContextReducer = (state: InlineMessageContextType, action: InlineMessageContextAction): InlineMessageContextType => {
  switch (action.type) {
    case 'add':
      return { ...state, displayMessages: [...state.displayMessages, { id: UUID.generate(), ...action.msg }] }
    case 'clear':
      if (action.deleteIdList?.length) {
        return { ...state, displayMessages: state.displayMessages.filter(msg => !action.deleteIdList!.includes(msg.id)) }
      } else {
        return { ...state, displayMessages: [] }
      }
  }
}

type InlineMessageContextType = {
  displayMessages: InlineMessageItem[]
  error: (msg: string) => void
  warn: (msg: string) => void
  info: (msg: string) => void
  clear: (deleteIdList?: string[]) => void
}

type InlineMessageItem = {
  id: string
  msg: string
  type: 'info' | 'warn' | 'error'
}

type InlineMessageContextAction = {
  type: 'add'
  msg: Omit<InlineMessageItem, 'id'>
} | {
  type: 'clear'
  /** クリアするメッセージのID。指定しない場合は全てのメッセージがクリアされる。 */
  deleteIdList?: string[]
}
import React, { ButtonHTMLAttributes, InputHTMLAttributes, PropsWithoutRef, TextareaHTMLAttributes, createContext, forwardRef, useCallback, useContext, useEffect, useImperativeHandle, useMemo, useReducer, useRef, useState } from 'react'
import * as Icon from '@ant-design/icons'
import * as UUID from 'uuid'

export namespace ReactHookUtil {
  // useReducerの簡略化
  type ReducerDef<S, M extends StateModifier<S>> = (state: S) => M
  type StateModifier<S> = { [action: string]: (...args: any[]) => S }
  type DispatchArg<S, M extends StateModifier<S>> = (modifier: M) => S
  export const defineReducer = <S, M extends StateModifier<S>>(
    reducerDef: ReducerDef<S, M>
  ): React.Reducer<S, DispatchArg<S, M>> => {
    return (state, action) => {
      const modifier = reducerDef(state)
      const newState = action(modifier)
      return newState
    }
  }

  // ファイルを超えてDispatcherの型を推論したいことがあるので
  export type DispatcherOf<TReducer>
    = TReducer extends React.Reducer<any, DispatchArg<any, infer TModifier>>
    ? Dispatcher<TModifier>
    : never
  type Dispatcher<TModifier>
    = TModifier extends StateModifier<infer TState>
    ? ((modifier: DispatchArg<TState, TModifier>) => void)
    : never

  // useContextの簡略化
  type ReducerEx<S, M extends StateModifier<S>> = React.Reducer<S, DispatchArg<S, M>>
  type ContextEx<S, M extends StateModifier<S>> = React.Context<readonly [S, React.Dispatch<DispatchArg<S, M>>]>
  type ProviderComponent = (props: { children?: React.ReactNode }) => JSX.Element
  export const defineContext = <S, M extends StateModifier<S>>(
    getInitialState: () => S,
    reducerDef: ReducerDef<S, M>,
    craeteProviderContext?: (Context: ContextEx<S, M>, reducer: ReducerEx<S, M>) => ProviderComponent
  ) => {
    const reducer = defineReducer(reducerDef)
    const dummyDispatcher = (() => { }) as React.Dispatch<DispatchArg<S, M>>
    const ContextEx = createContext([getInitialState(), dummyDispatcher] as const)
    /** App直下などに置く必要あり */
    const ContextProvider: ProviderComponent
      = craeteProviderContext?.(ContextEx, reducer)
      // 既定のコンテキストプロバイダー
      ?? (({ children }) => {
        const contextValue = useReducer(reducer, getInitialState())
        return (
          <ContextEx.Provider value={contextValue}>
            {children}
          </ContextEx.Provider>
        )
      })

    /** コンテキスト使用側はこれを使う */
    const useContextEx = () => useContext(ContextEx)

    return [ContextProvider, useContextEx] as const
  }

  /** forwardRefの戻り値の型定義がややこしいので単純化するためのラッピング関数 */
  export const forwardRefEx = <TRef, TProps>(
    fn: (props: TProps, ref: React.ForwardedRef<TRef>) => React.ReactNode
  ) => {
    return forwardRef(fn) as (
      (props: PropsWithoutRef<TProps> & { ref?: React.Ref<TRef> }) => React.ReactNode
    )
  }

  // 強制アップデート
  const forceUpdateReducer = (state: boolean, _?: undefined) => {
    return !state
  }
  export const useForceUpdate = () => {
    const [forceUpdateValue, triggerForceUpdate] = useReducer(forceUpdateReducer, false)
    return { forceUpdateValue, triggerForceUpdate }
  }

  // トグル
  const toggleReducer = defineReducer((state: boolean) => ({
    toggle: () => !state,
    setValue: (v: boolean) => v,
  }))
  export const useToggle = (initialState?: boolean) => {
    return useReducer(toggleReducer, initialState ?? false)
  }
}

// --------------------------------------------------
// UIコンポーネント
export namespace Components {
  type InputWithLabelAttributes = {
    labelText?: string
    labelClassName?: string
    inputClassName?: string
  }
  export const Text = ReactHookUtil.forwardRefEx<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & InputWithLabelAttributes>((props, ref) => {
    const {
      labelText,
      labelClassName,
      inputClassName,
      className,
      autoComplete,
      ...rest
    } = props
    return (
      <label className={`flex ${className}`}>
        {(labelText || labelClassName) && (
          <span className={`select-none ${labelClassName}`}>
            {labelText}
          </span>)}
        <input ref={ref} {...rest}
          className={`flex-1 border border-1 border-zinc-400 px-1 ${inputClassName}`}
          autoComplete={autoComplete ?? 'off'}
        />
      </label>
    )
  })

  export const Textarea = ReactHookUtil.forwardRefEx<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement> & InputWithLabelAttributes>((props, ref) => {
    const {
      className,
      spellCheck,
      labelText,
      labelClassName,
      inputClassName,
      onKeyDown,
      ...rest
    } = props

    const textareaRef = useRef<HTMLTextAreaElement>(null)
    useImperativeHandle(ref, () => textareaRef.current!)
    const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = useCallback(e => {
      onKeyDown?.(e)
      if (e.key === 'Tab' && textareaRef.current) {
        const start = textareaRef.current.selectionStart
        const text = textareaRef.current.value
        textareaRef.current.value
          = text.substring(0, start)
          + '\t'
          + text.substring(start, text.length)
        textareaRef.current.setSelectionRange(start + 1, start + 1)
        e.preventDefault()
      }
    }, [onKeyDown])

    return (
      <label className={`flex ${className}`}>
        {(labelText || labelClassName) && (
          <span className={`select-none ${labelClassName}`}>
            {labelText}
          </span>)}
        <textarea ref={textareaRef} {...rest}
          className={`flex-1 border border-1 border-zinc-400 px-1 ${inputClassName}`}
          spellCheck={spellCheck ?? 'false'}
          onKeyDown={handleKeyDown}
        ></textarea>
      </label>
    )
  })

  type ButtonAttrs = {
    submit?: boolean
    icon?: React.ElementType
  }
  export const Button = ReactHookUtil.forwardRefEx<HTMLButtonElement, ButtonHTMLAttributes<HTMLButtonElement> & ButtonAttrs>((props, ref) => {
    const {
      type,
      icon,
      title,
      submit,
      className,
      children,
      ...rest
    } = props

    const className2 = icon
      ? `text-zinc-500 flex p-1 ${className}`
      : `text-white bg-zinc-500 px-1 text-nowrap
         border border-1 border-zinc-700
         select-none
         ${className}`

    return (
      <button ref={ref} {...rest}
        type={type ?? (submit ? 'submit' : 'button')}
        className={className2}
        title={title ?? (icon ? (children as string) : undefined)}
      >
        {icon
          ? React.createElement(icon)
          : children}
      </button>
    )
  })

  export const Separator = ({ className }: {
    className?: string
  }) => {
    return (
      <hr className={`bg-zinc-300 border-none h-[1px] m-2 ${className}`} />
    )
  }

  export const NowLoading = ({ className }: {
    className?: string
  }) => {
    return (
      <div className={`animate-spin border-4 border-sky-500 border-t-transparent rounded-full ${className}`} aria-label="読み込み中"></div>
    )
  }

  export const Modal = ({ children }: {
    children?: React.ReactNode
  }) => {
    return <>
      <div className="
        z-[998] fixed inset-0 w-screen
        bg-gray-500 bg-opacity-75 transition-opacity
        flex justify-center items-center">
        <dialog open className="p-2 rounded bg-white">
          {children}
        </dialog>
      </div>

    </>
  }
}

// --------------------------------------------------
// ローカルストレージへの保存と復元
export namespace StorageUtil {
  export type DeserializeResult<T> = { ok: true, obj: T } | { ok: false }
  export type LocalStorageHandler<T> = {
    storageKey: string
    serialize: (obj: T) => string
    deserialize: (str: string) => DeserializeResult<T>
    defaultValue: () => T
    noMessageOnSave?: boolean
  }

  // アプリ全体でローカルストレージのデータの更新タイミングを同期するための仕組み
  type LocalStorageData = { [storageKey: string]: unknown }
  export const [LocalStorageContextProvider] = ReactHookUtil.defineContext(() => ({}) as LocalStorageData, state => ({
    cache: <K extends keyof LocalStorageData>(key: K, value: LocalStorageData[K]) => {
      return { ...state, [key]: value }
    },
  }))
  export const useLocalStorage = <T,>(init: LocalStorageHandler<T> | (() => LocalStorageHandler<T>)) => {
    const handler = useMemo(() => typeof init === 'function' ? init() : init, [])
    const { forceUpdateValue, triggerForceUpdate } = ReactHookUtil.useForceUpdate()
    const [, dispatchMsg] = Messaging.useMsgContext()

    const data: T = useMemo(() => {
      try {
        const serialized = localStorage.getItem(handler.storageKey)
        if (serialized == null) return handler.defaultValue()

        const deserializeResult = handler.deserialize(serialized)
        if (deserializeResult?.ok !== true) {
          dispatchMsg(msg => msg.push('warn', `Failuer to parse local storage value as '${handler.storageKey}'.`))
          return handler.defaultValue()
        }

        return deserializeResult.obj

      } catch (error) {
        dispatchMsg(msg => msg.push('warn', error))
        return handler.defaultValue()
      }
    }, [forceUpdateValue])

    const save = useCallback((value: T) => {
      const serialized = handler.serialize(value)
      localStorage.setItem(handler.storageKey, serialized)
      triggerForceUpdate()
      if (!handler.noMessageOnSave) {
        dispatchMsg(msg => msg.push('info', '保存しました。'))
      }
    }, [])

    return { data, save }
  }
}

// ------------------- 木構造データの操作 --------------------
export namespace Tree {
  export type TreeNode<T> = {
    item: T
    children: TreeNode<T>[]
    parent?: TreeNode<T>
    depth: number
  }

  type ToTreeArgs<T>
    = { getId: (item: T) => string, getParent: (item: T) => string | null | undefined, getChildren?: undefined }
    | { getId: (item: T) => string, getParent?: undefined, getChildren: (item: T) => T[] | null | undefined }
  export const toTree = <T,>(items: T[], fn: ToTreeArgs<T>): TreeNode<T>[] => {
    const treeNodes = new Map<string, TreeNode<T>>(items
      .map(item => [
        fn.getId(item),
        { item, children: [], depth: -1 }
      ]))
    // 親子マッピング
    if (fn.getParent) {
      for (const node of treeNodes) {
        const parentId = fn.getParent(node[1].item)
        if (parentId == null) continue
        const parent = treeNodes.get(parentId)
        node[1].parent = parent
        parent?.children.push(node[1])
      }
      for (const node of treeNodes) {
        node[1].depth = getDepth(node[1])
      }
    } else {
      const createChildrenRecursively = (parent: TreeNode<T>): void => {
        const childrenItems = fn.getChildren(parent.item) ?? []
        for (const childItem of childrenItems) {
          const childNode: TreeNode<T> = {
            item: childItem,
            depth: parent.depth + 1,
            parent,
            children: [],
          }
          parent.children.push(childNode)
          createChildrenRecursively(childNode)
        }
      }
      for (const node of treeNodes) {
        node[1].depth = 0
        createChildrenRecursively(node[1])
      }
    }
    // ルートのみ返す
    return Array
      .from(treeNodes.values())
      .filter(node => node.depth === 0)
  }

  export const getAncestors = <T,>(node: TreeNode<T>): TreeNode<T>[] => {
    const arr: TreeNode<T>[] = []
    let parent = node.parent
    while (parent) {
      arr.push(parent)
      parent = parent.parent
    }
    return arr.reverse()
  }
  export const flatten = <T,>(nodes: TreeNode<T>[]): TreeNode<T>[] => {
    return nodes.flatMap(node => getDescendantsAndSelf(node))
  }
  export const getDescendantsAndSelf = <T,>(node: TreeNode<T>): TreeNode<T>[] => {
    return [node, ...getDescendants(node)]
  }
  export const getDescendants = <T,>(node: TreeNode<T>): TreeNode<T>[] => {
    const arr: TreeNode<T>[] = []
    const pushRecursively = (n: TreeNode<T>): void => {
      for (const child of n.children) {
        arr.push(child)
        pushRecursively(child)
      }
    }
    pushRecursively(node)
    return arr
  }
  export const getDepth = <T,>(node: TreeNode<T>): number => {
    return getAncestors(node).length
  }
}

// ------------------- メッセージ --------------------
export namespace Messaging {
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
    ErrorMessageContextProvider,
    useMsgContext,
  ] = ReactHookUtil.defineContext((): State => ({
    inline: [] as Msg[],
    toast: [] as Msg[],
  }), state => ({

    push: (type: Msg['type'], ...messages: unknown[]) => {
      if (type === 'error') console.error(...messages)

      const flatten = messages.flatMap(m => Array.isArray(m) ? m : [m])
      const addedMessages = flatten.map<Msg>(m => {
        const id = UUID.v4()
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
    },

    clear: (nameOrItem?: string | Msg) => {
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
    },
  }))

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
              <Components.Button
                onClick={() => dispatch(state => state.clear(msg))}
                icon={Icon.CloseOutlined}
              />
            </li>
          ))}
        </ul>
        {filtered.length > 5 && (
          <div className="flex text-sm select-none items-center">
            {filtered.length}件の警告とエラー
            <div className="flex-1"></div>
            <Components.Button onClick={() => dispatch(msg => msg.clear(name))}>
              すべてクリア
            </Components.Button>
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
          z-[300] select-none cursor-pointer overflow-hidden
          ${(visible ? 'animate-slideIn' : 'animate-slideOut translate-x-[calc(-100%-1rem)]')}
          fixed left-4 bottom-4 p-2 w-64 h-24
          bg-sky-950 text-sky-50 border border-1 boder-sky-500
          ${className}`}>
        {msg.message}
        <Icon.CloseOutlined
          className="absolute right-2 top-2 pointer-none"
        />
      </div>
    )
  }
}

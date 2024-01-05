import React, { ButtonHTMLAttributes, InputHTMLAttributes, PropsWithoutRef, TextareaHTMLAttributes, createContext, forwardRef, useCallback, useContext, useMemo, useReducer } from 'react'
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

  // useContextの簡略化
  export const defineContext = <S, M extends StateModifier<S>>(
    getInitialState: () => S,
    reducerDef: ReducerDef<S, M>
  ) => {
    const reducer = defineReducer(reducerDef)
    const dummyDispatcher = (() => { }) as React.Dispatch<DispatchArg<S, M>>
    const ContextEx = createContext([getInitialState(), dummyDispatcher] as const)
    /** App直下などに置く必要あり */
    const ContextProvider = ({ children }: { children?: React.ReactNode }) => {
      const contextValue = useReducer(reducer, getInitialState())
      return (
        <ContextEx.Provider value={contextValue}>
          {children}
        </ContextEx.Provider>
      )
    }
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
      ...rest
    } = props
    return (
      <label className={`flex ${className}`}>
        {(labelText || labelClassName) && (
          <span className={`select-none ${labelClassName}`}>
            {labelText}
          </span>)}
        <textarea ref={ref} {...rest}
          className={`flex-1 border border-1 border-zinc-400 px-1 ${inputClassName}`}
          spellCheck={spellCheck ?? 'false'}
        ></textarea>
      </label>)
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
  }

  // アプリ全体でローカルストレージのデータの更新タイミングを同期するための仕組み
  type LocalStorageData = { [storageKey: string]: unknown }
  export const [
    LocalStorageContextProvider,
    useLocalStorageContext,
  ] = ReactHookUtil.defineContext(() => ({}) as LocalStorageData, state => ({
    cache: <K extends keyof LocalStorageData>(key: K, value: LocalStorageData[K]) => {
      return { ...state, [key]: value }
    },
  }))
  export const useLocalStorage = <T,>(init: LocalStorageHandler<T> | (() => LocalStorageHandler<T>)) => {
    const handler = useMemo(() => typeof init === 'function' ? init() : init, [])
    const { forceUpdateValue, triggerForceUpdate } = ReactHookUtil.useForceUpdate()
    const [, dispatchErrMsg] = Messaging.useMsgContext()

    const data: T = useMemo(() => {
      try {
        const serialized = localStorage.getItem(handler.storageKey)
        if (serialized == null) return handler.defaultValue()

        const deserializeResult = handler.deserialize(serialized)
        if (deserializeResult?.ok !== true) {
          dispatchErrMsg(msg => msg.push('warn', `Failuer to parse local storage value as '${handler.storageKey}'.`))
          return handler.defaultValue()
        }

        return deserializeResult.obj

      } catch (error) {
        dispatchErrMsg(msg => msg.push('warn', error))
        return handler.defaultValue()
      }
    }, [forceUpdateValue])

    const save = useCallback((value: T) => {
      const serialized = handler.serialize(value)
      localStorage.setItem(handler.storageKey, serialized)
      triggerForceUpdate()
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
  type Msg = { id: string, name?: string, message: string, type: 'error' | 'warn' }
  export const [
    ErrorMessageContextProvider,
    useMsgContext,
  ] = ReactHookUtil.defineContext(() => ({ messages: [] as Msg[] }), state => ({

    push: (type: Msg['type'], ...messages: unknown[]) => {
      const flatten = messages.flatMap(m => Array.isArray(m) ? m : [m])
      const addedMessages = flatten.map<Msg>(m => {
        const id = UUID.v4()
        if (typeof m === 'string') return { id, type, message: m }
        const asErrMsg = m as Omit<Msg, 'id'>
        if (typeof asErrMsg.message === 'string') return { id, type, message: asErrMsg.message, name: asErrMsg.name }
        return { id, type, message: m?.toString() ?? '' }
      })
      return { messages: [...state.messages, ...addedMessages] }
    },

    clear: (nameOrItem?: string | Msg) => {
      if (!nameOrItem) {
        return { messages: [] }
      } else if (typeof nameOrItem === 'string') {
        const name = nameOrItem
        return { messages: state.messages.filter(m => !m.name?.startsWith(name)) }
      } else {
        const id = nameOrItem.id
        return { messages: state.messages.filter(m => m.id !== id) }
      }
    },
  }))
  export const MessageList = ({ type, name, className }: {
    type?: Msg['type']
    name?: string
    className?: string
  }) => {
    const [{ messages: messages }, dispatch] = useMsgContext()
    const filtered = useMemo(() => {
      let arr = [...messages]
      if (type) arr = arr.filter(m => m.type === type)
      if (name) arr = arr.filter(m => m.name?.startsWith(name))
      return arr
    }, [messages, name])

    return (
      <ul className={`flex flex-col ${className}`}>
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
            >×</Components.Button>
          </li>
        ))}
      </ul>
    )
  }
}

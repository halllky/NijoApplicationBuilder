import React, { ButtonHTMLAttributes, Dispatch, InputHTMLAttributes, PropsWithoutRef, Reducer, TextareaHTMLAttributes, createContext, forwardRef, useCallback, useContext, useEffect, useMemo, useReducer } from 'react'
import * as UUID from 'uuid'

/** forwardRefの戻り値の型定義がややこしいので単純化するためのラッピング関数 */
export const forwardRefEx = <TRef, TProps>(
  fn: (props: TProps, ref: React.ForwardedRef<TRef>) => React.ReactNode
) => {
  return forwardRef(fn) as (
    (props: PropsWithoutRef<TProps> & { ref?: React.Ref<TRef> }) => React.ReactNode
  )
}

// --------------------------------------------------
// UIコンポーネント
export namespace Components {
  type InputWithLabelAttributes = {
    labelText?: string
    labelClassName?: string
    inputClassName?: string
  }
  export const Text = forwardRefEx<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & InputWithLabelAttributes>((props, ref) => {
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
          className={`flex-1 border border-1 border-slate-400 px-1 ${inputClassName}`}
          autoComplete={autoComplete ?? 'off'}
        />
      </label>
    )
  })

  export const Textarea = forwardRefEx<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement> & InputWithLabelAttributes>((props, ref) => {
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
          className={`flex-1 border border-1 border-slate-400 px-1 ${inputClassName}`}
          spellCheck={spellCheck ?? 'false'}
        ></textarea>
      </label>)
  })

  type ButtonAttrs = {
    submit?: boolean
  }
  export const Button = forwardRefEx<HTMLButtonElement, ButtonHTMLAttributes<HTMLButtonElement> & ButtonAttrs>((props, ref) => {
    const {
      type,
      submit,
      className,
      ...rest
    } = props
    return (
      <button ref={ref} {...rest}
        type={type ?? (submit ? 'submit' : 'button')}
        className={`text-white bg-slate-500
          px-1 text-nowrap
          border border-1 border-slate-700
          ${className}`}
      ></button>
    )
  })

  export const Separator = () => {
    return (
      <hr className="bg-slate-300 border-none h-[1px] m-2" />
    )
  }
}

// --------------------------------------------------
// 状態の型定義からreducer等の型定義をするのを簡略化するための仕組み
export namespace ContextUtil {
  type ActionParam<TState, TKey extends keyof TState = keyof TState> = {
    update: TKey
    value: TState[TKey]
  }
  type FlatObjectContext<TState> = React.Context<[TState, Dispatch<ActionParam<TState>>]>

  const reducerEx: Reducer<{}, ActionParam<{}>> = (state, action) => {
    return { ...state, [action.update]: action.value }
  }
  export const createContextEx = <TState,>(defaultValue: TState): FlatObjectContext<TState> => {
    return createContext([
      defaultValue,
      (() => { }) as Dispatch<ActionParam<TState>>
    ] as const)
  }
  export const useContextEx = <TState,>(ctx: FlatObjectContext<TState>) => {
    return useContext(ctx) as [
      TState,
      <TKey extends keyof TState>(action: ActionParam<TState, TKey>) => TState
    ]
  }
  export const useReducerEx = <TState,>(initialState: TState) => {
    return useReducer(
      reducerEx as unknown as Reducer<TState, ActionParam<TState>>,
      initialState) as [
        TState,
        <TKey extends keyof TState>(action: ActionParam<TState, TKey>) => TState
      ]
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
  const LocalStorageContext = ContextUtil.createContextEx({} as LocalStorageData)
  export const LocalStorageContextProvider = ({ children }: {
    children?: React.ReactNode
  }) => {
    const contextValue = ContextUtil.useReducerEx({} as LocalStorageData)
    return (
      <LocalStorageContext.Provider value={contextValue}>
        {children}
      </LocalStorageContext.Provider>
    )
  }

  export const useLocalStorage = <T,>(handler: LocalStorageHandler<T>) => {
    const [dataSet, dispatch] = ContextUtil.useContextEx(LocalStorageContext)
    const data: T = useMemo(() => {
      const cachedData = dataSet[handler.storageKey] as T | undefined
      return cachedData ?? handler.defaultValue()
    }, [dataSet[handler.storageKey], handler.defaultValue])

    useEffect(() => {
      // 初期表示時、LocalStorageの値をキャッシュに読み込む
      const serialized = localStorage.getItem(handler.storageKey)
      if (serialized == null) return
      const deserialized = handler.deserialize(serialized)
      if (!deserialized.ok) {
        // 保存されているが型が不正な場合
        console.warn(`Failuer to parse local storage value as '${handler.storageKey}'.`)
        return
      }
      dispatch({ update: handler.storageKey, value: deserialized.obj })
    }, [handler])

    const save = useCallback((value: T) => {
      const serialized = handler.serialize(value)
      localStorage.setItem(handler.storageKey, serialized)
      dispatch({ update: handler.storageKey, value })
    }, [handler])

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

// ------------------- エラーハンドリング --------------------
export namespace ErrorHandling {
  type ErrMsg = { id: string, name?: string, message: string, type: 'error' | 'warn' }
  type ErrMsgCtx = {
    errorMessages: ErrMsg[]
    addErrorMessages: (...messages: unknown[]) => void
    addWarningMessages: (...messages: unknown[]) => void
    clearErrorMessages: (filterOrItem?: string | ErrMsg) => void
  }
  type ReducerState = { errorMessages: ErrMsg[] }
  type ReducerAction
    = { type: 'add', msgs: Parameters<ErrMsgCtx['addErrorMessages']> }
    | { type: 'add-warn', msgs: Parameters<ErrMsgCtx['addErrorMessages']> }
    | { type: 'clear', nameOrItem: Parameters<ErrMsgCtx['clearErrorMessages']>[0] }
  const reducer = (state: ReducerState, action: ReducerAction): ReducerState => {
    switch (action.type) {
      case 'add':
      case 'add-warn':
        const type = action.type === 'add-warn' ? 'warn' : 'error'
        const flatten = action.msgs.flatMap(m => Array.isArray(m) ? m : [m])
        const addedMessages = flatten.map<ErrMsg>(m => {
          const id = UUID.v4()
          if (typeof m === 'string') return { id, type, message: m }
          const asErrMsg = m as Omit<ErrMsg, 'id'>
          if (typeof asErrMsg.message === 'string') return { id, type, message: asErrMsg.message, name: asErrMsg.name }
          return { id, type, message: m?.toString() ?? '' }
        })
        return { errorMessages: [...state.errorMessages, ...addedMessages] }
      case 'clear':
        if (!action.nameOrItem) {
          return { errorMessages: [] }
        } else if (typeof action.nameOrItem === 'string') {
          const name = action.nameOrItem
          return { errorMessages: state.errorMessages.filter(m => !m.name?.startsWith(name)) }
        } else {
          const id = action.nameOrItem.id
          return { errorMessages: state.errorMessages.filter(m => m.id !== id) }
        }
    }
  }

  const getEmptyCtxValue = (): ErrMsgCtx => ({
    errorMessages: [],
    addErrorMessages: () => { },
    addWarningMessages: () => { },
    clearErrorMessages: () => { },
  })
  const ErrorMessageContext = createContext(getEmptyCtxValue())

  export const useMsgContext = () => useContext(ErrorMessageContext)
  export const ErrorMessageContextProvider = ({ children }: {
    children?: React.ReactNode
  }) => {
    const [{ errorMessages }, dispatch] = useReducer(reducer, { errorMessages: [] })
    const addErrorMessages: ErrMsgCtx['addErrorMessages'] = useCallback((...msgs) => {
      dispatch({ type: 'add', msgs })
    }, [])
    const addWarningMessages: ErrMsgCtx['addErrorMessages'] = useCallback((...msgs) => {
      dispatch({ type: 'add-warn', msgs })
    }, [])
    const clearErrorMessages: ErrMsgCtx['clearErrorMessages'] = useCallback(nameOrItem => {
      dispatch({ type: 'clear', nameOrItem })
    }, [])

    return (
      <ErrorMessageContext.Provider value={{
        errorMessages,
        addErrorMessages,
        addWarningMessages,
        clearErrorMessages
      }}>
        {children}
      </ErrorMessageContext.Provider>
    )
  }
  export const MessageList = ({ filter, className }: {
    filter?: string
    className?: string
  }) => {
    const { errorMessages, clearErrorMessages } = useMsgContext()
    const filtered = useMemo(() => {
      return filter
        ? errorMessages.filter(m => m.name?.startsWith(filter))
        : errorMessages
    }, [errorMessages, filter])

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
              onClick={() => clearErrorMessages(msg)}
            >×</Components.Button>
          </li>
        ))}
      </ul>
    )
  }
}

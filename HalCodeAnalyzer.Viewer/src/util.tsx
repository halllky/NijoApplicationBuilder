import { ButtonHTMLAttributes, Dispatch, InputHTMLAttributes, PropsWithoutRef, Reducer, TextareaHTMLAttributes, createContext, forwardRef, useCallback, useContext, useReducer } from 'react'

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
        className={`text-white bg-slate-500 border border-1 border-slate-700 px-1 ${className}`}
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
// ローカルストレージへの保存と復元
export namespace StorageUtil {
  export type DeserializeResult<T> = { ok: true, obj: T } | { ok: false }
  export type Serializer<T> = {
    storageKey: string
    serialize: (obj: T) => string
    deserialize: (str: string) => DeserializeResult<T>
  }
  export const useLocalStorage = <T,>(serializer: Serializer<T>) => {
    const load = useCallback((): DeserializeResult<T> => {
      const serialized = localStorage.getItem(serializer.storageKey)
      if (serialized == null) return { ok: false }
      return serializer.deserialize(serialized)
    }, [serializer])
    const save = useCallback((obj: T) => {
      const serialized = serializer.serialize(obj)
      localStorage.setItem(serializer.storageKey, serialized)
    }, [serializer])
    return { load, save }
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
        createChildrenRecursively(node[1])
      }
    }
    // 深さ計算
    for (const node of treeNodes) {
      node[1].depth = getDepth(node[1])
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

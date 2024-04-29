import React, { useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import * as Collection from '../collection'
import * as Input from '../input'
import * as Tree from './Tree'
import * as Notification from './Notification'
import { useIndexedDbTable } from './Storage'
import { useLocalRepositoryCommitHandling } from './LocalRepository.Commit'


// 一覧/特定集約 共用

export type LocalRepositoryState
  = '' // No Change (Exists only remote repository)
  | '+' // Add
  | '*' // Modify
  | '-' // Delete

export type LocalRepositoryStoredItem<T = object> = {
  /** データの種類を一意に識別する文字列。基本的には集約の名前 */
  dataTypeKey: string
  /**
   * データを一意に識別する文字列。
   * - 新規作成された未保存のデータの場合は、主キーの値と関係しない自動採番されたUUID。なおこのUUIDは登録確定後に消える。
   * - 保存されたデータの場合は主キーの配列のJSON。
   */
  itemKey: ItemKey
  /** 画面に表示される名前 */
  itemName: string
  item: T
  state: LocalRepositoryState
}
const itemKeySymbol: unique symbol = Symbol()
export type ItemKey = string & { [itemKeySymbol]: never }

const useIndexedDbLocalRepositoryTable = () => {
  return useIndexedDbTable<LocalRepositoryStoredItem>({
    dbName: '::nijo::',
    dbVersion: 1,
    tableName: 'LocalRepository',
    keyPath: ['dataTypeKey', 'itemKey'],
  })
}

// -------------------------------------------------
// ローカルリポジトリ変更一覧

export type LocalRepositoryItemListItem = {
  dataTypeKey: string
  itemKey: ItemKey
  itemName: string
  state: LocalRepositoryState
}

export type LocalRepositoryContextValue = {
  changes: LocalRepositoryItemListItem[]
  changesCount: number
  reload: () => Promise<void>
  commit: (handler: SaveLocalItemHandler, ...keys: { dataTypeKey: string, itemKey: ItemKey }[]) => Promise<boolean>
  reset: (...keys: { dataTypeKey: string, itemKey: ItemKey }[]) => Promise<void>
  ready: boolean
}
const LocalRepositoryContext = React.createContext<LocalRepositoryContextValue>({
  changes: [],
  changesCount: 0,
  reload: () => Promise.resolve(),
  commit: () => Promise.resolve(false),
  reset: () => Promise.resolve(),
  ready: false,
})

export type SaveLocalItemHandler<T = object> = (localItem: LocalRepositoryStoredItem<T>) => Promise<{ commit: boolean }>

export const LocalRepositoryContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const [, dispatchMsg] = Notification.useMsgContext()
  const { ready, openCursor, commandToTable } = useIndexedDbLocalRepositoryTable()
  const [changes, setChanges] = useState<LocalRepositoryItemListItem[]>([])

  const changesCount = useMemo(() => {
    return changes.length
  }, [changes])

  const reload = useCallback(async () => {
    const changes: LocalRepositoryItemListItem[] = []
    await openCursor('readonly', cursor => {
      const { dataTypeKey, state, itemKey, itemName } = cursor.value
      changes.push({ dataTypeKey, state, itemKey, itemName })
    })
    setChanges(changes)
  }, [openCursor, setChanges])

  const reset = useCallback(async (...keys: { dataTypeKey: string, itemKey: ItemKey }[]): Promise<void> => {
    await commandToTable(table => {
      if (keys.length === 0) {
        table.clear()
      } else {
        for (const { dataTypeKey, itemKey } of keys) {
          table.delete([dataTypeKey, itemKey])
        }
      }
    })
    await reload()
  }, [commandToTable, reload])

  const commit = useCallback(async (handler: SaveLocalItemHandler, ...keys: { dataTypeKey: string, itemKey: ItemKey }[]) => {
    // ローカルリポジトリ内のデータの読み込み
    const localItems: LocalRepositoryStoredItem[] = []
    await openCursor('readonly', cursor => {
      if (keys.length === 0 || keys.some(k =>
        k.dataTypeKey === cursor.value.dataTypeKey
        && k.itemKey === cursor.value.itemKey)) {
        localItems.push({ ...cursor.value })
      }
    })
    // 保存処理ハンドラの呼び出し
    const commitedKeys: [string, ItemKey][] = []
    let allCommited = true
    for (const stored of localItems) {
      const { commit } = await handler(stored)
      if (commit) {
        commitedKeys.push([stored.dataTypeKey, stored.itemKey])
      } else {
        allCommited = false
      }
    }
    // 保存完了したデータをローカルリポジトリから削除する
    await commandToTable(table => {
      for (const [dataTypeKey, itemKey] of commitedKeys) {
        table.delete([dataTypeKey, itemKey])
      }
    })
    await reload()
    return allCommited
  }, [openCursor, reload, dispatchMsg])

  const contextValue: LocalRepositoryContextValue = useMemo(() => ({
    changes,
    changesCount,
    reload,
    reset,
    commit,
    ready,
  }), [changes, ready, reload])

  useEffect(() => {
    if (ready) reload()
  }, [ready, reload])

  return (
    <LocalRepositoryContext.Provider value={contextValue}>
      {children}
    </LocalRepositoryContext.Provider>
  )
}

export const useLocalRepositoryChangeList = () => {
  return useContext(LocalRepositoryContext)
}

export const LocalReposChangeListPage = () => {
  const { changes, reset, commit } = useLocalRepositoryChangeList()
  const handleCommitData = useLocalRepositoryCommitHandling()
  const dtRef = useRef<Collection.DataTableRef<LocalRepositoryItemListItem>>(null)

  const handleCommit = useCallback(async () => {
    if (!window.confirm('変更を確定します。よろしいですか？')) return
    const selected = (dtRef.current?.getSelectedRows() ?? []).map(x => ({
      dataTypeKey: x.row.dataTypeKey,
      itemKey: x.row.itemKey,
    }))
    await handleCommitData(commit, ...selected)
  }, [commit, handleCommitData])

  const handleReset = useCallback(() => {
    if (!window.confirm('変更を取り消します。よろしいですか？')) return
    const selected = (dtRef.current?.getSelectedRows() ?? []).map(x => ({
      dataTypeKey: x.row.dataTypeKey,
      itemKey: x.row.itemKey,
    }))
    reset(...selected)
  }, [reset])

  return (
    <div className="page-content-root">
      <div className="flex gap-2 justify-start">
        <span className="font-bold">一時保存</span>
        <div className="flex-1"></div>
        <Input.Button onClick={handleCommit}>確定</Input.Button>
        <Input.Button onClick={handleReset}>取り消し</Input.Button>
      </div>
      <Collection.DataTable
        ref={dtRef}
        data={changes}
        columns={CHANGE_LIST_COLS}
        className="flex-1"
      />
    </div>
  )
}
const CHANGE_LIST_COLS: Collection.ColumnDefEx<Tree.TreeNode<LocalRepositoryItemListItem>>[] = [
  { id: 'col0', header: '状態', accessorFn: x => x.item.state, size: 12 },
  { id: 'col1', header: '種類', accessorFn: x => x.item.dataTypeKey },
  { id: 'col2', header: '名前', accessorFn: x => x.item.itemName },
]
// -------------------------------------------------
// 特定の集約の変更

export type LocalRepositoryArgs<T> = {
  dataTypeKey: string
  getItemKey: (t: T) => string
  getItemName?: (t: T) => string
  loadRemoteItems?: () => Promise<T[]>
}
export type LocalRepositoryItem<T> = {
  itemKey: ItemKey
  state: LocalRepositoryState
  item: T
}

export const useLocalRepository = <T extends object>({
  dataTypeKey,
  getItemKey,
  getItemName,
  loadRemoteItems,
}: LocalRepositoryArgs<T>) => {

  const { ready: ready1, reload: reloadContext } = useContext(LocalRepositoryContext)
  const { ready: ready2, openCursor, queryToTable } = useIndexedDbLocalRepositoryTable()
  const [ready3, setReady3] = useState(false)

  const [remoteAndLocalItems, setRemoteAndLocalItems] = useState<LocalRepositoryItem<T>[]>(() => [])

  const reload = useCallback(async () => {
    if (!ready1 || !ready2) return
    setReady3(false)
    try {
      // リモート読み込み
      const remoteItems = (await loadRemoteItems?.()) ?? []

      // ローカル読み込み
      const localItems: LocalRepositoryItem<T>[] = []
      await openCursor('readonly', cursor => {
        if (cursor.value.dataTypeKey !== dataTypeKey) return
        const { state, itemKey, item } = cursor.value
        localItems.push({ state, itemKey, item: item as T })
      })

      // 合成
      const remoteAndLocal = crossJoin(
        localItems, local => local.itemKey,
        remoteItems, remote => getItemKey(remote) as ItemKey,
      ).map<LocalRepositoryItem<T>>(pair => {
        return pair.left ?? { state: '', itemKey: pair.key, item: pair.right }
      })
      setRemoteAndLocalItems(remoteAndLocal)

    } finally {
      setReady3(true)
    }
  }, [ready1, ready2, loadRemoteItems, openCursor, getItemKey, dataTypeKey])

  useEffect(() => {
    reload()
  }, [reload])

  const updateLocalRepositoryItem = useCallback(async ({ item, itemKey, state }: LocalRepositoryItem<T>) => {
    const itemName = getItemName?.(item) ?? ''
    await queryToTable(table => table.put({ dataTypeKey, itemKey, itemName, state, item }))
    return { item, itemKey, state }
  }, [dataTypeKey, queryToTable, getItemName])

  const [, dispatchMsg] = Notification.useMsgContext()
  /** 引数に渡されたデータの値を見てstateを適切に変更し然るべき場所への保存を判断し実行する。
   * TODO: add, update, deleteを全部これに一本化したい */
  const commit = useCallback(async (...items: LocalRepositoryItem<T>[]): Promise<LocalRepositoryItem<T>[]> => {
    // リモート読み込み
    const remoteItems = (await loadRemoteItems?.()) ?? []
    // ローカル読み込み
    const localItems: LocalRepositoryItem<T>[] = []
    await openCursor('readonly', cursor => {
      if (cursor.value.dataTypeKey !== dataTypeKey) return
      const { state, itemKey, item } = cursor.value
      localItems.push({ state, itemKey, item: item as T })
    })
    // 合成
    const remoteAndLocalTemp = crossJoin(
      localItems, local => local.itemKey,
      remoteItems, remote => getItemKey(remote) as ItemKey,
    ).map<readonly [ItemKey, LocalRepositoryItem<T>]>(pair => {
      return [pair.key, pair.left ?? { state: '', itemKey: pair.key, item: pair.right }] as const
    })
    const remoteAndLocal = new Map(remoteAndLocalTemp)

    // -------------------------
    // 状態更新。UIで不具合が起きる可能性を考慮し、とにかくエラーチェックを厳格に作る
    const result: LocalRepositoryItem<T>[] = []
    for (const { item, state, itemKey } of items) {
      // TODO: 楽観排他制御の考慮が無い
      const stored = remoteAndLocal.get(itemKey)
      if (state === '*') {
        if (!stored) {
          result.push(await updateLocalRepositoryItem({ item, itemKey, state: '+' }))
          dispatchMsg(msg => msg.warn(`更新対象データがリモートにもローカルにもありません: ${itemKey}`))
        } else if (stored.state === '+') {
          result.push(await updateLocalRepositoryItem({ item, itemKey, state: '+' }))
          dispatchMsg(msg => msg.warn(`新規作成データをデータ修正の区分で変更しようとしました: ${itemKey}`))
        } else if (stored.state === '-') {
          result.push(await updateLocalRepositoryItem({ item, itemKey, state: '-' }))
        } else {
          result.push(await updateLocalRepositoryItem({ item, itemKey, state: '*' }))
        }

      } else if (state === '+') {
        if (!stored) {
          result.push(await updateLocalRepositoryItem({ item, itemKey, state: '+' }))
        } else if (stored.state === '+') {
          result.push(await updateLocalRepositoryItem({ item, itemKey, state: '+' }))
        } else if (stored) {
          result.push(await updateLocalRepositoryItem({ itemKey, item, state: stored.state }))
          dispatchMsg(msg => msg.warn(`既に存在するデータと同じキーで新規追加しようとしました: ${itemKey}`))
        }

      } else if (state === '-') {
        if (!stored) {
          result.push({ itemKey, state, item })
          dispatchMsg(msg => msg.warn(`削除しようとしているデータが存在しません: ${itemKey}`))
        } else if (stored.state === '+') {
          await queryToTable(table => table.delete([dataTypeKey, itemKey]))
        } else {
          result.push(await updateLocalRepositoryItem({ item, itemKey, state: '-' }))
        }

      } else {
        if (stored) {
          result.push(stored)
        } else {
          result.push({ itemKey, state, item })
          dispatchMsg(msg => msg.warn(`リモートにもローカルにも存在しないデータがあります: ${itemKey}`))
        }
      }
    }
    await reloadContext()
    return result
  }, [loadRemoteItems, openCursor, queryToTable, updateLocalRepositoryItem, reloadContext, dispatchMsg, dataTypeKey, getItemKey])

  return {
    ready: ready1 && ready2 && ready3,
    items: remoteAndLocalItems,
    reload,
    commit,
  }
}

// ------------------------------------

const crossJoin = <T1, T2, TKey>(
  left: T1[], getKeyLeft: (t: T1) => TKey,
  right: T2[], getKeyRight: (t: T2) => TKey
): CrossJoinResult<T1, T2, TKey>[] => {

  const sortedLeft = [...left]
  sortedLeft.sort((a, b) => {
    const keyA = getKeyLeft(a)
    const keyB = getKeyLeft(b)
    if (keyA < keyB) return -1
    if (keyA > keyB) return 1
    return 0
  })
  const sortedRight = [...right]
  sortedRight.sort((a, b) => {
    const keyA = getKeyRight(a)
    const keyB = getKeyRight(b)
    if (keyA < keyB) return -1
    if (keyA > keyB) return 1
    return 0
  })
  const result: CrossJoinResult<T1, T2, TKey>[] = []
  let cursorLeft = 0
  let cursorRight = 0
  while (true) {
    const left = sortedLeft[cursorLeft]
    const right = sortedRight[cursorRight]
    if (left === undefined && right === undefined) {
      break
    }
    if (left === undefined && right !== undefined) {
      result.push({ key: getKeyRight(right), right })
      cursorRight++
      continue
    }
    if (left !== undefined && right === undefined) {
      result.push({ key: getKeyLeft(left), left })
      cursorLeft++
      continue
    }
    const keyLeft = getKeyLeft(left)
    const keyRight = getKeyRight(right)
    if (keyLeft === keyRight) {
      result.push({ key: keyLeft, left, right })
      cursorLeft++
      cursorRight++
    } else if (keyLeft < keyRight) {
      result.push({ key: keyLeft, left })
      cursorLeft++
    } else if (keyLeft > keyRight) {
      result.push({ key: keyRight, right })
      cursorRight++
    }
  }
  return result
}
type CrossJoinResult<T1, T2, TKey>
  = { key: TKey, left: T1, right: T2 }
  | { key: TKey, left: T1, right?: never }
  | { key: TKey, left?: never, right: T2 }

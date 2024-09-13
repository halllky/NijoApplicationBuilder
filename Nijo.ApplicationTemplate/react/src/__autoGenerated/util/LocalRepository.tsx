import React, { useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import * as Collection from '../collection'
import * as Input from '../input'
import * as Tree from './Tree'
import * as Notification from './Notification'
import { useIndexedDbTable } from './Storage'
import { useLocalRepositoryCommitHandling } from './LocalRepository.Commit'
import { SideMenuCollapseButton } from './SideMenuCollapseButton'


// 一覧/特定集約 共用

export type LocalRepositoryState
  = '' // No Change (Exists only remote repository)
  | '+' // Add
  | '*' // Modify
  | '-' // Delete
/** 引数のオブジェクトが、更新確定時に、新規追加・更新・削除のいずれの処理にかけられるかの種別を計算して返します。 */
export const getUpdateType = (item: {
  /** このデータがDBに保存済みかどうか */
  existsInDatabase: boolean
  /** このデータに更新がかかっているかどうか */
  willBeChanged: boolean
  /** このデータが更新確定時に削除されるかどうか */
  willBeDeleted: boolean
}): LocalRepositoryState => {
  if (item.willBeDeleted) return '-'
  if (!item.existsInDatabase) return '+'
  if (item.willBeChanged) return '*'
  return ''
}
/** getUpdateStateと一部のプロパティ名が異なるオブジェクトのための関数。やっていることは同じ */
export const getLocalRepositoryState = (item: Pick<LocalRepositoryItem<unknown>, 'existsInRemoteRepository' | 'willBeChanged' | 'willBeDeleted'>): LocalRepositoryState => {
  if (item.willBeDeleted) return '-'
  if (!item.existsInRemoteRepository) return '+'
  if (item.willBeChanged) return '*'
  return ''
}

export type LocalRepositoryItem<T> = {
  itemKey: ItemKey
  item: T
  existsInRemoteRepository: boolean
  willBeChanged: boolean
  willBeDeleted: boolean
}
export type LocalRepositoryStoredItem<T = object> = LocalRepositoryItem<T> & {
  /** データの種類を一意に識別する文字列。基本的には集約の名前 */
  dataTypeKey: string
  /** 画面に表示される名前 */
  itemName: string
}
const itemKeySymbol: unique symbol = Symbol()
/**
 * データを一意に識別する文字列。
 * - 新規作成された未保存のデータの場合は、主キーの値と関係しない自動採番されたUUID。なおこのUUIDは登録確定後に消える。
 * - 保存されたデータの場合は主キーの配列のJSON。
 */
export type ItemKey = string & { [itemKeySymbol]: never }

export const useIndexedDbLocalRepositoryTable = () => {
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
export const useLocalRepositoryContext = () => useContext(LocalRepositoryContext)

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
      const { dataTypeKey, itemKey, itemName } = cursor.value
      changes.push({ dataTypeKey, itemKey, itemName, state: getLocalRepositoryState(cursor.value) })
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
      <div className="flex gap-1 p-1 justify-start">
        <SideMenuCollapseButton />
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
const CHANGE_LIST_COLS: Collection.DataTableColumn<LocalRepositoryItemListItem>[] = [
  { id: 'col0', header: '状態', render: x => x.state, defaultWidthPx: 12, onClipboardCopy: row => row.state },
  { id: 'col1', header: '種類', render: x => x.dataTypeKey, onClipboardCopy: row => row.dataTypeKey },
  { id: 'col2', header: '名前', render: x => x.itemName, onClipboardCopy: row => row.itemName },
]

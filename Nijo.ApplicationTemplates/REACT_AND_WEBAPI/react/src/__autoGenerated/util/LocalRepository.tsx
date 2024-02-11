import React, { useCallback, useContext, useEffect, useMemo, useReducer, useState } from 'react'
import { UUID } from 'uuidjs'
import * as ReactUtil from './ReactUtil'
import * as Validation from './Validation'
import * as Notification from './Notification'
import { useFieldArray } from 'react-hook-form'
import { useIndexedDbTable } from './Storage'


// 一覧/特定集約 共用

export type LocalRepositoryState
  = '' // No Change (Exists only remote repository)
  | '+' // Add
  | '*' // Modify
  | '-' // Delete

type LocalRepositoryStoredItem = {
  dataTypeKey: string
  itemKey: ItemKey
  itemName: string
  item: object
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

type LocalRepositoryContextValue = {
  changes: LocalRepositoryItemListItem[]
  reload: () => Promise<void>
  ready: boolean
}
const LocalRepositoryContext = React.createContext<LocalRepositoryContextValue>({
  changes: [],
  reload: () => Promise.resolve(),
  ready: false,
})

export const LocalRepositoryContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const { ready, openCursor } = useIndexedDbLocalRepositoryTable()
  const [changes, setChanges] = useState<LocalRepositoryItemListItem[]>([])

  const reload = useCallback(async () => {
    const changes: LocalRepositoryItemListItem[] = []
    await openCursor('readonly', cursor => {
      changes.push({
        dataTypeKey: cursor.value.dataTypeKey,
        state: cursor.value.state,
        itemKey: cursor.value.itemKey,
        itemName: cursor.value.itemName,
      })
    })
    setChanges(changes)
  }, [openCursor, setChanges])

  const contextValue: LocalRepositoryContextValue = useMemo(() => ({
    changes,
    reload,
    ready,
  }), [changes, ready, reload])

  useEffect(() => {
    if (ready) reload()
  }, [reload, ready])

  return (
    <LocalRepositoryContext.Provider value={contextValue}>
      {children}
    </LocalRepositoryContext.Provider>
  )
}

export const useLocalRepositoryChangeList = () => {
  const { ready, changes } = useContext(LocalRepositoryContext)
  return { ready, changes }
}

// -------------------------------------------------
// 特定の集約の変更

export type LocalRepositoryArgs<T> = {
  dataTypeKey: string
  getItemKey: (t: T) => string
  getItemName?: (t: T) => string
  remoteItems?: T[]
}
export type LocalRepositoryStateAndKeyAndItem<T> = {
  itemKey: ItemKey
  state: LocalRepositoryState
  item: T
}

export const useLocalRepository = <T extends object>({
  dataTypeKey,
  getItemKey,
  getItemName,
  remoteItems,
}: LocalRepositoryArgs<T>) => {

  const { ready, reload: reloadContext } = useContext(LocalRepositoryContext)
  const { openCursor, queryToTable, commandToTable } = useIndexedDbLocalRepositoryTable()

  const loadLocalItems = useCallback(async () => {
    const localItems: LocalRepositoryStateAndKeyAndItem<T>[] = []
    await openCursor('readonly', cursor => {
      if (cursor.value.dataTypeKey !== dataTypeKey) return
      localItems.push({
        state: cursor.value.state,
        itemKey: cursor.value.itemKey,
        item: cursor.value.item as T,
      })
    })
    const recalculated = crossJoin(
      localItems, local => local.itemKey,
      (remoteItems ?? []), remote => getItemKey(remote) as ItemKey,
    ).map<LocalRepositoryStateAndKeyAndItem<T>>(pair => {
      return pair.left ?? { state: '', itemKey: pair.key, item: pair.right }
    })
    return recalculated
  }, [remoteItems, openCursor, getItemKey, dataTypeKey])

  const addToLocalRepository = useCallback(async (item: T): Promise<LocalRepositoryStateAndKeyAndItem<T>> => {
    const itemKey = UUID.generate() as ItemKey
    const itemName = getItemName?.(item) ?? ''
    const state: LocalRepositoryState = '+'
    await queryToTable(table => table.put({ state, dataTypeKey, itemKey, itemName, item }))
    await reloadContext()
    return { itemKey, state, item }
  }, [dataTypeKey, queryToTable, reloadContext, getItemName])

  const updateLocalRepositoryItem = useCallback(async (itemKey: ItemKey, item: T): Promise<LocalRepositoryStateAndKeyAndItem<T>> => {
    const itemName = getItemName?.(item) ?? ''
    const stateBeforeUpdate = (await queryToTable(table => table.get([dataTypeKey, itemKey])))?.state
    const state: LocalRepositoryState = stateBeforeUpdate === '+' || stateBeforeUpdate === '-'
      ? stateBeforeUpdate
      : '*'
    await queryToTable(table => table.put({ dataTypeKey, itemKey, itemName, state, item }))
    await reloadContext()
    return { itemKey, state, item }
  }, [dataTypeKey, queryToTable, reloadContext, getItemName])

  const deleteLocalRepositoryItem = useCallback(async (itemKey: ItemKey, item: T): Promise<LocalRepositoryStateAndKeyAndItem<T> | undefined> => {
    const stored = (await queryToTable(table => table.get([dataTypeKey, itemKey])))
    const existsRemote = remoteItems?.some(x => getItemKey(x) === itemKey)

    if (stored?.state === '-') {
      // 既に削除済みの場合: 何もしない
      const { state, itemKey, item } = stored
      return { state, itemKey, item: item as T }

    } else if (stored?.state === '+') {
      // 新規作成後コミット前の場合: 物理削除
      await queryToTable(table => table.delete([dataTypeKey, itemKey]))
      await reloadContext()
      return undefined

    } else if (stored?.state === '*' || stored?.state === '' || existsRemote) {
      // リモートにある場合: 削除済みにマークする
      const itemName = getItemName?.(item) ?? ''
      const state: LocalRepositoryState = '-'
      await queryToTable(table => table.put({ dataTypeKey, itemKey, itemName, state, item }))
      await reloadContext()
      return { state, itemKey, item }

    } else {
      // ローカルにもリモートにも無い場合: 何もしない
      return undefined
    }
  }, [dataTypeKey, queryToTable, reloadContext, getItemKey, getItemName])

  const commit = useCallback(async (...itemKeys: string[]): Promise<void> => {
    await commandToTable(table => {
      for (const itemKey of itemKeys) table.delete([dataTypeKey, itemKey])
    })
    await reloadContext()
  }, [queryToTable, reloadContext, dataTypeKey])

  const reset = useCallback(async (): Promise<void> => {
    await openCursor('readwrite', cursor => {
      if (cursor.value.dataTypeKey === dataTypeKey) cursor.delete()
    })
    await reloadContext()
  }, [loadLocalItems, openCursor, dataTypeKey, reloadContext])

  return {
    ready,
    loadLocalItems,
    addToLocalRepository,
    updateLocalRepositoryItem,
    deleteLocalRepositoryItem,
    commit,
    reset,
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

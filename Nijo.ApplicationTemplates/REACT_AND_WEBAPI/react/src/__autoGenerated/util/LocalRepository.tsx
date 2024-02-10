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
  itemKey: string
  itemName: string
  serializedItem: string
  state: LocalRepositoryState
}

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
  itemKey: string
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
  serialize: (t: T) => string
  deserialize: (str: string) => T
  remoteItems?: T[]
}
export type LocalRepositoryStateAndKeyAndItem<T> = {
  itemKey: string
  state: LocalRepositoryState
  item: T
}

export const useLocalRepository = <T,>({
  dataTypeKey,
  getItemKey,
  getItemName,
  serialize,
  deserialize,
  remoteItems,
}: LocalRepositoryArgs<T>) => {

  const { ready, reload: reloadContext } = useContext(LocalRepositoryContext)
  const { openCursor, openTable } = useIndexedDbLocalRepositoryTable()
  const [localItems, dispatch] = useReducer<typeof arrayReducer<LocalRepositoryStateAndKeyAndItem<T>>>(arrayReducer, [])

  const recalculateItems = useCallback(async () => {
    const unhandled = new Map<string, LocalRepositoryStoredItem>()
    await openCursor('readonly', cursor => {
      if (cursor.value.dataTypeKey === dataTypeKey) {
        const key = getItemKey(deserialize(cursor.value.serializedItem))
        unhandled.set(key, cursor.value)
      }
    })
    const itemsWithState: LocalRepositoryStateAndKeyAndItem<T>[] = []
    for (const remote of (remoteItems ?? [])) {
      const key = getItemKey(remote)
      const localItem = unhandled.get(key)
      if (localItem) {
        const item = deserialize(localItem.serializedItem)
        itemsWithState.push({ itemKey: localItem.itemKey, item, state: localItem.state })
        unhandled.delete(key)
      } else {
        itemsWithState.push({ itemKey: key, item: remote, state: '' })
      }
    }
    const arrUnhandled = Array.from(unhandled.values()).map<LocalRepositoryStateAndKeyAndItem<T>>(x => ({
      item: deserialize(x.serializedItem),
      itemKey: x.itemKey,
      state: x.state,
    }))
    const recalculated = [...arrUnhandled, ...itemsWithState]
    dispatch(arr => arr.reset(recalculated))
    return recalculated
  }, [remoteItems, openCursor, getItemKey, dataTypeKey, deserialize, dispatch])

  useEffect(() => {
    if (ready) recalculateItems()
  }, [ready, recalculateItems])

  const addToLocalRepository = useCallback(async (item: T): Promise<LocalRepositoryStateAndKeyAndItem<T>> => {
    const itemKey = UUID.generate()
    const itemName = getItemName?.(item) ?? ''
    const serializedItem = serialize(item)
    const state: LocalRepositoryState = '+'
    dispatch(arr => arr.insert({ item, itemKey, state }))
    await openTable(table => table.put({ state, dataTypeKey, itemKey, itemName, serializedItem }))
    await reloadContext()
    return { itemKey, state, item }
  }, [dataTypeKey, openTable, reloadContext, getItemName, serialize, dispatch])

  const updateLocalRepositoryItem = useCallback(async (itemKey: string, item: T): Promise<LocalRepositoryStateAndKeyAndItem<T>> => {
    const serializedItem = serialize(item)
    const itemName = getItemName?.(item) ?? ''
    const stateBeforeUpdate = (await openTable(table => table.get([dataTypeKey, itemKey])))?.state
    const state: LocalRepositoryState = stateBeforeUpdate === '+' || stateBeforeUpdate === '-'
      ? stateBeforeUpdate
      : '*'
    dispatch(arr => arr.upsert(x => getItemKey(x.item), { item, itemKey, state }))
    await openTable(table => table.put({ dataTypeKey, itemKey, itemName, serializedItem, state }))
    await reloadContext()
    return { itemKey, state, item }
  }, [dataTypeKey, getItemKey, openTable, reloadContext, serialize, getItemName, dispatch])

  const deleteLocalRepositoryItem = useCallback(async (dbKey: string, item: T): Promise<{ remains: boolean }> => {
    const localState = (await openTable(table => table.get([dataTypeKey, dbKey])))?.state
    const itemKey = getItemKey(item)
    const existsRemote = remoteItems?.some(x => getItemKey(x) === itemKey)

    if (localState === '-') {
      // 既に削除済みの場合: 何もしない
      return { remains: true }

    } else if (localState === '+') {
      // 新規作成後コミット前の場合: 物理削除
      dispatch(arr => arr.delete(x => x.itemKey === dbKey))
      await openTable(table => table.delete([dataTypeKey, dbKey]))
      await reloadContext()
      return { remains: false }

    } else if (localState === '*' || localState === '' || existsRemote) {
      // リモートにある場合: 削除済みにマークする
      const serializedItem = serialize(item)
      const itemName = getItemName?.(item) ?? ''
      const state: LocalRepositoryState = '-'
      dispatch(arr => arr.upsert(x => getItemKey(x.item), { item, itemKey: dbKey, state }))
      await openTable(table => table.put({ dataTypeKey, itemKey: dbKey, itemName, serializedItem, state }))
      await reloadContext()
      return { remains: true }

    } else {
      // ローカルにもリモートにも無い場合: 何もしない
      return { remains: true }
    }
  }, [dataTypeKey, openTable, reloadContext, serialize, getItemKey, getItemName, dispatch])

  const commit = useCallback(async (...itemKeys: string[]): Promise<void> => {
    for (const itemKey of itemKeys) {
      await openTable(table => table.delete([dataTypeKey, itemKey]))
    }
    await recalculateItems()
    await reloadContext()
  }, [recalculateItems, openTable, reloadContext, dataTypeKey])

  const reset = useCallback(async (): Promise<void> => {
    await openCursor('readwrite', cursor => {
      if (cursor.value.dataTypeKey === dataTypeKey) cursor.delete()
    })
    await recalculateItems()
    await reloadContext()
  }, [recalculateItems, openCursor, dataTypeKey, reloadContext])

  return {
    ready,
    localItems,
    addToLocalRepository,
    updateLocalRepositoryItem,
    deleteLocalRepositoryItem,
    commit,
    reset,
    reload: recalculateItems,
  }
}

const arrayReducer = ReactUtil.defineReducer(<T,>(state: T[]) => ({
  reset: (arr: T[]) => arr,
  delete: (where: (t: T) => boolean) => state.filter(t => !where(t)),
  insert: (t: T, index?: number) => {
    const arr2 = [...state]
    arr2.splice(index ?? 0, 0, t)
    return arr2
  },
  upsert: <TKey,>(getKey: (item: T) => TKey, newItem: T) => {
    const key = getKey(newItem)
    const index = state.findIndex(x => getKey(x) === key)
    if (index === -1) {
      return [newItem, ...state]
    } else {
      const arr2 = [...state]
      arr2.splice(index, 1, newItem)
      return arr2
    }
  },
}))

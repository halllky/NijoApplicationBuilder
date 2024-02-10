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
  const [localItems, setLocalItems] = useState<LocalRepositoryStateAndKeyAndItem<T>[]>(() => [])

  const recalculateItems = useCallback(async () => {
    const unhandled = new Map<string, LocalRepositoryStoredItem>()
    await openCursor('readonly', cursor => {
      if (cursor.value.dataTypeKey === dataTypeKey) {
        const key = getItemKey(deserialize(cursor.value.serializedItem))
        unhandled.set(key, cursor.value)
      }
    })
    const itemsWithState: LocalRepositoryStateAndKeyAndItem<T>[] = []
    if (remoteItems) {
      for (const remote of remoteItems) {
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
    }
    const arrUnhandled = Array.from(unhandled.values()).map<LocalRepositoryStateAndKeyAndItem<T>>(x => ({
      item: deserialize(x.serializedItem),
      itemKey: x.itemKey,
      state: x.state,
    }))
    setLocalItems([...arrUnhandled, ...itemsWithState])
  }, [remoteItems, openCursor, getItemKey, dataTypeKey, deserialize])

  useEffect(() => {
    if (ready) recalculateItems()
  }, [ready, recalculateItems])

  const loadAll = useCallback(async (): Promise<LocalRepositoryStateAndKeyAndItem<T>[]> => {
    const arr: LocalRepositoryStateAndKeyAndItem<T>[] = []
    await openCursor('readonly', cursor => {
      if (cursor.value.dataTypeKey === dataTypeKey) arr.push({
        item: deserialize(cursor.value.serializedItem),
        itemKey: cursor.value.itemKey,
        state: cursor.value.state,
      })
    })
    return arr
  }, [dataTypeKey, deserialize, openCursor, getItemKey])

  const withLocalReposState = useCallback(async (remoteItems: T[], includesLocalReposOnly: boolean = true): Promise<LocalRepositoryStateAndKeyAndItem<T>[]> => {
    const decorated: LocalRepositoryStateAndKeyAndItem<T>[] = []
    const unhandled = new Map<string, LocalRepositoryStoredItem>()
    await openCursor('readonly', cursor => {
      if (cursor.value.dataTypeKey === dataTypeKey) {
        const key = getItemKey(deserialize(cursor.value.serializedItem))
        unhandled.set(key, cursor.value)
      }
    })
    for (const remote of remoteItems) {
      const key = getItemKey(remote)
      const localItem = unhandled.get(key)
      if (localItem) {
        const item = deserialize(localItem.serializedItem)
        decorated.push({ itemKey: localItem.itemKey, item, state: localItem.state })
        unhandled.delete(key)
      } else {
        decorated.push({ itemKey: key, item: remote, state: '' })
      }
    }
    if (includesLocalReposOnly) {
      decorated.unshift(...Array.from(unhandled.values()).map<LocalRepositoryStateAndKeyAndItem<T>>(x => ({
        item: deserialize(x.serializedItem),
        itemKey: x.itemKey,
        state: x.state,
      })))
    }
    return decorated
  }, [openCursor, getItemKey, dataTypeKey, deserialize])

  const addToLocalRepository = useCallback(async (item: T): Promise<LocalRepositoryStateAndKeyAndItem<T>> => {
    const itemKey = UUID.generate()
    const itemName = getItemName?.(item) ?? ''
    const serializedItem = serialize(item)
    const state: LocalRepositoryState = '+'
    setLocalItems([{ item, itemKey, state }, ...localItems])
    await openTable(table => table.put({ state, dataTypeKey, itemKey, itemName, serializedItem }))
    await reloadContext()
    return { itemKey, state, item }
  }, [localItems, dataTypeKey, openTable, reloadContext, getItemName, serialize])

  const updateLocalRepositoryItem = useCallback(async (itemKey: string, item: T): Promise<LocalRepositoryStateAndKeyAndItem<T>> => {
    const serializedItem = serialize(item)
    const itemName = getItemName?.(item) ?? ''
    const stateBeforeUpdate = (await openTable(table => table.get([dataTypeKey, itemKey])))?.state
    const state: LocalRepositoryState = stateBeforeUpdate === '+' || stateBeforeUpdate === '-'
      ? stateBeforeUpdate
      : '*'
    setLocalItems(replaceOrUnshift(localItems, x => getItemKey(x.item), { item, itemKey, state }))
    await openTable(table => table.put({ dataTypeKey, itemKey, itemName, serializedItem, state }))
    await reloadContext()
    return { itemKey, state, item }
  }, [localItems, dataTypeKey, getItemKey, openTable, reloadContext, serialize, getItemName])

  const deleteLocalRepositoryItem = useCallback(async (itemKey: string, item: T): Promise<{ remains: boolean }> => {
    const stateBeforeUpdate = (await openTable(table => table.get([dataTypeKey, itemKey])))?.state
    if (stateBeforeUpdate === '-') {
      return { remains: true }

    } else if (stateBeforeUpdate === '+') {
      setLocalItems(localItems.filter(x => x.itemKey !== itemKey))
      await openTable(table => table.delete([dataTypeKey, itemKey]))
      await reloadContext()
      return { remains: false }

    } else {
      const serializedItem = serialize(item)
      const itemName = getItemName?.(item) ?? ''
      const state: LocalRepositoryState = '-'
      setLocalItems(replaceOrUnshift(localItems, x => getItemKey(x.item), { item, itemKey, state }))
      await openTable(table => table.put({ dataTypeKey, itemKey, itemName, serializedItem, state }))
      await reloadContext()
      return { remains: true }
    }
  }, [localItems, dataTypeKey, openTable, reloadContext, serialize, getItemKey, getItemName])

  const commit = useCallback(async (itemKey: string): Promise<void> => {
    await openTable(table => table.delete([dataTypeKey, itemKey]))
    await reloadContext()
  }, [openTable, reloadContext, dataTypeKey])

  const reset = useCallback(async (): Promise<void> => {
    await openCursor('readwrite', cursor => {
      if (cursor.value.dataTypeKey === dataTypeKey) cursor.delete()
    })
    await reloadContext()
  }, [openCursor, dataTypeKey, reloadContext])

  return {
    ready,
    localItems,
    loadAll,
    withLocalReposState,
    addToLocalRepository,
    updateLocalRepositoryItem,
    deleteLocalRepositoryItem,
    commit,
    reset,
  }
}

const replaceOrUnshift = <TItem, TKey>(arr: TItem[], getKey: (t: TItem) => TKey, item: TItem): TItem[] => {
  const key = getKey(item)
  const index = arr.findIndex(x => getKey(x) === key)
  if (index === -1) {
    return [item, ...arr]
  } else {
    const arr2 = [...arr]
    arr2.splice(index, 1, item)
    return arr2
  }
}

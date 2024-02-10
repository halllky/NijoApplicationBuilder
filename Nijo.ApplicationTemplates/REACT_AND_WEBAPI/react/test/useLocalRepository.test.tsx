import React from 'react'
import { expect, test } from 'vitest'
import { setup as setupIndexedDB } from 'vitest-indexeddb'
import { renderHook, act, waitFor } from '@testing-library/react'
import { useIndexedDbTable } from '../src/__autoGenerated/util'
import {
  useLocalRepository,
  LocalRepositoryStateAndKeyAndItem,
  LocalRepositoryState,
  LocalRepositoryContextProvider,
} from '../src/__autoGenerated/util/useBatchUpdate'


test('useLocalRepository 状態遷移テスト（排他に引っかかるパターンなし網羅）', async () => {
  setupIndexedDB()
  const { Remote, scope, Debug } = setupLocalRepositoryHook()

  // ----------------------------------------
  expect(await current(Remote)).toEqual<TestData[]>([
  ])

  // 画面表示 1回目
  await scope(async Local => {
    expect(await current(Local)).toEqual<LocalState[]>([
    ])

    let a = await add(Local, 'a')
    let b = await add(Local, 'b')
    let x = await add(Local, 'x')
    let y = await add(Local, 'y')
    expect(await current(Local)).toEqual<LocalState[]>([
      ['+', { key: 'a', name: '', version: 0 }],
      ['+', { key: 'b', name: '', version: 0 }],
      ['+', { key: 'x', name: '', version: 0 }],
      ['+', { key: 'y', name: '', version: 0 }],
    ])

    a = await edit(Local, a, 'あ')
    a = await edit(Local, a, 'い')
    expect(await current(Local)).toEqual<LocalState[]>([
      ['+', { key: 'a', name: 'い', version: 0 }],
      ['+', { key: 'b', name: '', version: 0 }],
      ['+', { key: 'x', name: '', version: 0 }],
      ['+', { key: 'y', name: '', version: 0 }],
    ])

    await remove(Local, x)
    await remove(Local, x)
    expect(await current(Local)).toEqual<LocalState[]>([
      ['+', { key: 'a', name: 'い', version: 0 }],
      ['+', { key: 'b', name: '', version: 0 }],
      ['+', { key: 'y', name: '', version: 0 }],
    ])
  })

  // 他者がリモートリポジトリのデータを更新
  expect(await current(Remote)).toEqual<TestData[]>([
  ])
  await add(Remote, 'z')
  expect(await current(Remote)).toEqual<TestData[]>([
    { key: 'z', name: '', version: 0 },
  ])

  // 画面表示 2回目
  await scope(async Local => {
    expect(await current(Local)).toEqual<LocalState[]>([
      ['+', { key: 'a', name: 'い', version: 0 }],
      ['+', { key: 'b', name: '', version: 0 }],
      ['+', { key: 'y', name: '', version: 0 }],
    ])

    let a = (await get(Remote, Local, 'a'))!
    let y = (await get(Remote, Local, 'y'))!
    await edit(Local, a, 'う')
    await edit(Local, a, 'え')
    await remove(Local, y)
    await remove(Local, y)
    await add(Local, 'c')
    await add(Local, 'd')
    expect(await current(Local)).toEqual<LocalState[]>([
      ['+', { key: 'a', name: 'え', version: 0 }],
      ['+', { key: 'b', name: '', version: 0 }],
      ['+', { key: 'c', name: '', version: 0 }],
      ['+', { key: 'd', name: '', version: 0 }],
    ])

    await save(Remote, Local)
    expect(await current(Local)).toEqual<LocalState[]>([
    ])

    await save(Remote, Local)
    expect(await current(Local)).toEqual<LocalState[]>([
    ])
  })

  expect(await current(Remote)).toEqual<TestData[]>([
    { key: 'a', name: 'え', version: 0 },
    { key: 'b', name: '', version: 0 },
    { key: 'c', name: '', version: 0 },
    { key: 'd', name: '', version: 0 },
    { key: 'z', name: '', version: 0 },
  ])

  // 画面表示 3回目
  await scope(async Local => {
    expect(await current(Local)).toEqual<LocalState[]>([
    ])

    let a = (await get(Remote, Local, 'a'))!
    let b = (await get(Remote, Local, 'b'))!
    let c = (await get(Remote, Local, 'c'))!
    let d = (await get(Remote, Local, 'd'))!
    a = await edit(Local, a, 'お')
    a = await edit(Local, a, 'か')
    b = await edit(Local, b, 'ぱ')
    b = await edit(Local, b, 'ぴ')
    await remove(Local, c)
    await remove(Local, c)
    await remove(Local, d)
    await remove(Local, d)
    expect(await current(Local)).toEqual<LocalState[]>([
      ['*', { key: 'a', name: 'か', version: 0 }],
      ['*', { key: 'b', name: 'ぴ', version: 0 }],
      ['-', { key: 'c', name: '', version: 0 }],
      ['-', { key: 'd', name: '', version: 0 }],
    ])
  })

  // 画面表示 4回目
  await scope(async Local => {
    await save(Remote, Local)
    await save(Remote, Local)

    expect(await current(Local)).toEqual<LocalState[]>([
    ])
  })

  expect(await current(Remote)).toEqual<TestData[]>([
    { key: 'a', name: 'か', version: 1 },
    { key: 'b', name: 'ぴ', version: 1 },
    { key: 'z', name: '', version: 0 },
  ])
})


// ----------------------------------------
// テストコードを読みやすくするためのヘルパー関数
type TestData = {
  key?: string
  name?: string
  numValue?: number
  version: number
}
type LocalState = [LocalRepositoryState, TestData]
type TestLocalRepos = ReturnType<typeof useLocalRepository<TestData>>
type TestRemoteRepos = Map<string, TestData>

const setupLocalRepositoryHook = () => {
  /** リモートリポジトリ */
  const Remote = new Map<string, TestData>()

  /** 画面表示と対応するスコープ */
  const scope = async (fn: (Local: TestLocalRepos) => Promise<void>): Promise<void> => {
    const wrapper = ({ children }: { children?: React.ReactNode }) => {
      return (
        <LocalRepositoryContextProvider>
          {children}
        </LocalRepositoryContextProvider>
      )
    }

    const { result, unmount } = renderHook(() => {
      return useLocalRepository<TestData>({
        dataTypeKey: 'TEST-DATA-20240204',
        serialize: data => JSON.stringify(data),
        deserialize: str => JSON.parse(str),
        getItemKey: data => data.key ?? '',
        getItemName: data => data.name ?? '',
      })
    }, { wrapper })

    // 内部でuseEffectを使っているので初期化完了まで待つ
    await waitFor(() => expect(result.current.ready).toBe(true))

    await fn(result.current)
    unmount()
  }

  // 不具合調査用
  const { result: Debug } = renderHook(() => useIndexedDbTable<{
    dataTypeKey: string
    itemKey: string
    itemName: string
    serializedItem: string
    state: LocalRepositoryState
  }>({
    dbName: '::nijo::',
    dbVersion: 1,
    tableName: 'LocalRepository',
    keyPath: ['dataTypeKey', 'itemKey'],
  }))

  return { Remote, scope, Debug }
}

const isLocal = (localOrRemote: TestLocalRepos | TestRemoteRepos): localOrRemote is TestLocalRepos => {
  return typeof (localOrRemote as TestLocalRepos).addToLocalRepository === 'function'
}
async function current(local: TestLocalRepos): Promise<LocalState[]>
async function current(remote: TestRemoteRepos): Promise<TestData[]>
async function current(localOrRemote: TestLocalRepos | TestRemoteRepos): Promise<(LocalState[] | TestData[])> {
  if (isLocal(localOrRemote)) {
    return await act(async () => {
      const data = await localOrRemote.loadAll()
      const localState: LocalState[] = data
        .sort((a, b) => {
          if ((a.item.key ?? '') < (b.item.key ?? '')) return -1
          if ((a.item.key ?? '') > (b.item.key ?? '')) return 1
          return 0
        })
        .map(x => [x.state, x.item] as const)
      return localState
    })
  } else {
    return Array
      .from(localOrRemote.values())
      .sort((a, b) => {
        if ((a.key ?? '') < (b.key ?? '')) return -1
        if ((a.key ?? '') > (b.key ?? '')) return 1
        return 0
      })
  }
}
async function add(local: TestLocalRepos, key: string): Promise<LocalRepositoryStateAndKeyAndItem<TestData>>
async function add(remote: TestRemoteRepos, key: string): Promise<void>
async function add(localOrRemote: TestLocalRepos | TestRemoteRepos, key: string): Promise<LocalRepositoryStateAndKeyAndItem<TestData> | void> {
  const data: TestData = { key, name: '', version: 0 }
  if (isLocal(localOrRemote)) {
    return await act(async () => {
      return await localOrRemote.addToLocalRepository(data)
    })
  } else {
    if (localOrRemote.has(key)) throw new Error(`キー重複: ${key}`)
    localOrRemote.set(key, data)
  }
}
async function edit(local: TestLocalRepos, item: LocalRepositoryStateAndKeyAndItem<TestData>, name: string): Promise<LocalRepositoryStateAndKeyAndItem<TestData>> {
  return await act(async () => {
    if (!item.itemKey) throw new Error('Key is nothing.')
    return await local.updateLocalRepositoryItem(item.itemKey, { ...item.item, name })
  })
}
async function remove(local: TestLocalRepos, item: LocalRepositoryStateAndKeyAndItem<TestData>): Promise<void> {
  await act(async () => {
    await local.deleteLocalRepositoryItem(item.itemKey, item.item)
  })
}
async function get(remote: TestRemoteRepos, local: TestLocalRepos, key: string): Promise<LocalRepositoryStateAndKeyAndItem<TestData> | undefined> {
  return await act(async () => {
    const inRemote = remote.get(key)
    if (inRemote) {
      const withState = (await local.withLocalReposState([inRemote], false))
      return withState[0]
    } else {
      const withState = (await local.withLocalReposState([], true))
      return withState.find(x => x.item.key === key)
    }
  })
}
async function save(remote: TestRemoteRepos, local: TestLocalRepos): Promise<void> {
  await act(async () => {
    const allLocalItems = await local.loadAll()
    for (const localItem of allLocalItems) {
      if (localItem.state === '+') {
        if (!localItem.item.key) { console.error(`キーなし: ${localItem.item.name}`); continue }
        if (remote.has(localItem.item.key)) { console.error(`キー重複: ${localItem.item.key}`); continue }
        remote.set(localItem.item.key, localItem.item)
        await local.commit(localItem.itemKey)

      } else if (localItem.state === '*') {
        if (!localItem.item.key) { console.error(`キーなし: ${localItem.item.name}`); continue }
        if (!remote.has(localItem.item.key)) { console.error(`更新対象なし: ${localItem.item.key}`); continue }
        remote.set(localItem.item.key, { ...localItem.item, version: localItem.item.version + 1 })
        await local.commit(localItem.itemKey)

      } else if (localItem.state === '-') {
        if (!localItem.item.key) { console.error(`キーなし: ${localItem.item.name}`); continue }
        if (!remote.has(localItem.item.key)) { console.error(`更新対象なし: ${localItem.item.key}`); continue }
        remote.delete(localItem.item.key)
        await local.commit(localItem.itemKey)
      }
    }
  })
}

import React, { useCallback, useEffect, useMemo, useState } from 'react'
import cytoscape from 'cytoscape'
import ExpandCollapse from './GraphView.ExpandCollapse'
import { Toolbar } from './GraphView.ToolBar'
import Navigator from './GraphView.Navigator'
import Layout from './GraphView.Layout'
// import enumerateData from './data'
import { Components, StorageUtil } from './util'
import { useNeo4jQueryRunner } from './GraphView.Neo4j'
import * as UUID from 'uuid'
import { Route, useParams } from 'react-router-dom'
import * as SideMenu from './appSideMenu'

Layout.configure(cytoscape)
Navigator.configure(cytoscape)
ExpandCollapse.configure(cytoscape)

// ------------------------------------

const usePages: SideMenu.UsePagesHook = () => {
  const { data: storedQueries } = StorageUtil.useLocalStorage(queryStorageHandler)
  const menuItems = useMemo((): SideMenu.SideMenuSection[] => [{
    url: '/',
    itemId: 'APP::HOME',
    label: 'ホーム',
    order: 0,
    children: storedQueries.map<SideMenu.SideMenuSectionItem>(query => ({
      url: `/${query.queryId}`,
      itemId: `STOREDQUERY::${query.queryId}`,
      label: query.name,
    })),
  }], [storedQueries])

  const Routes = useCallback((): React.ReactNode => <>
    <Route path="/" element={<Page />} />
    <Route path="/:queryId" element={<Page />} />
  </>, [])
  return { menuItems, Routes }
}

// ------------------------------------
const Page = () => {

  // query editing
  const { queryId } = useParams()
  const { data: storedQueries, save } = StorageUtil.useLocalStorage(queryStorageHandler)
  const [displayedQuery, setDisplayedQuery] = useState(() => createNewQuery())
  const handleQueryNameEdit: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    setDisplayedQuery({ ...displayedQuery, name: e.target.value })
  }, [displayedQuery])
  const handleQueryStringEdit: React.ChangeEventHandler<HTMLTextAreaElement> = useCallback(e => {
    setDisplayedQuery({ ...displayedQuery, queryString: e.target.value })
  }, [displayedQuery])
  const handleQuerySaving = useCallback(() => {
    // 保存のたびに新規採番する
    save([...storedQueries, { ...displayedQuery, queryId: UUID.v4() }])
  }, [displayedQuery, storedQueries])

  // Neo4j
  const { runQuery, queryResult, nowLoading } = useNeo4jQueryRunner()
  useEffect(() => {
    // 画面表示時、保存されているクエリ定義を取得しクエリ実行
    if (!queryId) return
    const loaded = storedQueries.find(q => q.queryId === queryId)
    if (!loaded) return
    setDisplayedQuery(loaded)
    runQuery(loaded.queryString)
  }, [queryId, storedQueries, runQuery])
  const handleQueryRerun = useCallback(() => {
    if (nowLoading) return
    runQuery(displayedQuery.queryString)
  }, [displayedQuery.queryString, nowLoading])

  // Cytoscape
  const [cy, setCy] = useState<cytoscape.Core>()
  const [initialized, setInitialized] = useState(false)
  const divRef = useCallback((divElement: HTMLDivElement | null) => {
    if (!divElement) return
    const cyInstance = cytoscape({
      container: divElement,
      elements: queryResult,
      style: STYLESHEET,
      layout: Layout.DEFAULT,
    })
    Navigator.setupCyInstance(cyInstance)
    ExpandCollapse.setupCyInstance(cyInstance)
    setCy(cyInstance)

    if (!initialized) {
      cyInstance.resize().fit().reset()
      setInitialized(true)
    }
  }, [queryResult, initialized])

  return (
    <div className="flex flex-col relative">
      <Components.Text value={displayedQuery.name} onChange={handleQueryNameEdit} />
      <Components.Textarea value={displayedQuery.queryString} onChange={handleQueryStringEdit} />
      <div className="flex gap-2 justify-end">
        <Components.Button onClick={handleQueryRerun}>
          {nowLoading ? '読込中...' : '読込'}
        </Components.Button>
        <Components.Button onClick={handleQuerySaving}>
          お気に入り登録
        </Components.Button>
      </div>
      <Components.Separator />
      <Toolbar cy={cy} className="mb-1" />
      <div ref={divRef} className="
        overflow-hidden [&>div>canvas]:left-0
        flex-1
        border border-1 border-slate-400">
      </div>
      <Navigator.Component className="absolute w-1/4 h-1/4 right-6 bottom-6 z-[200]" />
    </div>
  )
}

const STYLESHEET: cytoscape.CytoscapeOptions['style'] = [{
  selector: 'node',
  css: {
    'shape': 'round-rectangle',
    'width': (node: any) => node.data('label')?.length * 10,
    'text-valign': 'center',
    'text-halign': 'center',
    'border-width': '1px',
    'border-color': '#909090',
    'background-color': '#666666',
    'background-opacity': .1,
    'label': 'data(label)',
  },
}, {
  selector: 'node:parent', // 子要素をもつノードに適用される
  css: {
    'text-valign': 'top',
    'color': '#707070',
  },
}, {
  selector: 'edge',
  style: {
    'target-arrow-shape': 'triangle',
    'curve-style': 'bezier',
  },
}, {
  selector: 'edge:selected',
  style: {
    'label': 'data(label)',
    'color': 'blue',
  },
}]

// ------------------------------------------------------
export type Query = {
  queryId: string
  name: string
  queryString: string
}
const createNewQuery = (): Query => ({
  queryId: UUID.v4(),
  name: '',
  queryString: '',
})

const queryStorageHandler: StorageUtil.LocalStorageHandler<Query[]> = {
  storageKey: 'HALDIAGRAM::QUERIES',
  serialize: obj => {
    return JSON.stringify(obj)
  },
  deserialize: str => {
    try {
      const parsed: Partial<Query>[] = JSON.parse(str)
      if (!Array.isArray(parsed)) return { ok: false }
      const obj = parsed.map<Query>(item => ({
        queryId: item.queryId ?? '',
        name: item.name ?? '',
        queryString: item.queryString ?? '',
      }))
      return { ok: true, obj }
    } catch (error) {
      console.error(`Failure to load application settings.`, error)
      return { ok: false }
    }
  },
  defaultValue: () => [],
}

export default {
  usePages,
  Page,
  createNewQuery,
}

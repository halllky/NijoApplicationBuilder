import React, { useCallback, useEffect, useMemo, useState } from 'react'
import { Route, useNavigate, useParams } from 'react-router-dom'
import cytoscape from 'cytoscape'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import * as UUID from 'uuid'
import * as Icon from '@ant-design/icons'
import ExpandCollapse from './GraphView.ExpandCollapse'
import Navigator from './GraphView.Navigator'
import Layout from './GraphView.Layout'
// import enumerateData from './data'
import { Components, ErrorHandling, StorageUtil } from './util'
import { useNeo4jQueryRunner } from './GraphView.Neo4j'
import * as SideMenu from './appSideMenu'

Layout.configure(cytoscape)
Navigator.configure(cytoscape)
ExpandCollapse.configure(cytoscape)

// ------------------------------------

const usePages: SideMenu.UsePagesHook = () => {
  const navigate = useNavigate()
  const { data: storedQueries, save } = StorageUtil.useLocalStorage(queryStorageHandler)
  const [, dispatch] = ErrorHandling.useMsgContext()
  const deleteItem = useCallback((query: Query) => {
    if (!confirm(`${query.name}を削除します。よろしいですか？`)) return
    save(storedQueries.filter(q => q.queryId !== query.queryId))
    navigate('/')
  }, [storedQueries, save, navigate])
  const renameItem = useCallback((query: Query, newName: string) => {
    const updated = storedQueries.find(q => q.queryId === query.queryId)
    if (!updated) { dispatch(state => state.add('error', `Rename item '${query.name}' not found.`)); return }
    updated.name = newName
    save([...storedQueries])
  }, [storedQueries, save])

  const menuItems = useMemo((): SideMenu.SideMenuSection[] => [{
    url: '/',
    itemId: 'APP::HOME',
    label: 'ホーム',
    order: 0,
    children: storedQueries.map<SideMenu.SideMenuSectionItem>(query => ({
      url: `/${query.queryId}`,
      itemId: `STOREDQUERY::${query.queryId}`,
      label: query.name,
      onRename: newName => renameItem(query, newName),
      actions: [{ icon: Icon.DeleteOutlined, actionName: '削除', onClick: () => deleteItem(query) }]
    })),
  }], [storedQueries, deleteItem])

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
  const navigate = useNavigate()
  const handleQueryStringEdit: React.ChangeEventHandler<HTMLTextAreaElement> = useCallback(e => {
    setDisplayedQuery({ ...displayedQuery, queryString: e.target.value })
  }, [displayedQuery])
  const handleQuerySaving = useCallback(() => {
    const index = storedQueries.findIndex(q => q.queryId === displayedQuery.queryId)
    if (index === -1) {
      save([...storedQueries, displayedQuery])
      navigate(`/${displayedQuery.queryId}`)
    } else {
      storedQueries.splice(index, 1, displayedQuery)
      save([...storedQueries])
    }
  }, [displayedQuery, storedQueries])

  // Neo4j
  const { runQuery, clear, queryResult, nowLoading } = useNeo4jQueryRunner()
  useEffect(() => {
    // 画面表示時、保存されているクエリ定義を取得しクエリ実行
    if (queryId) {
      const loaded = storedQueries.find(q => q.queryId === queryId)
      if (!loaded) return
      setDisplayedQuery(loaded)
      runQuery(loaded.queryString)
    } else {
      setDisplayedQuery(createNewQuery())
      clear()
    }
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

  // サイドメニューやデータソース欄の表示/非表示
  const [{ showSideMenu }, dispatchSideMenu] = SideMenu.useSideMenuContext()
  const [showDataSource, setShowDataSource] = useState(true)

  // ノード位置固定
  const [locked, setLocked] = useState(false)
  const handleLockChanged: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    if (!cy) { setLocked(false); return }
    setLocked(e.target.checked)
    cy.autolock(e.target.checked)
  }, [cy, locked])

  // 自動レイアウト
  const [currentLayout, setCurrentLayout] = useState(Layout.DEFAULT.name)
  const handleLayoutChanged: React.ChangeEventHandler<HTMLSelectElement> = useCallback(e => {
    setCurrentLayout(e.target.value)
  }, [])
  const handlePositionReset = useCallback(() => {
    if (!cy) return
    cy.layout(Layout.OPTION_LIST[currentLayout])?.run()
    cy.resize().fit().reset()
  }, [cy, currentLayout])

  // ノードの折りたたみ/展開
  const handleExpandAll = useCallback(() => {
    const api = (cy as any)?.expandCollapse('get')
    api.expandAll()
    api.expandAllEdges()
  }, [cy])
  const handleCollapseAll = useCallback(() => {
    const api = (cy as any)?.expandCollapse('get')
    api.collapseAll()
    api.collapseAllEdges()
  }, [cy])

  return (
    <PanelGroup direction="vertical" className="flex flex-col relative">

      {/* ツールバー */}
      <div className="flex content-start items-center gap-2 mb-2">
        <Components.Button
          icon={showSideMenu ? Icon.LeftOutlined : Icon.RightOutlined}
          onClick={() => dispatchSideMenu(state => state.toggleSideMenu())}
        >メニュー</Components.Button>
        <select className="border border-1 border-zinc-400" value={currentLayout} onChange={handleLayoutChanged}>
          {Object.entries(Layout.OPTION_LIST).map(([key]) => (
            <option key={key} value={key}>
              {key}
            </option>
          ))}
        </select>
        <Components.Button onClick={handlePositionReset}>
          自動レイアウト
        </Components.Button>
        <label className="text-nowrap">
          <input type="checkbox" checked={locked} onChange={handleLockChanged} />
          ノード位置固定
        </label>
        <Components.Button onClick={handleExpandAll}>
          すべて展開
        </Components.Button>
        <Components.Button onClick={handleCollapseAll}>
          すべて折りたたむ
        </Components.Button>

        <div className="flex-1"></div>

        <Components.Button onClick={() => setShowDataSource(!showDataSource)} icon={showDataSource ? Icon.UpOutlined : Icon.DownOutlined} />
        <span className="text-nowrap">
        データソース:Neo4j
        </span>
        <Components.Button onClick={handleQueryRerun} icon={Icon.ReloadOutlined}>
          {nowLoading ? '読込中...' : '再読込'}
        </Components.Button>
        <Components.Button onClick={handleQuerySaving}>
          保存
        </Components.Button>
      </div>

      {/* データソース */}
      <Panel defaultSize={16} className={`flex flex-col ${!showDataSource && 'hidden'}`}>
        <Components.Textarea
          value={displayedQuery.queryString}
          onChange={handleQueryStringEdit}
          className="flex-1 font-mono"
          inputClassName="resize-none"
        />
      </Panel>

      {showDataSource && <PanelResizeHandle className="h-2" />}

      {/* グラフ */}
      <Panel className="flex flex-col bg-white">
        <div ref={divRef} className="
          overflow-hidden [&>div>canvas]:left-0
          flex-1
          border border-1 border-zinc-400">
        </div>
        <Navigator.Component className="absolute w-1/4 h-1/4 right-2 bottom-2 z-[200]" />
      </Panel>
    </PanelGroup>
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

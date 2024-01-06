import React, { useCallback, useEffect, useMemo, useState } from 'react'
import { Route, useNavigate, useParams } from 'react-router-dom'
import cytoscape from 'cytoscape'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import * as Icon from '@ant-design/icons'
import ExpandCollapse from './GraphView.ExpandCollapse'
import Navigator from './GraphView.Navigator'
import Layout from './GraphView.Layout'
// import enumerateData from './data'
import { Components, Messaging } from './util'
import ViewState, { Query, createNewQuery, useQueryRepository } from './GraphView.Query'
import { useNeo4jQueryRunner } from './GraphView.Neo4j'
import GraphDataSource from './GraphView.DataSource'
import * as SideMenu from './appSideMenu'

Layout.configure(cytoscape)
Navigator.configure(cytoscape)
ExpandCollapse.configure(cytoscape)

// ------------------------------------

const usePages: SideMenu.UsePagesHook = () => {
  const navigate = useNavigate()
  const { storedQueries, saveQueries } = useQueryRepository()
  const [, dispatchMessage] = Messaging.useMsgContext()
  const deleteItem = useCallback((query: Query) => {
    if (!confirm(`${query.name}を削除します。よろしいですか？`)) return
    saveQueries(storedQueries.filter(q => q.queryId !== query.queryId))
    navigate('/')
  }, [storedQueries, saveQueries, navigate])
  const renameItem = useCallback((query: Query, newName: string) => {
    const updated = storedQueries.find(q => q.queryId === query.queryId)
    if (!updated) { dispatchMessage(state => state.push('error', `Rename item '${query.name}' not found.`)); return }
    updated.name = newName
    saveQueries([...storedQueries])
  }, [storedQueries, saveQueries])

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
  const { queryId } = useParams()
  const [, dispatchMessage] = Messaging.useMsgContext()

  // Cytoscape
  const [cy, setCy] = useState<cytoscape.Core>()
  const [navInstance, setNavInstance] = useState<{ destroy: () => void }>()
  const divRef = useCallback((divElement: HTMLDivElement | null) => {
    if (!cy && divElement) {
      // 初期化
      const cyInstance = cytoscape({
        container: divElement,
        elements: [],
        style: STYLESHEET,
        layout: Layout.DEFAULT,
      })
      setNavInstance(Navigator.setupCyInstance(cyInstance))
      ExpandCollapse.setupCyInstance(cyInstance)
      setCy(cyInstance)

    } else if (cy && !divElement) {
      // 破棄
      navInstance?.destroy()
    }
  }, [cy, navInstance])

  // query editing
  const { storedQueries, saveQueries } = useQueryRepository()
  const [displayedQuery, setDisplayedQuery] = useState(() => createNewQuery())
  const navigate = useNavigate()
  const handleQueryStringEdit: React.ChangeEventHandler<HTMLTextAreaElement> = useCallback(e => {
    setDisplayedQuery({ ...displayedQuery, queryString: e.target.value })
  }, [displayedQuery])
  const handleQuerySaving = useCallback(() => {
    const saveItem1 = cy ? ViewState.getViewState(displayedQuery, cy) : displayedQuery
    const saveItem = cy ? ExpandCollapse.getViewState(saveItem1, cy) : saveItem1
    const index = storedQueries.findIndex(q => q.queryId === saveItem.queryId)
    if (index === -1) {
      saveQueries([...storedQueries, saveItem])
      navigate(`/${saveItem.queryId}`)
    } else {
      storedQueries.splice(index, 1, saveItem)
      saveQueries([...storedQueries])
    }
  }, [displayedQuery, storedQueries, cy])

  const { autoLayout, LayoutSelector } = Layout.useAutoLayout(cy)
  const { expandAll, collapseAll } = ExpandCollapse.useExpandCollapse(cy)
  const { currentQueryResult, runQuery, nowLoading } = useNeo4jQueryRunner(cy)
  GraphDataSource.useDataSource(cy, currentQueryResult, displayedQuery)

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

  // 画面表示時
  useEffect(() => {
    if (!cy) return // queryIdが設定されたあとdivが初期化される前にuseEffectが実行されてしまうので
    let loaded: Query | undefined
    if (queryId) {
      loaded = storedQueries.find(q => q.queryId === queryId)
      if (!loaded) dispatchMessage(msg => msg.push('error', `Query id '${queryId}' is not found.`))
    }
    if (!loaded) loaded = createNewQuery()
    setDisplayedQuery(loaded)
    runQuery(loaded)
  }, [queryId, runQuery])

  const resetViewPosition = useCallback(() => {
    cy?.resize().fit().reset()
    autoLayout()
    setDisplayedQuery({ ...displayedQuery, nodePositions: {} })
  }, [cy, autoLayout, displayedQuery])

  const handleKeyDown: React.KeyboardEventHandler<React.ElementType> = useCallback(e => {
    if (e.ctrlKey && e.key === 'Enter') {
      runQuery(displayedQuery)
      e.preventDefault()
    } else if (e.ctrlKey && e.key === 's') {
      handleQuerySaving()
      e.preventDefault()
    } else if (e.ctrlKey && e.key === 'b') {
      dispatchSideMenu(state => state.toggleSideMenu())
      setShowDataSource(!showDataSource)
    }
  }, [runQuery, displayedQuery, handleQuerySaving, showDataSource])

  return (
    <PanelGroup
      direction="vertical"
      className="flex flex-col relative outline-none"
      onKeyDown={handleKeyDown}
      tabIndex={0}>

      {/* ツールバー */}
      <div className="flex content-start items-center gap-2 mb-2">
        <Components.Button
          icon={showSideMenu ? Icon.LeftOutlined : Icon.RightOutlined}
          onClick={() => dispatchSideMenu(state => state.toggleSideMenu())}
        >メニュー</Components.Button>
        <LayoutSelector />
        <Components.Button onClick={resetViewPosition}>
          自動レイアウト
        </Components.Button>
        <label className="text-nowrap">
          <input type="checkbox" checked={locked} onChange={handleLockChanged} />
          ノード位置固定
        </label>
        <Components.Button onClick={expandAll}>
          すべて展開
        </Components.Button>
        <Components.Button onClick={collapseAll}>
          すべて折りたたむ
        </Components.Button>

        <div className="flex-1"></div>

        <Components.Button onClick={() => setShowDataSource(!showDataSource)} icon={showDataSource ? Icon.UpOutlined : Icon.DownOutlined} />
        <span className="text-nowrap">
          データソース:Neo4j
        </span>
        <Components.Button onClick={() => runQuery(displayedQuery)} icon={Icon.ReloadOutlined}>
          {nowLoading ? '読込中...' : '再読込(Ctrl + Enter)'}
        </Components.Button>
        <Components.Button onClick={handleQuerySaving}>
          保存(Ctrl+S)
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
      <Panel className="flex flex-col bg-white relative">
        <div ref={divRef} className="
          overflow-hidden [&>div>canvas]:left-0
          flex-1
          border border-1 border-zinc-400">
        </div>
        <Navigator.Component className="absolute w-[20vw] h-[20vh] right-2 bottom-2 z-[200]" />
        {nowLoading && (
          <Components.NowLoading className="w-10 h-10 absolute left-0 right-0 top-0 bottom-0 m-auto" />
        )}
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

export default {
  usePages,
  Page,
}

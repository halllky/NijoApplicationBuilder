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
import { Query, createNewQuery, useQueryRepository } from './GraphView.Query'
import { useNeo4jQueryRunner } from './GraphView.Neo4j'
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

  // query editing
  const { queryId } = useParams()
  const { storedQueries, saveQueries } = useQueryRepository()
  const [displayedQuery, setDisplayedQuery] = useState(() => createNewQuery())
  const [commitedQueryString, setCommitedQueryString] = useState(() => displayedQuery?.queryString ?? '')
  const navigate = useNavigate()
  const handleQueryStringEdit: React.ChangeEventHandler<HTMLTextAreaElement> = useCallback(e => {
    setDisplayedQuery({ ...displayedQuery, queryString: e.target.value })
  }, [displayedQuery])
  const handleQuerySaving = useCallback(() => {
    const index = storedQueries.findIndex(q => q.queryId === displayedQuery.queryId)
    if (index === -1) {
      saveQueries([...storedQueries, displayedQuery])
      navigate(`/${displayedQuery.queryId}`)
    } else {
      storedQueries.splice(index, 1, displayedQuery)
      saveQueries([...storedQueries])
    }
  }, [displayedQuery, storedQueries])

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

  const { autoLayout, LayoutSelector } = Layout.useAutoLayout(cy)
  const { expandAll, collapseAll } = ExpandCollapse.useExpandCollapse(cy)
  const { forceRerun, nowLoading } = useNeo4jQueryRunner(cy, commitedQueryString, autoLayout)

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

  // 画面表示時、保存されているクエリ定義を取得しクエリ実行
  useEffect(() => {
    const loaded = queryId
      ? storedQueries.find(q => q.queryId === queryId)
      : undefined
    setDisplayedQuery(loaded ?? createNewQuery())
    setCommitedQueryString(loaded?.queryString ?? '')
  }, [queryId, storedQueries])
  const handleQueryRerun = useCallback(() => {
    setCommitedQueryString(displayedQuery?.queryString ?? '')
    forceRerun()
  }, [displayedQuery?.queryString, forceRerun])

  const resetViewPosition = useCallback(() => {
    cy?.resize().fit().reset()
    autoLayout()
  }, [cy, autoLayout])

  return (
    <PanelGroup direction="vertical" className="flex flex-col relative">

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

export default {
  usePages,
  Page,
}

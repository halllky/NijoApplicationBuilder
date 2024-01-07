import React, { useCallback } from 'react'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import * as Icon from '@ant-design/icons'
// import enumerateData from './data'
import { Components, Messaging, ReactHookUtil } from './util'
import { useDataSource } from './Graph.DataSource'
import { useViewState } from './Graph.ViewState'
import { useCytoscape } from './Cy'
import Navigator from './Cy.Navigator'

export default function () {
  const [
    viewState,
    dispatchViewState,
  ] = useViewState()

  const {
    dataSource,
    reloadDataSet,
    saveDataSource,
    nowLoading,
    DataSourceEditor,
  } = useDataSource()

  const {
    reload,
    containerRef,
    reset,
    expandAll,
    collapseAll,
    LayoutSelector,
    nodesLocked,
    toggleNodesLocked,
  } = useCytoscape(reloadDataSet, viewState, dispatchViewState)

  // データソース欄の表示/非表示
  const [showDataSource, setShowDataSource] = ReactHookUtil.useToggle(true)

  const handleKeyDown: React.KeyboardEventHandler<React.ElementType> = useCallback(e => {
    if (e.ctrlKey && e.key === 'Enter') {
      reload()
      e.preventDefault()
    } else if (e.ctrlKey && e.key === 's') {
      saveDataSource()
      e.preventDefault()
    }
  }, [reload, saveDataSource])

  return (
    <PanelGroup
      direction="vertical"
      className="flex flex-col relative outline-none p-2"
      onKeyDown={handleKeyDown}
      tabIndex={0}>

      {/* ツールバー */}
      <div className="flex content-start items-center gap-2 mb-2">

        <Components.Button
          onClick={() => setShowDataSource(x => x.toggle())}
          icon={showDataSource ? Icon.UpOutlined : Icon.DownOutlined}
        />
        <Components.Button onClick={reload}>
          {nowLoading ? '読込中...' : '再読込(Ctrl+Enter)'}
        </Components.Button>

        <div className="flex-1"></div>

        <LayoutSelector />
        <Components.Button onClick={reset}>
          自動レイアウト
        </Components.Button>

        <label className="text-nowrap">
          <input type="checkbox" checked={nodesLocked} onChange={toggleNodesLocked} />
          ノード位置固定
        </label>

        <Components.Button onClick={expandAll}>
          すべて展開
        </Components.Button>
        <Components.Button onClick={collapseAll}>
          すべて折りたたむ
        </Components.Button>

        <Components.Button onClick={saveDataSource}>
          保存(Ctrl+S)
        </Components.Button>
      </div>

      {/* データソース */}
      <Panel defaultSize={16} className={`flex flex-col ${!showDataSource && 'hidden'}`}>
        <DataSourceEditor dataSource={dataSource} className="flex-1" />
      </Panel>

      {showDataSource && <PanelResizeHandle className="h-2" />}

      {/* グラフ */}
      <Panel className="flex flex-col bg-white relative">
        <div ref={containerRef} className="
          overflow-hidden [&>div>canvas]:left-0
          flex-1
          border border-1 border-zinc-400">
        </div>
        <Navigator.Component className="absolute w-[20vw] h-[20vh] right-2 bottom-2 z-[200]" />
        {nowLoading && (
          <Components.NowLoading className="w-10 h-10 absolute left-0 right-0 top-0 bottom-0 m-auto" />
        )}
      </Panel>

      <Messaging.InlineMessageList />
      <Messaging.Toast />
    </PanelGroup>
  )
}

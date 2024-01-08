import React, { useCallback, useEffect, useState } from 'react'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import * as Icon from '@ant-design/icons'
// import enumerateData from './data'
import { Components, Messaging, ReactHookUtil } from './util'
import { useDataSource } from './Graph.DataSource'
import { useCytoscape } from './Cy'
import Navigator from './Cy.Navigator'
import { useTauriApi } from './TauriApi'

export default function () {
  const [, dispatchMessage] = Messaging.useMsgContext()
  const { saveViewStateFile, loadViewStateFile } = useTauriApi()

  const {
    dataSource,
    reloadDataSet,
    saveDataSource,
    DataSourceEditor,
  } = useDataSource()

  const {
    apply,
    containerRef,
    reset,
    expandAll,
    collapseAll,
    LayoutSelector,
    nodesLocked,
    toggleNodesLocked,
    hasNoElements,
    collectViewState,
  } = useCytoscape()

  // -----------------------------------------------------

  // 読込
  const [nowLoading, setNowLoading] = useState(true)
  const reload = useCallback(async () => {
    setNowLoading(true)
    try {
      const dataSet = await reloadDataSet()
      const viewState = await loadViewStateFile()
      apply(dataSet, viewState)
    } catch (error) {
      dispatchMessage(msg => msg.error(error))
    } finally {
      setNowLoading(false)
    }
  }, [reloadDataSet, loadViewStateFile])

  useEffect(() => {
    const timer = setTimeout(reload, 500)
    return () => clearTimeout(timer)
  }, [reload])

  // 保存
  const saveAll = useCallback(async () => {
    try {
      await saveDataSource()
      const viewState = collectViewState()
      await saveViewStateFile(viewState)
      dispatchMessage(msg => msg.info('保存しました。'))
    } catch (error) {
      dispatchMessage(msg => msg.error(error))
    }
  }, [saveDataSource, collectViewState])

  // データソース欄の表示/非表示
  const [showDataSource, setShowDataSource] = ReactHookUtil.useToggle(true)

  // キー操作
  const handleKeyDown: React.KeyboardEventHandler<React.ElementType> = useCallback(e => {
    if (e.ctrlKey && e.key === 's') {
      saveAll()
      e.preventDefault()
    }
  }, [saveAll])

  return (
    <PanelGroup
      direction="vertical"
      className="flex flex-col relative outline-none p-2 bg-zinc-200"
      onKeyDown={handleKeyDown}
      tabIndex={0}>

      {/* ツールバー */}
      <div className="flex content-start items-center gap-2 mb-2">

        <Components.Button
          onClick={() => setShowDataSource(x => x.toggle())}
          icon={showDataSource ? Icon.UpOutlined : Icon.DownOutlined}
        />
        <Components.Button outlined onClick={reload}>
          {nowLoading ? '読込中...' : '再読込(Ctrl+Enter)'}
        </Components.Button>

        <div className="flex-1"></div>

        <LayoutSelector />
        <Components.Button outlined onClick={reset}>自動レイアウト</Components.Button>

        <label className="text-nowrap flex gap-1">
          <input type="checkbox" checked={nodesLocked} onChange={toggleNodesLocked} />
          ノード位置固定
        </label>

        <Components.Button outlined onClick={expandAll}>すべて展開</Components.Button>
        <Components.Button outlined onClick={collapseAll}>すべて折りたたむ</Components.Button>

        <Components.Button onClick={saveAll}>保存(Ctrl+S)</Components.Button>
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
        <Navigator.Component hasNoElements={hasNoElements} className="absolute w-[20vw] h-[20vh] right-2 bottom-2 z-[200]" />
        {nowLoading && (
          <Components.NowLoading className="w-10 h-10 absolute left-0 right-0 top-0 bottom-0 m-auto" />
        )}
      </Panel>

      <Messaging.InlineMessageList />
      <Messaging.Toast />
    </PanelGroup>
  )
}

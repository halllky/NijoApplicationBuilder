import React, { useCallback, useEffect, useState } from 'react'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import * as Icon from '@ant-design/icons'
import { Components, Messaging, ReactHookUtil, StorageUtil } from './util'
import { useDataSourceHandler, UnknownDataSource, IDataSourceHandler } from './DataSource'
import { useCytoscape } from './Cy'
import Navigator from './Cy.Navigator'
import { useTauriApi } from './TauriApi'

function App() {
  const [, dispatchMessage] = Messaging.useMsgContext()
  const { loadTargetFile, saveTargetFile, saveViewStateFile, loadViewStateFile } = useTauriApi()

  const [dataSource, setDataSource] = useState<UnknownDataSource>()
  const [dsHandler, setDsHandler] = useState<IDataSourceHandler>()
  const { defineHandler } = useDataSourceHandler()

  const {
    cy,
    applyToCytoscape,
    containerRef,
    reset,
    expandSelections,
    collapseSelections,
    toggleExpandCollapse,
    LayoutSelector,
    nodesLocked,
    toggleNodesLocked,
    hasNoElements,
    collectViewState,
    ...otherActions
  } = useCytoscape()

  // -----------------------------------------------------
  // 読込
  const [nowLoading, setNowLoading] = useState(true)
  const reload = useCallback(async (source: UnknownDataSource) => {
    setNowLoading(true)
    try {
      const handler = defineHandler(source)
      const dataSet = await handler.reload(source)
      const viewState = await loadViewStateFile()
      setDataSource(source)
      setDsHandler(handler)
      applyToCytoscape(dataSet, viewState)
    } catch (error) {
      dispatchMessage(msg => msg.error(error))
    } finally {
      setNowLoading(false)
    }
  }, [applyToCytoscape, defineHandler, loadViewStateFile])
  const reloadByCurrentData = useCallback(async () => {
    if (dataSource) await reload(dataSource)
  }, [dataSource, reload])

  useEffect(() => {
    const timer = setTimeout(async () => {
      reload(await loadTargetFile())
    }, 500)
    return () => clearTimeout(timer)
  }, [reload])

  // -----------------------------------------------------
  // 保存
  const saveAll = useCallback(async () => {
    try {
      if (dataSource) await saveTargetFile(dataSource)
      const viewState = collectViewState()
      await saveViewStateFile(viewState)
      dispatchMessage(msg => msg.info('保存しました。'))
    } catch (error) {
      dispatchMessage(msg => msg.error(error))
    }
  }, [dataSource, collectViewState])

  // -----------------------------------------------------
  // 表示/非表示
  const [showDataSource, setShowDataSource] = ReactHookUtil.useToggle(true)
  const [showExplorer, setShowExplorer] = ReactHookUtil.useToggle(true)

  // -----------------------------------------------------
  // キー操作
  const handleKeyDown: React.KeyboardEventHandler<HTMLDivElement> = useCallback(e => {
    // console.log(e.key)
    if (e.ctrlKey && e.key === 's') {
      saveAll()
      e.preventDefault()
    } else if (e.ctrlKey && e.key === 'a') {
      otherActions.selectAll()
      e.preventDefault()
    } else if (e.key === 'Space' || e.key === ' ') {
      toggleExpandCollapse()
      e.preventDefault()
    }
  }, [reloadByCurrentData, dataSource, saveAll, toggleExpandCollapse, otherActions])

  // -----------------------------------------------------
  // 選択中の要素のプロパティ
  const [detailJson, setDetailJson] = useState('')
  const updateDetailJson = useCallback(() => {
    if (!cy) {
      setDetailJson('')
      return
    }
    let str: string[] = []
    const selected = [...cy.nodes(':selected'), ...cy.edges(':selected')]
    if (selected.length >= 1) {
      const data = selected[0].data()
      let obj: {}
      if (data.source && data.target) {
        obj = {
          ...data,
          source: cy.$id(data.source).data(),
          target: cy.$id(data.target).data(),
        }
      } else {
        obj = data
      }
      str.push(JSON.stringify(obj, undefined, '  '))
    }
    if (selected.length >= 2) {
      str.push(`...ほか ${selected.length - 1} 件の選択`)
    }
    setDetailJson(str.join('\n'))
  }, [cy])

  return (
    <PanelGroup direction="horizontal" className="w-full h-full bg-zinc-200">

      {/* エクスプローラ */}
      <Panel defaultSize={16} className={`flex flex-col ${!showExplorer && 'hidden'}`}>
        <Components.Button onClick={updateDetailJson}>
          プロパティ
        </Components.Button>
        <span className="flex-1 whitespace-pre">
          {detailJson}
        </span>
      </Panel>

      <PanelResizeHandle className={`w-2 ${!showExplorer && 'hidden'}`} />

      <Panel className="flex flex-col">

        {/* ツールバー */}
        <div className="flex content-start items-center gap-2 p-1">
          <Components.Button
            onClick={() => setShowExplorer(x => x.toggle())}
            icon={Icon.MenuOutlined}
          />
          {dsHandler?.Editor && (
            <Components.Button
              onClick={() => setShowDataSource(x => x.toggle())}
              icon={showDataSource ? Icon.UpOutlined : Icon.DownOutlined}
            />)}
          <Components.Button outlined onClick={reloadByCurrentData}>
            {nowLoading ? '読込中...' : '再読込(Ctrl+Enter)'}
          </Components.Button>

          <div className="flex-1"></div>

          <LayoutSelector />
          <Components.Button outlined onClick={reset}>自動レイアウト</Components.Button>

          <label className="text-nowrap flex gap-1">
            <input type="checkbox" checked={nodesLocked} onChange={toggleNodesLocked} />
            ノード位置固定
          </label>

          <Components.Button outlined onClick={expandSelections}>展開</Components.Button>
          <Components.Button outlined onClick={collapseSelections}>折りたたむ</Components.Button>

          <Components.Button onClick={saveAll}>保存(Ctrl+S)</Components.Button>
        </div>

        <PanelGroup direction="vertical" className="flex-1">
          {/* データソース */}
          <Panel
            defaultSize={16}
            className={`flex flex-col ${!showDataSource && 'hidden'}`}>
            {dsHandler?.Editor && (
              <dsHandler.Editor
                value={dataSource}
                onChange={setDataSource}
                onReload={reloadByCurrentData}
                className="flex-1"
              />
            )}
          </Panel>

          <PanelResizeHandle className={`h-2 ${!showDataSource && 'hidden'}`} />

          {/* グラフ */}
          <Panel className="bg-white relative">
            <div ref={containerRef}
              className="overflow-hidden [&>div>canvas]:left-0 h-full w-full outline-none"
              tabIndex={0}
              onKeyDown={handleKeyDown}>
            </div>
            <Navigator.Component hasNoElements={hasNoElements} className="absolute w-[20vw] h-[20vh] right-2 bottom-2 z-[200]" />
            {nowLoading && (
              <Components.NowLoading className="w-10 h-10 absolute left-0 right-0 top-0 bottom-0 m-auto" />
            )}
          </Panel>

          <Messaging.InlineMessageList />
          <Messaging.Toast />
        </PanelGroup>
      </Panel>
    </PanelGroup>
  )
}


function AppWithContextProvider() {
  return (
    <Messaging.ErrorMessageContextProvider>
      <StorageUtil.LocalStorageContextProvider>
        <App />
      </StorageUtil.LocalStorageContextProvider>
    </Messaging.ErrorMessageContextProvider>
  )
}

export default AppWithContextProvider

import { useMemo } from 'react'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import { BrowserRouter, Routes } from 'react-router-dom'
import * as AppSetting from './appSetting'
import * as SideMenu from './appSideMenu'
import GraphView from './GraphView'
import { StorageUtil } from './util'

function App() {
  return (
    <BrowserRouter>
      <StorageUtil.LocalStorageContextProvider>
        <AppInsideContext />
      </StorageUtil.LocalStorageContextProvider>
    </BrowserRouter>
  )
}

function AppInsideContext() {

  const queryPages = GraphView.usePages()
  const appSettingPages = AppSetting.usePages()
  const sideMenuItems = useMemo(() => [
    ...queryPages.menuItems,
    ...appSettingPages.menuItems,
  ], [queryPages.menuItems, appSettingPages.menuItems])

  return (
    <PanelGroup direction="horizontal">

      <Panel defaultSize={20} className="flex [&>*]:flex-1">
        <SideMenu.Explorer sections={sideMenuItems} />
      </Panel>

      <PanelResizeHandle style={{ width: 8 }} />

      <Panel className="flex [&>*]:flex-1 [&>*]:min-w-0 py-2 pr-2 bg-white">
        <Routes>
          {queryPages.Routes()}
          {appSettingPages.Routes()}
        </Routes>
      </Panel>

    </PanelGroup>
  )
}

export default App

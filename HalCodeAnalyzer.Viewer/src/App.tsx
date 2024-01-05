import { useMemo } from 'react'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import { BrowserRouter, Routes } from 'react-router-dom'
import * as AppSetting from './appSetting'
import * as SideMenu from './appSideMenu'
import GraphView from './GraphView'
import { Messaging, StorageUtil } from './util'

function App() {
  return (
    <BrowserRouter>
      <StorageUtil.LocalStorageContextProvider>
        <Messaging.ErrorMessageContextProvider>
          <SideMenu.SideMenuContextProvider>
            <AppInsideContext />
          </SideMenu.SideMenuContextProvider>
        </Messaging.ErrorMessageContextProvider>
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

  // サイドメニュー表示非表示
  const [{ showSideMenu }] = SideMenu.useSideMenuContext()

  return (
    <PanelGroup direction="horizontal">

      <Panel defaultSize={20} className={`
        flex [&>*]:flex-1
        ${showSideMenu ? '' : 'hidden'}`}>
        <SideMenu.Explorer sections={sideMenuItems} />
      </Panel>

      {showSideMenu && (
        <PanelResizeHandle className="w-2 bg-zinc-100" />
      )}

      <Panel className={`
        flex flex-col
        [&>*:first-child]:flex-1 [&>*:first-child]:min-h-0 py-2
        ${showSideMenu ? 'pr-2' : 'px-2'}
        bg-zinc-100`}>
        <Routes>
          {queryPages.Routes()}
          {appSettingPages.Routes()}
        </Routes>
        <Messaging.InlineMessageList />
      </Panel>

      <Messaging.Toast />
    </PanelGroup>
  )
}

export default App

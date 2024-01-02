import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import { BrowserRouter, Routes } from 'react-router-dom'
import * as AppSetting from './appSetting'
import * as SideMenu from './appSideMenu'
import GraphView from './GraphView'
import { useMemo } from 'react'

function App() {

  const queryPages = GraphView.usePages()
  const appSettingPages = AppSetting.usePages()
  const sideMenuItems = useMemo(() => [
    ...queryPages.menuItems,
    ...appSettingPages.menuItems,
  ], [queryPages.menuItems, appSettingPages.menuItems])

  return (
    <BrowserRouter>
      <PanelGroup direction="horizontal">

        <Panel defaultSize={20} className="flex [&>*]:flex-1">
          <SideMenu.Explorer sections={sideMenuItems} />
        </Panel>

        <PanelResizeHandle style={{ width: 4 }} />

        <Panel className="flex [&>*]:flex-1 [&>*]:min-w-0 p-2 border-l border-1 border-slate-400">
          <Routes>
            {queryPages.Routes()}
            {appSettingPages.Routes()}
          </Routes>
        </Panel>

      </PanelGroup>
    </BrowserRouter>
  )
}

export default App

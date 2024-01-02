import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AppSettingPage } from './appSetting'
import SideMenu from './appSideMenu'
import GraphView from './GraphView'

function App() {
  return (
    <BrowserRouter>
      <SideMenu.ContextProvider>
        <GraphView.ContextProvider>
          <PanelGroup direction="horizontal">

            <Panel defaultSize={20} className="flex [&>*]:flex-1">
              <SideMenu.Explorer />
            </Panel>

            <PanelResizeHandle style={{ width: 4 }} />

            <Panel className="flex [&>*]:flex-1 [&>*]:min-w-0 p-2 border-l border-1 border-slate-400">
              <Routes>
                <Route path="/" element={<GraphView.Page />} />
                <Route path="/settings" element={<AppSettingPage />} />
              </Routes>
            </Panel>

          </PanelGroup>
        </GraphView.ContextProvider>
      </SideMenu.ContextProvider>
    </BrowserRouter>
  )
}

export default App

import { useState } from 'react'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import { TreeExplorer } from './GraphView.TreeExplorer'
import { AppSettingPage } from './appSetting'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { Components } from './util'
import GraphView from './GraphView'

function App() {
  const [location, setLocation] = useState('/')

  return (
    <BrowserRouter>
      <GraphView.ContextProvider>
        <PanelGroup direction="horizontal">

          <Panel defaultSize={20} className="flex flex-col py-2 pl-2 pr-1">
            <button onClick={() => setLocation('/')} className="text-start">グラフ</button>
            <Components.Separator />
            <TreeExplorer className="flex-1 min-h-0" />
            <Components.Separator />
            <button onClick={() => setLocation('/settings')} className="text-start">設定</button>
          </Panel>

          <PanelResizeHandle style={{ width: 4 }} />

          <Panel className="flex [&>*]:flex-1 p-2 border-l border-1 border-slate-400">
            <Routes location={location}>
              <Route path="/" element={<GraphView.Page />} />
              <Route path="/settings" element={<AppSettingPage />} />
            </Routes>
          </Panel>

        </PanelGroup>
      </GraphView.ContextProvider>
    </BrowserRouter>
  )
}

export default App

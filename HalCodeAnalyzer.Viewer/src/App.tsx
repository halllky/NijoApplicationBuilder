import { useCallback, useMemo, useState } from 'react'
import cytoscape from 'cytoscape'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import enumerateData from './data'
import Layout from './Extension.Layout'
import Navigator from './Extension.Navigator'
import ExpandCollapse from './Extension.ExpandCollapse'
import { Toolbar } from './Extension.ToolBar'
import { TreeExplorer } from './Extension.TreeExplorer'
import Neo4j from './Extension.Neo4j'
import { AppSettingPage } from './appSetting'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import * as Components from './input-ui'

Layout.configure(cytoscape)
Navigator.configure(cytoscape)
ExpandCollapse.configure(cytoscape)

function App() {
  const elements = useMemo(() => enumerateData(), [])
  const [cy, setCy] = useState<cytoscape.Core>()
  const [initialized, setInitialized] = useState(false)
  const [location, setLocation] = useState('/')

  const divRef = useCallback((divElement: HTMLDivElement | null) => {
    if (!divElement) return
    const cyInstance = cytoscape({
      container: divElement,
      elements: elements,
      style: STYLESHEET,
      layout: Layout.DEFAULT,
    })
    Navigator.setupCyInstance(cyInstance)
    ExpandCollapse.setupCyInstance(cyInstance)
    setCy(cyInstance)
    if (!initialized) {
      cyInstance.resize().fit().reset()
      setInitialized(true)
    }
  }, [initialized])

  return (
    <BrowserRouter>
      <PanelGroup direction="horizontal">
        <Panel defaultSize={20} className="flex flex-col py-2 pl-2 pr-1">
          <button onClick={() => setLocation('/')} className="text-start">グラフ</button>
          <Components.Separator />
          <TreeExplorer cy={cy} data={elements} className="flex-1 min-h-0" />
          <Components.Separator />
          <button onClick={() => setLocation('/settings')} className="text-start">設定</button>
          <Components.Separator />
          <Neo4j.QueryView />
        </Panel>

        <PanelResizeHandle style={{ width: 4 }} />

        <Panel className="relative flex flex-col p-2 border-l border-1 border-slate-400">
          <Routes location={location}>

            {/* cytoscapeキャンバス */}
            <Route path="/" element={<>
              <Toolbar cy={cy} className="mb-1" />
              <div ref={divRef} className="
                  overflow-hidden [&>div>canvas]:left-0
                  flex-1
                  border border-1 border-slate-400">
              </div>
              <Navigator.Component className="absolute w-1/4 h-1/4 right-6 bottom-6 z-[200]" />
            </>} />

            {/* 設定 */}
            <Route path="/settings" element={<AppSettingPage />} />

          </Routes>
        </Panel>
      </PanelGroup>
    </BrowserRouter>
  )
}

export default App

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

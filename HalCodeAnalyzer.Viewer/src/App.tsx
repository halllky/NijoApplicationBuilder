import { useCallback, useMemo, useState } from 'react'
import cytoscape from 'cytoscape'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import enumerateData from './data'
import Layout from './Extension.Layout'
import Navigator from './Extension.Navigator'
import ExpandCollapse from './Extension.ExpandCollapse'
import { Toolbar } from './Extension.ToolBar'
import { TreeExplorer } from './Extension.TreeExplorer'

Layout.configure(cytoscape)
Navigator.configure(cytoscape)
ExpandCollapse.configure(cytoscape)

function App() {
  const elements = useMemo(() => enumerateData(), [])
  const [cy, setCy] = useState<cytoscape.Core>()
  const [initialized, setInitialized] = useState(false)
  const divRef = useCallback((divElement: HTMLDivElement | null) => {
    if (!divElement) return
    const cyInstance = cytoscape({
      container: divElement,
      elements,
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
  }, [elements, initialized])

  return (
    <PanelGroup direction="horizontal">
      <Panel defaultSize={20}>
        <TreeExplorer cy={cy} data={elements} className="h-full" />
      </Panel>

      <PanelResizeHandle style={{ width: 4 }} />

      <Panel className="relative flex flex-col py-1 pr-1">
        <Toolbar cy={cy} className="mb-1" />

        <div ref={divRef} className="
          overflow-hidden [&>div>canvas]:left-0
          flex-1
          border border-1 border-slate-400">
        </div>

        <Navigator.Component className="absolute w-1/4 h-1/4 right-6 bottom-6 z-[200]" />
      </Panel>
    </PanelGroup>
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

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
  const data = useMemo(() => enumerateData(), [])
  const [cy, setCy] = useState<cytoscape.Core>()
  const divRef = useCallback((divElement: HTMLDivElement | null) => {
    if (cy || !divElement) return

    const cyInstance = cytoscape({
      container: divElement,
      elements: data,
      style: [{
        selector: 'node',
        style: {
          label: 'data(label)',
        },
      }, {
        selector: 'edge',
        style: {
          label: 'data(label)',
          'target-arrow-shape': 'triangle',
          'curve-style': 'bezier',
        },
      }],
      layout: Layout.OPTIONS,
    })
    Navigator.setupCyInstance(cyInstance)
    ExpandCollapse.setupCyInstance(cyInstance)

    setCy(cyInstance)
  }, [cy, data])



  return (
    <PanelGroup direction="horizontal">
      <Panel defaultSize={20}>
        <TreeExplorer cy={cy} data={data} className="h-full" />
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

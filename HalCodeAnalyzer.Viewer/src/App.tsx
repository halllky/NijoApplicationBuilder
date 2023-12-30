import { useEffect, useRef, useState } from 'react'
import cytoscape from 'cytoscape'
// @ts-ignore
import layoutExt from 'cytoscape-dagre'
import './App.css'
import enumerateData from './data'

cytoscape.use(layoutExt)

function App() {
  const divRef = useRef<HTMLDivElement>(null)
  const [cy, setCy] = useState<cytoscape.Core>()

  useEffect(() => {
    if (!cy && divRef.current) setCy(cytoscape({
      container: divRef.current,
      elements: enumerateData(),
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
      layout: {
        name: 'dagre',
        rankDir: 'LR',
      } as any,
    }))
  }, [cy])

  return (
    <div style={{
      width: '100%',
      height: '100%',
      display: 'flex',
    }}>
      <div ref={divRef} className="cytoscape-canvas-container" style={{
        margin: 12,
        flex: 1,
        border: '1px solid gray',
      }}></div>
    </div>
  )
}

export default App

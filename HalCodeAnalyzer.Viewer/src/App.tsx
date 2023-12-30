import { useCallback, useState } from 'react'
import enumerateData from './data'

import cytoscape from 'cytoscape'
// @ts-ignore
import layoutExt from 'cytoscape-dagre'
// @ts-ignore
import cytospaceNavigator from 'cytoscape-navigator'
// import 'cytoscape-context-menus/cytoscape-context-menus.css'
import 'cytoscape-navigator/cytoscape.js-navigator.css'

cytoscape.use(layoutExt)
cytospaceNavigator(cytoscape)

function App() {
  const [cy, setCy] = useState<cytoscape.Core>()
  const divRef = useCallback((divElement: HTMLDivElement | null) => {
    if (cy || !divElement) return

    const cyInstance = cytoscape({
      container: divElement,
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
    });
    (cyInstance as any).navigator({
      container: '.cytoscape-navigator-container', // string | false | undefined. Supported strings: an element id selector (like "#someId"), or a className selector (like ".someClassName"). Otherwise an element will be created by the library.
      viewLiveFramerate: 0, // set false to update graph pan only on drag end; set 0 to do it instantly; set a number (frames per second) to update not more than N times per second
      thumbnailEventFramerate: 30, // max thumbnail's updates per second triggered by graph updates
      thumbnailLiveFramerate: false,// max thumbnail's updates per second. Set false to disable
      dblClickDelay: 200,// milliseconds
      removeCustomContainer: true,// destroy the container specified by user on plugin destroy
      rerenderDelay: 100, // ms to throttle rerender updates to the panzoom for performance
    })
    setCy(cyInstance)
  }, [cy])

  return (
    <div className="cytoscape-root" style={{
      position: 'relative',
      width: '100%',
      height: '100%',
      display: 'flex',
      padding: 12,
    }}>
      {/* キャンバス */}
      <div ref={divRef} className="cytoscape-canvas-container" style={{
        flex: 1,
        border: '1px solid gray',
      }}></div>

      {/* ナビゲータ */}
      <div className="cytoscape-navigator-container" style={{
        position: 'absolute',
        width: '20%',
        height: '20%',
        right: 24,
        bottom: 24,
        overflow: 'hidden', // オーバーレイのうちはみ出た部分を非表示にする
        border: '1px solid gray',
        background: '#fcfcfc',
      }}></div>
    </div>
  )
}

export default App

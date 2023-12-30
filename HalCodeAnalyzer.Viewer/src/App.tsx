import React, { useCallback, useState } from 'react'
import enumerateData from './data'

import cytoscape from 'cytoscape'
// @ts-ignore
import layoutExt from 'cytoscape-dagre'
const LAYOUT_OPTIONS = {
  name: 'dagre',
  rankDir: 'TD',
} as cytoscape.LayoutOptions

// @ts-ignore
import cytospaceNavigator from 'cytoscape-navigator'
import 'cytoscape-navigator/cytoscape.js-navigator.css'

// @ts-ignore
import cytospaceExpandCollapse from 'cytoscape-expand-collapse'

cytoscape.use(layoutExt)
cytospaceNavigator(cytoscape)
cytospaceExpandCollapse(cytoscape)

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
      layout: LAYOUT_OPTIONS,
    })

    const cyInstanceAsAny = cyInstance as any
    cyInstanceAsAny.navigator({
      container: '.cytoscape-navigator-container', // string | false | undefined. Supported strings: an element id selector (like "#someId"), or a className selector (like ".someClassName"). Otherwise an element will be created by the library.
      viewLiveFramerate: 0, // set false to update graph pan only on drag end; set 0 to do it instantly; set a number (frames per second) to update not more than N times per second
      thumbnailEventFramerate: 30, // max thumbnail's updates per second triggered by graph updates
      thumbnailLiveFramerate: false,// max thumbnail's updates per second. Set false to disable
      dblClickDelay: 200,// milliseconds
      removeCustomContainer: true,// destroy the container specified by user on plugin destroy
      rerenderDelay: 100, // ms to throttle rerender updates to the panzoom for performance
    })

    cyInstanceAsAny.expandCollapse({
      layoutBy: LAYOUT_OPTIONS, // to rearrange after expand/collapse. It's just layout options or whole layout function. Choose your side!
      // recommended usage: use cose-bilkent layout with randomize: false to preserve mental map upon expand/collapse
      fisheye: true, // whether to perform fisheye view after expand/collapse you can specify a function too
      animate: true, // whether to animate on drawing changes you can specify a function too
      animationDuration: 1000, // when animate is true, the duration in milliseconds of the animation
      ready: function () { },  // callback when expand/collapse initialized
      undoable: false, // and if undoRedoExtension exists,

      cueEnabled: true, // Whether cues are enabled
      expandCollapseCuePosition: 'top-left', // default cue position is top left you can specify a function per node too
      expandCollapseCueSize: 12, // size of expand-collapse cue
      expandCollapseCueLineSize: 8, // size of lines used for drawing plus-minus icons
      expandCueImage: undefined, // image of expand icon if undefined draw regular expand cue
      collapseCueImage: undefined, // image of collapse icon if undefined draw regular collapse cue
      expandCollapseCueSensitivity: 1, // sensitivity of expand-collapse cues
      edgeTypeInfo: "edgeType", // the name of the field that has the edge type, retrieved from edge.data(), can be a function, if reading the field returns undefined the collapsed edge type will be "unknown"
      groupEdgesOfSameTypeOnCollapse: false, // if true, the edges to be collapsed will be grouped according to their types, and the created collapsed edges will have same type as their group. if false the collapased edge will have "unknown" type.
      allowNestedEdgeCollapse: true, // when you want to collapse a compound edge (edge which contains other edges) and normal edge, should it collapse without expanding the compound first
      zIndex: 100,// z-index value of the canvas in which cue ımages are drawn
    })

    setCy(cyInstance)
  }, [cy])

  // 位置固定
  const [locked, setLocked] = useState(false)
  const handleLockChanged: React.ChangeEventHandler<HTMLInputElement> = useCallback(e => {
    if (!cy) {
      setLocked(false)
      return
    }
    setLocked(e.target.checked)
    cy.autolock(e.target.checked)
  }, [cy, locked])

  // ボタン
  const handlePositionReset = useCallback(() => {
    if (!cy) return
    cy.layout(LAYOUT_OPTIONS)
      ?.run()
    cy.resize()
      .fit()
      .reset()
  }, [cy])
  const handleExpandAll = useCallback(() => {
    (cy as any)?.expandCollapse('get').expandAll()
  }, [cy])
  const handleCollapseAll = useCallback(() => {
    (cy as any)?.expandCollapse('get').collapseAll()
  }, [cy])

  return (
    <div className="cytoscape-root" style={{
      position: 'relative',
      width: '100%',
      height: '100%',
      display: 'flex',
      flexDirection: 'column',
      padding: 12,
    }}>
      {/* ツールバー */}
      <div style={{
        display: 'flex',
        justifyContent: 'flex-start',
        alignItems: 'center',
        gap: 12,
        marginBottom: 4,
      }}>
        <button onClick={handlePositionReset}>位置リセット</button>
        <label>
          <input type="checkbox" checked={locked} onChange={handleLockChanged} />
          ノード位置固定
        </label>
        <button onClick={handleExpandAll}>すべて展開</button>
        <button onClick={handleCollapseAll}>すべて折りたたむ</button>
      </div>

      {/* キャンバス */}
      <div ref={divRef} className="cytoscape-canvas-container" style={{
        flex: 1,
        border: '1px solid gray',
        overflow: 'hidden',
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
        zIndex: 200, // expand-collapse のcueより手前にくるようにする
      }}></div>
    </div>
  )
}

export default App

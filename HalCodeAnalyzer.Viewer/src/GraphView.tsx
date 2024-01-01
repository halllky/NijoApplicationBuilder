import React, { useCallback, useEffect, useState } from 'react'
import cytoscape from 'cytoscape'
import ExpandCollapse from './GraphView.ExpandCollapse'
import { Toolbar } from './GraphView.ToolBar'
import Navigator from './GraphView.Navigator'
import Layout from './GraphView.Layout'
import enumerateData from './data'
import { createContextForFlatObject, useContextForFlatObject, useReducerForFlatObject } from './util'

Layout.configure(cytoscape)
Navigator.configure(cytoscape)
ExpandCollapse.configure(cytoscape)

const Page = () => {
  const [{ cy, elements }, dispatch] = useGraphContext()
  const [initialized, setInitialized] = useState(false)
  useEffect(() => {
    dispatch({ update: 'elements', value: enumerateData() })
  }, [])

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
    dispatch({ update: 'cy', value: cyInstance })
    if (!initialized) {
      cyInstance.resize().fit().reset()
      setInitialized(true)
    }
  }, [elements, initialized, dispatch])

  return (
    <>
      <Toolbar cy={cy} className="mb-1" />
      <div ref={divRef} className="
        overflow-hidden [&>div>canvas]:left-0
        flex-1
        border border-1 border-slate-400">
      </div>
      <Navigator.Component className="absolute w-1/4 h-1/4 right-6 bottom-6 z-[200]" />
    </>
  )
}

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

// ------------------- Context(TreeExplorerで使うために必要) -----------------------
type GraphViewState = {
  cy: cytoscape.Core | undefined
  elements: cytoscape.ElementDefinition[]
}
const getEmptyGraphViewState = (): GraphViewState => ({
  cy: undefined,
  elements: [],
})
const GraphViewContext = createContextForFlatObject(getEmptyGraphViewState())
const useGraphContext = () => useContextForFlatObject(GraphViewContext)
const ContextProvider = ({ children }: {
  children?: React.ReactNode
}) => {
  const reducerValue = useReducerForFlatObject(getEmptyGraphViewState())
  return (
    <GraphViewContext.Provider value={reducerValue}>
      {children}
    </GraphViewContext.Provider>
  )
}

export default {
  Page,
  ContextProvider,
  useGraphContext,
}

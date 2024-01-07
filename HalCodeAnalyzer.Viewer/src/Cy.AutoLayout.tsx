import { useCallback, useState } from 'react'
import cytoscape from 'cytoscape'
// @ts-ignore
import klay from 'cytoscape-klay'
// @ts-ignore
import dagre from 'cytoscape-dagre'
// @ts-ignore
import fcose from 'cytoscape-fcose'

const configure = (cy: typeof cytoscape) => {
  cy.use(klay)
  cy.use(dagre)
  cy.use(fcose)
}

const OPTION_LIST: { [key: string]: cytoscape.LayoutOptions } = {
  'klay': { name: 'klay' },
  'dagre': { name: 'dagre' },
  'fcose': { name: 'fcose' },
  'null': { name: 'null' },
  'random': { name: 'random' },
  'preset': { name: 'preset' },
  'grid': { name: 'grid' },
  'circle': { name: 'circle' },
  'concentric': { name: 'concentric' },
  'breadthfirst': { name: 'breadthfirst' },
  'cose': { name: 'cose' },
}
const DEFAULT = OPTION_LIST['klay']

const useAutoLayout = (cy: cytoscape.Core | undefined) => {
  const [currentLayout, setCurrentLayout] = useState(DEFAULT.name)
  const LayoutSelector = useCallback(() => {
    return (
      <select
        className="border border-1 border-zinc-400"
        value={currentLayout}
        onChange={e => setCurrentLayout(e.target.value)}>
        {Object.entries(OPTION_LIST).map(([key]) => (
          <option key={key} value={key}>
            {key}
          </option>
        ))}
      </select>
    )
  }, [currentLayout])

  const autoLayout = useCallback(() => {
    cy?.layout(OPTION_LIST[currentLayout])?.run()
  }, [cy, currentLayout])

  return {
    autoLayout,
    LayoutSelector,
  }
}

export default {
  OPTION_LIST,
  DEFAULT,
  configure,
  useAutoLayout,
}

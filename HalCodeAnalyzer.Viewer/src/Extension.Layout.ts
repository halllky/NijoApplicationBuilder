import cytoscape from 'cytoscape'
// import darge from 'cytoscape-dagre'
// import fcose from 'cytoscape-fcose'
// @ts-ignore
import klay from 'cytoscape-klay'

const OPTIONS: cytoscape.LayoutOptions = {
  name: 'klay',
}

const configure = (cy: typeof cytoscape) => {
  cy.use(klay)
}

export default {
  OPTIONS,
  configure,
}

import { Node, Edge } from './DataSource'

export type FileDataSource = {
  type?: 'file'
  nodes: Node[]
  edges: Edge[]
}


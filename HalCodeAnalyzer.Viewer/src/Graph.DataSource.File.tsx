import { Node, Edge } from './Graph.DataSource'

export type FileDataSource = {
  type?: 'file'
  nodes: Node[]
  edges: Edge[]
}


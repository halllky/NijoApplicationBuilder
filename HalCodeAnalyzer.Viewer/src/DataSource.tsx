import React, { useCallback } from 'react'
import { useNeo4jDataSource } from './DataSource.Neo4j'

export type DataSet = {
  nodes: { [id: string]: Node }
  edges: Edge[]
}
export type Node = {
  label: string
  parent?: string
}
export type Edge = {
  source: string
  target: string
  label?: string
}

export const createEmptyDataSet = (): DataSet => ({
  nodes: {},
  edges: [],
})

export const useDataSourceHandler = () => {

  const neo4jHandler = useNeo4jDataSource()
  const defineHandler = useCallback((dataSource: UnknownDataSource): IDataSourceHandler => {
    return [
      neo4jHandler,
      DefaultHandler,
    ].find(h => h.match(dataSource?.type))!
  }, [neo4jHandler])

  return { defineHandler }
}

// ---------------------------
export interface IDataSourceHandler {
  match: (type: string | undefined) => boolean
  Editor: DataSourceEditor
  reload: ReloadFunc
}
export type ReloadFunc<T = any> = (dataSource: T) => Promise<DataSet>

export type UnknownDataSource = {
  type?: string
}
export type DataSourceEditor<T = any> = (props: {
  value: T | undefined
  onChange: (v: T) => void
  className?: string
}) => React.ReactNode

const DefaultHandler: IDataSourceHandler = {
  match: () => true,
  Editor: () => <div></div>,
  reload: async () => ({
    nodes: {},
    edges: [],
  }),
}

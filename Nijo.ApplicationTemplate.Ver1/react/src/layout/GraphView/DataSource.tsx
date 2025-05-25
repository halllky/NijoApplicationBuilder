import React from 'react'

export type DataSet = {
  nodes: { [id: string]: Node }
  edges: Edge[]
}
export type Node = {
  id: string
  label: string
  parent?: string
  'color'?: string
  'border-color'?: string
  'background-color'?: string
  'border-color:selected'?: string
  'color:container'?: string
}
export type Edge = {
  source: string
  target: string
  label?: string
  'line-color'?: string
}

export const createEmptyDataSet = (): DataSet => ({
  nodes: {},
  edges: [],
})

// ---------------------------
export interface IDataSourceHandler {
  match: (type: string | undefined) => boolean
  Editor: DataSourceEditor | undefined
  reload: ReloadFunc
}
export type ReloadFunc<T = any> = (dataSource: T) => Promise<DataSet>

export type UnknownDataSource = {
  type?: string
}
export type DataSourceEditor<T = any> = (props: {
  value: T | undefined
  onChange: (v: T) => void
  onReload: () => void
  className?: string
}) => React.ReactNode

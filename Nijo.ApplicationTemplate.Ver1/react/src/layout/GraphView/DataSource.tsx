import React from 'react'

export type DataSet = {
  nodes: { [id: string]: Node }
  edges: Edge[]
}

/** グラフのノード */
export type Node = {
  id: string
  label: string
  parent?: string
  'color'?: string
  'border-color'?: string
  'background-color'?: string
  'border-color:selected'?: string
  'color:container'?: string
  tags?: NodeTag[]
  members?: string[]
}

/** ノードの右肩に表示するタグ */
export type NodeTag = {
  label: string
  'color'?: string
  'background-color'?: string
}

/** グラフのエッジ */
export type Edge = {
  source: string
  target: string
  label?: string
  'line-color'?: string
  'line-style'?: 'solid' | 'dashed' | 'dotted'
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
export type ReloadFunc<T = unknown> = (dataSource: T) => Promise<DataSet>

export type UnknownDataSource = {
  type?: string
}
export type DataSourceEditor<T = unknown> = (props: {
  value: T | undefined
  onChange: (v: T) => void
  onReload: () => void
  className?: string
}) => React.ReactNode

import React, { useCallback, useEffect, useMemo, useState } from 'react'
import { useTauriApi } from './TauriApi'
import { useNeo4jDataSource } from './Graph.DataSource.Neo4j'
import { Messaging } from './util'

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

export const useDataSource = () => {
  const [, dispatchMessage] = Messaging.useMsgContext()

  // データソースの読み込みと保存
  const { loadTargetFile, saveTargetFile } = useTauriApi()
  const [dataSource, setDataSource] = useState<UnknownDataSource>()
  useEffect(() => {
    loadTargetFile().then(obj => {
      setDataSource(obj)
    }).catch(err => {
      dispatchMessage(msg => msg.error(err))
    })
  }, [])

  const saveDataSource = useCallback(async () => {
    if (!dataSource) return
    try {
      await saveTargetFile(dataSource)
      dispatchMessage(msg => msg.info('保存しました。'))
    } catch (error) {
      dispatchMessage(msg => msg.error(error))
    }
  }, [dataSource])

  // ハンドラの決定
  const neo4jHandler = useNeo4jDataSource()
  const handler: IDataSourceHandler = useMemo(() => {
    return [
      neo4jHandler,
      DefaultHandler,
    ].find(h => h.match(dataSource?.type))!
  }, [dataSource?.type])

  // 再読み込み
  const reloadDataSet = useCallback(async () => {
    return await handler.reload(dataSource)
  }, [handler, dataSource])

  const DataSourceEditor = useCallback((props: {
    dataSource: UnknownDataSource | undefined
    className?: string
  }) => {
    return (
      <handler.Editor
        value={props.dataSource}
        onChange={setDataSource}
        className={props.className}
      />
    )
  }, [handler.Editor])

  return {
    dataSource,
    saveDataSource,
    reloadDataSet,
    DataSourceEditor,
  }
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

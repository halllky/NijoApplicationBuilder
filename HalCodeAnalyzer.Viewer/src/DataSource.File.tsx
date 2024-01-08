import { useMemo } from 'react'
import { IDataSourceHandler, ReloadFunc, DataSet } from './DataSource'

export type FileDataSource = DataSet & {
  type?: 'file'
}
const match: IDataSourceHandler['match']
  = type => type === 'file' || type === undefined

export const useFileDataSource = (): IDataSourceHandler => {
  return useMemo(() => ({
    match,
    reload,
    Editor: undefined,
  }), [])
}

const reload: ReloadFunc<FileDataSource> = async dataSource => {
  return await Promise.resolve<DataSet>({
    nodes: { ...dataSource.nodes },
    edges: [...dataSource.edges],
  })
}

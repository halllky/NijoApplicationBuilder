import { useCallback, useMemo, useState } from 'react'
import neo4j, { Node, Relationship, Record } from 'neo4j-driver'
import cytoscape from 'cytoscape'
import { useStoredSettings } from './appSetting'
import { Messaging } from './util'
import { Query } from './GraphView.Query'
import GraphDataSource from './GraphView.DataSource'

export const useNeo4jQueryRunner = (cy: cytoscape.Core | undefined) => {
  // 接続先DBの決定
  const { setting } = useStoredSettings()
  const driver = useMemo(() => {
    if (!setting.activeNeo4jServerId) return null
    const serverConfig = setting.neo4jServer.find(x => x.uniqueId === setting.activeNeo4jServerId)
    if (!serverConfig) return null
    return neo4j.driver(serverConfig.url, neo4j.auth.basic(serverConfig.user, serverConfig.pass))
  }, [setting])

  // クエリ実行
  const [currentQueryResult, setResult] = useState(() => GraphDataSource.createEmptyDataSet())
  const [nowLoading, setNowLoading] = useState(false)
  const [, dispatchMsg] = Messaging.useMsgContext()
  const runQuery = useCallback(async (query: Query) => {
    if (!driver) return
    const session = driver.session({ defaultAccessMode: neo4j.session.READ })
    const dataSet = GraphDataSource.createEmptyDataSet()
    const parentChildMap: { [child: string]: string } = {}
    setNowLoading(true)
    cy?.elements().remove()
    let run: ReturnType<typeof session.run>
    try {
      run = session.run(query.queryString)
    } catch (err) {
      dispatchMsg(state => state.push('error', err))
      return
    }
    run.subscribe({
      onNext: record => neo4jQueryReusltToCytoscapeItem(record, dataSet, parentChildMap),
      onError: err => {
        dispatchMsg(state => state.push('error', err))
        setNowLoading(false)
      },
      onCompleted: async (summary) => {
        console.debug(summary)
        // 親子関係の設定
        for (const [childNodeId, node] of Object.entries(dataSet.nodes)) {
          node.parent = parentChildMap[childNodeId]
        }

        setResult(dataSet)
        setNowLoading(false)
        await session.close()
      },
    })
  }, [cy, driver])

  const clear = useCallback(() => {
    cy?.elements().remove()
  }, [cy])

  return { currentQueryResult, runQuery, clear, nowLoading }
}

/** ノードがこの名前のプロパティを持つ場合は表示名称に使われる */
const NAME = 'name'
/** リレーションシップのtypeがこの値か否かで処理が変わる */
const CHILD = 'HAS_CHILD'

const neo4jQueryReusltToCytoscapeItem = (
  record: Record,
  dataSet: GraphDataSource.DataSet,
  parentChildMap: { [child: string]: string }
): void => {
  const parseValue = (value: any) => {
    if (value instanceof Relationship && value.type === CHILD) {
      parentChildMap[value.endNodeElementId] = value.startNodeElementId

    } else if (value instanceof Relationship) {
      const label = value.properties[NAME] ?? value.type
      const source = value.startNodeElementId
      const target = value.endNodeElementId
      dataSet.edges.push({ source, target, label })

    } else if (value instanceof Node) {
      const id = value.elementId
      const label = value.properties[NAME] ?? value.elementId
      dataSet.nodes[id] = ({ label })

    } else {
      console.warn('Failure to handle qurey result.', record)
    }
  }
  for (const key of record.keys) {
    const value = record.get(key)
    if (Array.isArray(value)) {
      for (const arrayElement of value) parseValue(arrayElement)
    } else {
      parseValue(value)
    }
  }
}

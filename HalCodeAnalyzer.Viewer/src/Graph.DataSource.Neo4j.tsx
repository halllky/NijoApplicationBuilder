import React, { useCallback, useState } from 'react'
import { useForm } from 'react-hook-form'
import neo4j, { Node, Relationship, Record } from 'neo4j-driver'
import * as Icon from '@ant-design/icons'
import { DataSet, DataSourceEditor, IDataSourceHandler, ReloadFunc, createEmptyDataSet } from './Graph.DataSource'
import { Components, Messaging } from './util'

const match: IDataSourceHandler['match'] = type => type === 'neo4j'
type Neo4jDataSource = {
  type: 'neo4j'
  query: string
  connection: {
    host: string
    user: string
    pass: string
  }
}

export const useNeo4jDataSource = (): IDataSourceHandler => {
  const [, dispatchMsg] = Messaging.useMsgContext()

  const reload: ReloadFunc<Neo4jDataSource> = useCallback(async dataSource => {
    try {
      const auth = neo4j.auth.basic(dataSource.connection.user, dataSource.connection.pass)
      const driver = neo4j.driver(dataSource.connection.host, auth)
      const session = driver.session({ defaultAccessMode: neo4j.session.READ })
      const run = session.run(dataSource.query)

      return await new Promise(async (resolve, reject) => {
        const dataSet = createEmptyDataSet()
        const parentChildMap: { [child: string]: string } = {}
        run.subscribe({
          onNext: record => neo4jQueryReusltToCytoscapeItem(
            record,
            dataSet,
            parentChildMap,
            dispatchMsg),

          onCompleted: async (summary) => {
            // 親子関係の設定
            for (const [childNodeId, node] of Object.entries(dataSet.nodes)) {
              node.parent = parentChildMap[childNodeId]
            }

            console.debug(summary)
            resolve(dataSet)
            await session.close()
          },

          onError: err => reject(err),
        })
      })
    } catch (err) {
      dispatchMsg(state => state.push('error', err))
      return createEmptyDataSet()
    }
  }, [])

  return {
    match,
    Editor,
    reload,
  }
}

// ----------------------------------------
/** ノードがこの名前のプロパティを持つ場合は表示名称に使われる */
const NAME = 'name'
/** リレーションシップのtypeがこの値か否かで処理が変わる */
const CHILD = 'HAS_CHILD'

const neo4jQueryReusltToCytoscapeItem = (
  record: Record,
  dataSet: DataSet,
  parentChildMap: { [child: string]: string },
  dispatchMsg: ReturnType<typeof Messaging.useMsgContext>['1'],
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
      dispatchMsg(msg => msg.push('warn', 'Failure to handle qurey result.'))
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

// ----------------------------------------
const Editor: DataSourceEditor<Neo4jDataSource> = ({
  value,
  onChange,
  className,
}) => {
  const [showSettingModal, setShowSettingModal] = useState(false)
  const handleQueryStringEdit: React.ChangeEventHandler<HTMLTextAreaElement> = useCallback(e => {
    if (value) onChange({ ...value, query: e.target.value })
  }, [value, onChange])
  const handleConnectionEdit = useCallback((connection: Neo4jDataSource['connection']) => {
    if (value) onChange({ ...value, connection })
    setShowSettingModal(false)
  }, [value, onChange])
  const handleCancelConnEdit = useCallback(() => {
    setShowSettingModal(false)
  }, [])

  return (
    <div className={`relative flex ${className}`}>
      <Components.Textarea
        value={value?.query}
        onChange={handleQueryStringEdit}
        className="flex-1 font-mono"
        inputClassName="resize-none"
      />

      <Components.Button
        onClick={() => setShowSettingModal(true)}
        icon={Icon.SettingOutlined}>
        設定
      </Components.Button>

      {showSettingModal && value && (
        <Components.Modal>
          <ConnectionSettingView
            value={value.connection}
            onChange={handleConnectionEdit}
            onCancel={handleCancelConnEdit}
          />
        </Components.Modal>
      )}
    </div>
  )
}

const ConnectionSettingView = (props: {
  value: Neo4jDataSource['connection']
  onChange: (v: Neo4jDataSource['connection']) => void
  onCancel: () => void
  className?: string
}) => {
  const { register, handleSubmit } = useForm<Neo4jDataSource['connection']>({
    defaultValues: props.value
  })

  return (
    <form
      onSubmit={handleSubmit(props.onChange)}
      className={`flex flex-col gap-1 ${props.className}`}
    >
      <Components.Text type="url" labelText="URL" labelClassName="basis-24" placeholder="neo4j://localhost:7687"
        {...register(`host`)}
      />
      <Components.Text type="text" labelText="USER" labelClassName="basis-24" placeholder="neo4j"
        {...register(`user`)}
      />
      <Components.Text type="password" labelText="PASSWORD" labelClassName="basis-24"
        {...register(`pass`)}
      />
      <Components.Separator />
      <div className="flex">
        <Components.Button onClick={props.onCancel}>キャンセル</Components.Button>
        <div className="flex-1"></div>
        <Components.Button submit>保存</Components.Button>
      </div>
    </form>
  )
}

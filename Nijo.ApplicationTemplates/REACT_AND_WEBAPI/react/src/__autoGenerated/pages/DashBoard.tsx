import { useCallback, useState } from 'react'
import { useHttpRequest, useMsgContext, useDummyDataGenerator, useLocalRepositoryChangeList } from '../util'
import * as Input from '../input'
import { VerticalForm as VForm } from '../collection'

/** DashBoard */
export default function ({ applicationName }: {
  applicationName?: string
}) {

  // デバッグ用DB再作成コマンド
  const [, dispatchMsg] = useMsgContext()
  const { post } = useHttpRequest()
  const [withDummyData, setWithDummyData] = useState<boolean | undefined>(true)
  const genereateDummyData = useDummyDataGenerator()
  const {reset: resetLocalRepository} = useLocalRepositoryChangeList()
  const recreateDatabase = useCallback(async () => {
    if (window.confirm('DBを再作成します。データは全て削除されます。よろしいですか？')) {
      try {
        await resetLocalRepository()
      } catch (error) {
        dispatchMsg(msg => msg.error(`ローカルリポジトリの初期化に失敗しました: ${error}`))
      }

      const response = await post('/WebDebugger/recreate-database')
      if (!response.ok) { return }
      if (withDummyData) {
        const success = await genereateDummyData()
        if (!success) {
          dispatchMsg(msg => msg.error('DBを再作成しましたがダミーデータ作成に失敗しました。'))
          return
        }
      }
      dispatchMsg(msg => msg.info('DBを再作成しました。'))
    }
  }, [post, withDummyData, genereateDummyData, resetLocalRepository, dispatchMsg])

  return (
    <div className="page-content-root">
      <h1 className="p-1 text-lg font-semibold select-none">
        {applicationName}
      </h1>

      <hr />

      {import.meta.env.DEV && (
        <VForm.Root className="p-4">
          <VForm.Section label="デバッグ用コマンド（開発環境でのみ有効）" table>
            <VForm.Row fullWidth>
              <Input.Button outlined onClick={recreateDatabase}>DBを再作成する</Input.Button>
              <Input.CheckBox value={withDummyData} onChange={setWithDummyData}>ダミーデータも併せて作成する</Input.CheckBox>
            </VForm.Row>
          </VForm.Section>
        </VForm.Root>
      )}
    </div>
  )
}

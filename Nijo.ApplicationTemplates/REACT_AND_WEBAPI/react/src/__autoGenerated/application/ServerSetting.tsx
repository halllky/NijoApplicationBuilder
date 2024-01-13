import { useCallback, useState } from 'react';
import { UserSettings, useUserSetting } from './UserSetting';
import * as Input from "../components";
import { VerticalForm as VForm } from "../layout";
import { useDummyDataGenerator, useFormEx, useHttpRequest, useMsgContext } from '../util';

export const ServerSettingScreen = () => {

  const { data: appState, save } = useUserSetting()
  const [, dispatchMsg] = useMsgContext()
  const { registerEx, handleSubmit } = useFormEx<UserSettings>({ defaultValues: appState })

  // --------------------------------------
  // DB再作成
  const { post } = useHttpRequest()
  const [withDummyData, setWithDummyData] = useState<boolean | undefined>(true)
  const genereateDummyData = useDummyDataGenerator()
  const recreateDatabase = useCallback(async () => {
    if (window.confirm('DBを再作成します。データは全て削除されます。よろしいですか？')) {
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
  }, [post, withDummyData, genereateDummyData])

  return (
    <div className="page-content-root">
      <VForm.Root onSubmit={handleSubmit(save)}>
        <VForm.Section label="基本設定" table>
          <VForm.Row label="APIサーバーURL">
            <Input.Word {...registerEx('apiDomain')} />
          </VForm.Row>
          <VForm.Row label="ダークモード">
            <Input.CheckBox {...registerEx('darkMode')} />
          </VForm.Row>
        </VForm.Section>

        <VForm.Spacer />

        {process.env.NODE_ENV === 'development' && (
          <VForm.Section label="データベース" table>
            <VForm.Row fullWidth>
              <Input.IconButton fill onClick={recreateDatabase}>DB再作成</Input.IconButton>
              <Input.CheckBox value={withDummyData} onChange={setWithDummyData}>ダミーデータも併せて作成する</Input.CheckBox>
            </VForm.Row>
          </VForm.Section>
        )}

      </VForm.Root>
    </div>
  )
}

import { useCallback, useMemo, useState } from 'react';
import * as Input from "../input";
import { VerticalForm as VForm } from "../collection";
import { useFormEx } from "./ReactHookFormUtil";
import { useMsgContext } from './Notification'
import { useHttpRequest } from './Http'
import { defineStorageContext } from './Storage'
import { useDummyDataGenerator } from './useDummyDataGenerator'

export type UserSettings = {
  apiDomain?: string
  darkMode?: boolean
  fontFamily?: string
  environmentName?: string
  environmentColor?: string
}
export const DEFAULT_FONT_FAMILY = [
  'Arial',
  '"BIZ UDGothic"',
  'sans-serif'
].join(',')

export const [UserSettingContextProvider, useUserSetting] = defineStorageContext<UserSettings>({
  storageKey: 'appcontext',
  defaultValue: () => ({}),
  serialize: obj => JSON.stringify(obj),
  deserialize: str => ({ ok: true, obj: JSON.parse(str) as UserSettings }),
})


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
  }, [post, withDummyData, genereateDummyData, dispatchMsg])

  return (
    <form className="page-content-root" onSubmit={handleSubmit(save)}>
      <VForm.Root>
        <VForm.Section label="基本設定" table>
          <VForm.Row label="APIサーバーURL">
            <Input.Word {...registerEx('apiDomain')} />
          </VForm.Row>
          <VForm.Row label="ダークモード">
            <Input.CheckBox {...registerEx('darkMode')} />
          </VForm.Row>
          <VForm.Row label="フォント(font-family)">
            <Input.Word {...registerEx('fontFamily')} className="flex-1" />
          </VForm.Row>
          <VForm.Row label="画面隅のリボンの文字">
            <Input.Word {...registerEx('environmentName')} />
          </VForm.Row>
          <VForm.Row label="画面隅のリボンの色">
            <Input.Word {...registerEx('environmentColor')} />
          </VForm.Row>
          <VForm.Row fullWidth>
            <Input.Button outlined submit>更新</Input.Button>
          </VForm.Row>
        </VForm.Section>

        <VForm.Spacer />

        {process.env.NODE_ENV === 'development' && (
          <VForm.Section label="データベース" table>
            <VForm.Row fullWidth>
              <Input.Button outlined onClick={recreateDatabase}>DB再作成</Input.Button>
              <Input.CheckBox value={withDummyData} onChange={setWithDummyData}>ダミーデータも併せて作成する</Input.CheckBox>
            </VForm.Row>
          </VForm.Section>
        )}

      </VForm.Root>
    </form>
  )
}

// ---------------------------------
/** 画面隅のリボン */
export const EnvNameRibbon = () => {
  const { data: {
    darkMode,
    environmentName,
    environmentColor,
  } } = useUserSetting()

  const style: React.CSSProperties = useMemo(() => ({
    bottom: 24,
    right: -96,
    width: 320,
    height: 72,
    backgroundColor: environmentColor ? environmentColor : undefined,
    color: darkMode ? 'black' : 'white',
    zIndex: 100,
  }), [darkMode, environmentColor])

  if (!environmentName) return <></>

  return (
    <div className="
    fixed flex justify-center items-center text-4xl
    rotate-[-45deg] opacity-75 pointer-events-none select-none" style={style}>
      {environmentName}
    </div>
  )
}

import { useCallback, useMemo } from 'react';
import * as Input from "../input";
import { PageFrame, PageTitle, VForm2 as VForm } from '../collection'
import { useFormEx } from './ReactHookFormUtil'
import { defineStorageContext } from './Storage'
import { useToastContext } from './Notification'

export type UserSettings = {
  apiDomain?: string
  darkMode?: boolean
  fontFamily?: string
  environmentName?: string
  environmentColor?: string
}
export const DEFAULT_FONT_FAMILY = [
  '"Cascadia Mono"',
  '"BIZ UDGothic"',
  'Arial',
  'sans-serif'
].join(',')

export const [UserSettingContextProvider, useUserSetting] = defineStorageContext<UserSettings>({
  storageKey: 'appcontext',
  defaultValue: () => ({}),
  serialize: obj => JSON.stringify(obj),
  deserialize: str => ({ ok: true, obj: JSON.parse(str) as UserSettings }),
})


export const ServerSettingScreen = () => {

  const [, dispatchToast] = useToastContext()
  const { data: appState, save } = useUserSetting()
  const { registerEx, getValues } = useFormEx<UserSettings>({ defaultValues: appState })
  const handleSave = useCallback(() => {
    save(getValues())
    dispatchToast(msg => msg.info('保存しました。'))
  }, [save, dispatchToast])

  return (
    <PageFrame
      header={<>
        <PageTitle>設定</PageTitle>
        <div className="flex-1"></div>
        <Input.IconButton fill onClick={handleSave}>更新</Input.IconButton>
      </>}
    >
      <VForm.Root estimatedLabelWidth="10rem">
        <VForm.Indent label="外観設定">
          <VForm.AutoColumn>
            <VForm.Item label="ダークモード">
              <Input.CheckBox {...registerEx('darkMode')} />
            </VForm.Item>
          </VForm.AutoColumn>
          <VForm.Item label="フォント" wideValue>
            <Input.Word {...registerEx('fontFamily')} className="w-full" />
            <span className="block text-sm text-color-6">
              CSSの font-family の記法で記載してください。
            </span>
          </VForm.Item>
        </VForm.Indent>

        <VForm.Indent label="画面隅のリボン">
          <VForm.AutoColumn>
            <VForm.Item label="文字">
              <Input.Word {...registerEx('environmentName')} />
            </VForm.Item>
            <VForm.Item label="色">
              <Input.Word {...registerEx('environmentColor')} />
            </VForm.Item>
          </VForm.AutoColumn>
        </VForm.Indent>

        <VForm.Indent label="デバッグ設定">
          <VForm.Item label="APIサーバーURL" wideValue>
            <Input.Word {...registerEx('apiDomain')} className="w-full" />
            <span className="block text-sm text-color-6">
              この値は基本的に未指定で問題ありませんが、
              APIサーバーが動作しているにもかかわらず
              接続できない場合は手入力してください。
              <br />
              未指定の場合の規定値は <span className="select-all">{import.meta.env.VITE_BACKEND_API}</span> です。
            </span>
          </VForm.Item>
        </VForm.Indent>
      </VForm.Root>
    </PageFrame>
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

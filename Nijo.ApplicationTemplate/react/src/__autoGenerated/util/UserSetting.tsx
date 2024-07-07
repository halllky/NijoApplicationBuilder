import { useCallback, useMemo } from 'react';
import * as Input from "../input";
import { VerticalForm as VForm } from "../collection";
import { useFormEx } from "./ReactHookFormUtil";
import { defineStorageContext } from './Storage'
import { useToastContext } from './Notification'
import { SideMenuCollapseButton } from './SideMenuCollapseButton';

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

  const [, dispatchToast] = useToastContext()
  const { data: appState, save } = useUserSetting()
  const { registerEx, handleSubmit } = useFormEx<UserSettings>({ defaultValues: appState })
  const handleSave = useCallback((value: UserSettings) => {
    save(value)
    dispatchToast(msg => msg.info('保存しました。'))
  }, [save, dispatchToast])

  return (
    <form className="page-content-root" onSubmit={handleSubmit(handleSave)}>
      <h1 className="flex gap-1 p-1 font-semibold select-none">
        <SideMenuCollapseButton />
        設定
      </h1>
      <VForm.Container className="p-1">
        <VForm.Container label="基本設定">
          <VForm.Item label="ダークモード">
            <Input.CheckBox {...registerEx('darkMode')} />
          </VForm.Item>
          <VForm.Item label="フォント(font-family)" wide className="p-1">
            <Input.Word {...registerEx('fontFamily')} className="w-full" />
          </VForm.Item>
          <VForm.Container label="画面隅のリボン" leftColumnMinWidth="8rem">
            <VForm.Item label="文字">
              <Input.Word {...registerEx('environmentName')} />
            </VForm.Item>
            <VForm.Item label="色">
              <Input.Word {...registerEx('environmentColor')} />
            </VForm.Item>
          </VForm.Container>
          <VForm.Item label="APIサーバーURL" wide className="flex flex-col w-full p-1">
            <Input.Word {...registerEx('apiDomain')} />
            <span className="text-sm">
              この値は基本的に未指定で問題ありませんが、
              APIサーバーが動作しているにもかかわらず
              接続できない場合は手入力してください。
              <br />
              未指定の場合の規定値は <span>{import.meta.env.VITE_BACKEND_API}</span> です。
            </span>
          </VForm.Item>
          <VForm.Item wide>
            <Input.Button outlined submit>更新</Input.Button>
          </VForm.Item>
        </VForm.Container>
      </VForm.Container>
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

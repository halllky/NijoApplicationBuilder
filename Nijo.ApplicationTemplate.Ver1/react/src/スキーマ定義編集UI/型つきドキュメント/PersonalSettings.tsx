import * as React from "react";
import * as ReactHookForm from "react-hook-form";
import useEvent from "react-use-event-hook";
import * as Layout from "../../layout";
import * as Input from "../../input";
import { PersonalSettings } from "./types";

/**
 * ユーザー自身にだけ適用される設定。
 * 実体はローカルストレージに保存される。
 */
export const usePersonalSettings = () => {

  const [forceUpdate, setForceUpdate] = React.useState(-1)
  const personalSettings = React.useMemo((): PersonalSettings => {
    try {
      const settings = localStorage.getItem(LOCAL_STORAGE_KEY)
      if (!settings) return {}
      return JSON.parse(settings)
    } catch (e) {
      return {}
    }
  }, [forceUpdate])

  const save = React.useCallback(<TPath extends ReactHookForm.Path<PersonalSettings>>(
    path: TPath,
    value: ReactHookForm.PathValue<PersonalSettings, TPath>
  ) => {
    const clone = window.structuredClone(personalSettings)
    ReactHookForm.set(clone, path, value)
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(clone))
    setForceUpdate(prev => prev * -1)
  }, [personalSettings])

  // ブラウザの他のタブで設定が更新された場合にこちらにも適用する
  React.useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === LOCAL_STORAGE_KEY) {
        setForceUpdate(prev => prev * -1)
      }
    }
    window.addEventListener('storage', handleStorageChange)
    return () => {
      window.removeEventListener('storage', handleStorageChange)
    }
  }, [])

  return {
    personalSettings,
    save,
  }
}

const LOCAL_STORAGE_KEY = 'typedDocument:personalSettings'

export const PersonalSettingsEditDialog = ({ onClose }: {
  onClose: () => void
}) => {

  const { personalSettings, save } = usePersonalSettings()

  const handleChangeHideGridButtons = useEvent((e: React.ChangeEvent<HTMLInputElement>) => {
    save('hideGridButtons', e.target.checked)
  })

  return (
    <Layout.ModalDialog open onOutsideClick={onClose} className="relative w-md h-96 flex flex-col bg-white border border-gray-400">
      <div className="flex flex-col gap-px px-8 py-1 border-b border-gray-300">
        <h1 className="font-bold">
          個人設定
        </h1>
        <p className="text-sm text-gray-500">
          この設定は自分にだけ適用されます。
        </p>
      </div>

      <div className="flex-1 flex flex-col gap-2 px-8 py-1 overflow-y-auto">
        <label className="flex items-center gap-2">
          <input type="checkbox"
            checked={personalSettings.hideGridButtons}
            onChange={handleChangeHideGridButtons}
          />
          <span>グリッドの操作説明ボタンを非表示にする</span>
        </label>
      </div>

      <div className="flex justify-end px-8 py-1 border-t border-gray-300">
        <Input.IconButton onClick={onClose} outline>閉じる</Input.IconButton>
      </div>
    </Layout.ModalDialog>
  )
}

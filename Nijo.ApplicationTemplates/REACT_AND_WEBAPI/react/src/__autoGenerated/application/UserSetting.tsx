import { defineStorageContext } from "../util/Storage";

export type UserSettings = {
  apiDomain?: string
  darkMode?: boolean
}

export const [UserSettingContextProvider, useUserSetting] = defineStorageContext<UserSettings>({
  storageKey: 'appcontext',
  defaultValue: () => ({}),
  serialize: obj => JSON.stringify(obj),
  deserialize: str => ({ ok: true, obj: JSON.parse(str) as UserSettings }),
})

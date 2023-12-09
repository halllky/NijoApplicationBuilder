import { ArrowPathIcon, BookmarkSquareIcon, PlusIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { useCallback, useState } from 'react';
import { FieldValues, SubmitHandler, useFieldArray, useForm } from 'react-hook-form';
import { UUID } from 'uuidjs';
import { ClientSettings, useAppContext } from './AppContext';
import * as Input from "../user-input";
import { VerticalForm as VForm } from "../layout";
import { InlineMessageBar, BarMessage } from '../decoration';
import { useHttpRequest } from '../util';

export const ServerSettingScreen = () => {

  const [appState, dispatch] = useAppContext()
  const { get, post } = useHttpRequest()

  // --------------------------------------

  const [commandErrors, setCommandErrors] = useState([] as BarMessage[])
  const recreateDatabase = useCallback(async () => {
    if (window.confirm('DBを再作成します。データは全て削除されます。よろしいですか？')) {
      const response = await post('/HalappDebug/recreate-database')
      if (response.ok) {
        alert('DBを再作成しました。')
      } else {
        setCommandErrors([...commandErrors, ...response.errors])
      }
    }
  }, [post, commandErrors, setCommandErrors])

  // --------------------------------------

  const [settingErrors, setSettingErrors] = useState([] as BarMessage[])
  const loadSettings = useCallback(async () => {
    const response = await get<DbSetting>(`/HalappDebug/secret-settings`)
    if (response.ok) {
      return objectToFieldValues(response.data)
    } else {
      setSettingErrors([...settingErrors, ...response.errors])
      return {}
    }
  }, [get, settingErrors, setSettingErrors])
  const { control, register, handleSubmit, reset } = useForm({ defaultValues: loadSettings })
  const { fields, append, remove } = useFieldArray({ name: 'db', control })
  const reload = useCallback(async () => {
    reset(await loadSettings())
  }, [loadSettings, reset])

  const onSaveSettings: SubmitHandler<FieldValues> = useCallback(async data => {
    const response = await post('/HalappDebug/secret-settings', fieldValuesToObject(data))
    if (response.ok) {
      setSettingErrors([])
    } else {
      setSettingErrors([...settingErrors, ...response.errors])
    }
  }, [post, settingErrors, setSettingErrors])
  const onError = useCallback(() => {
    setSettingErrors([...settingErrors, { uuid: UUID.generate(), text: 'ERROR!' }])
  }, [settingErrors])



  const {
    registerEx,
    handleSubmit: handleSubmit2,
  } = Input.useFormEx<ClientSettings>({ defaultValues: appState })

  return (
    <div className="page-content-root">
      <VForm.Root>
        <VForm.Section label="基本設定" table>
          <VForm.Row label="APIサーバーURL">
            <Input.Word
              value={appState.apiDomain}
              onChange={v => dispatch({ type: 'changeDomain', value: v ?? '' })}
            />
          </VForm.Row>
          <VForm.Row label="ダークモード">
            <Input.CheckBox
              value={appState.darkMode}
              onChange={v => dispatch({ type: 'changeDarkMode', value: v ?? false })}
            />
          </VForm.Row>
        </VForm.Section>

        <VForm.Spacer />

        {process.env.NODE_ENV === 'development' && (
          <VForm.Section label="データベース" table>
            <VForm.Row hidden={commandErrors.length === 0}>
              <InlineMessageBar value={commandErrors} onChange={setCommandErrors} />
            </VForm.Row>
            <VForm.Row fullWidth>
              <Input.IconButton fill onClick={recreateDatabase}>DB再作成</Input.IconButton>
            </VForm.Row>
          </VForm.Section>
        )}

      </VForm.Root>
    </div>
  )
}

type DbSetting = {
  currentDb: string | null
  db: { name: string, connStr: string }[]
}
const fieldValuesToObject = (data: FieldValues): DbSetting => {
  const db = (data['db'] as { name: string, connStr: string }[]).map(({ name, connStr }) => ({ name, connStr }))
  const strCurrentDb = data['currentDb'] as string | null
  const numCurrentDb = strCurrentDb == null ? NaN : Number.parseInt(strCurrentDb)
  const currentDb = isNaN(numCurrentDb) ? null : db[numCurrentDb]?.name

  return { db, currentDb }
}
const objectToFieldValues = (obj: DbSetting): FieldValues => {
  const db = obj['db'] as { name: string, connStr: string }[]
  const numCurrentDb = db.findIndex(item => item.name === obj['currentDb'])
  const currentDb = numCurrentDb === -1 ? null : String(numCurrentDb)

  return { db, currentDb }
}

const SettingSection = ({ title, sholder, children }: {
  title?: string
  sholder?: React.ReactNode
  children?: React.ReactNode
}) => {
  return (
    <section className="flex flex-col items-stretch space-y-2 border border-color-4 p-2">
      <div className="flex justify-start items-center">
        {title && <h2 className="text-sm font-semibold select-none">{title}</h2>}
        {sholder}
      </div>
      {children}
    </section>
  )
}

const Setting = ({ label, children, multiRow }: {
  label?: React.ReactNode
  children?: React.ReactNode
  multiRow?: true
}) => {
  return (
    <div className={`flex flex-row space-x-2 ${(multiRow ? 'items-start' : 'items-center')}`}>
      <span className="text-sm select-none basis-32">
        {label}
      </span>
      {children}
    </div>
  )
}

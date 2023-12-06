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
      if (!response.ok) setCommandErrors([...commandErrors, ...response.errors])
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
            <Input.Word {...registerEx(`apiDomain`)} />
          </VForm.Row>
          <VForm.Row label="ダークモード">
            <Input.CheckBox {...registerEx(`darkMode`)} />
          </VForm.Row>
          <VForm.Row fullWidth>
            <Input.IconButton underline icon={ArrowPathIcon} onClick={reload}>保存</Input.IconButton>
          </VForm.Row>
        </VForm.Section>

        <VForm.Spacer />

        {process.env.NODE_ENV === 'development' && (
          <VForm.Section label="データベース設定" table>
            <VForm.Row fullWidth>
              <Input.IconButton underline icon={ArrowPathIcon} onClick={reload}>再読み込み</Input.IconButton>
              <Input.IconButton fill onClick={recreateDatabase}>DB再作成</Input.IconButton>
              <InlineMessageBar value={commandErrors} onChange={setCommandErrors} />
            </VForm.Row>
          </VForm.Section>
        )}

      </VForm.Root>

      ------------------------------------------------------

      {/* {process.env.NODE_ENV === 'development' &&
        <SettingSection
          title="データベース"
          sholder={<IconButton underline icon={ArrowPathIcon} onClick={reload}>再読み込み</IconButton>}
        >
          <InlineMessageBar value={settingErrors} onChange={setSettingErrors} />
          <form onSubmit={handleSubmit(onSaveSettings, onError)}>
            <Setting
              label={<div className="flex items-center">
                接続先DB
                <IconButton underline icon={PlusIcon} onClick={e => { append(e); e.preventDefault() }} className="self-start inline-flex ml-1">追加</IconButton>
              </div>}
              multiRow
            >
              <div className="flex-1 flex flex-col items-stretch space-y-1">
                {fields.map((field, index) => (
                  <div key={field.id} className="flex items-center space-x-1">
                    <input {...register(`currentDb`)} type="radio" value={index} />
                    <Word {...register(`db.${index}.name`, { required: true })} className="basis-32 min-w-0" />
                    <Word {...register(`db.${index}.connStr`, { required: true })} className="flex-1" />
                    <IconButton underline icon={XMarkIcon} onClick={e => { remove(index); e.preventDefault() }}>削除</IconButton>
                  </div>
                ))}

              </div>
            </Setting>
            <IconButton fill icon={BookmarkSquareIcon} className="mt-2">保存</IconButton>
          </form>
        </SettingSection>}

      {process.env.NODE_ENV === 'development' &&
        <SettingSection title="デバッグ用コマンド（開発環境でのみ有効）">
          <InlineMessageBar value={commandErrors} onChange={setCommandErrors} />
          <Setting label="DB再作成">
            <IconButton fill onClick={recreateDatabase}>実行</IconButton>
          </Setting>
        </SettingSection>} */}

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

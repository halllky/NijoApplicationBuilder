import { BookmarkSquareIcon, PlusIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { useCallback, useState } from 'react';
import { FieldValues, SubmitHandler, useFieldArray, useForm } from 'react-hook-form';
import { UUID } from 'uuidjs';
import { useAppContext } from '../hooks/AppContext';
import { IconButton } from './IconButton';
import { InlineMessageBar, BarMessage } from './InlineMessageBar';

export const SettingsScreen = () => {

  const [{ apiDomain }, dispatch] = useAppContext()

  // --------------------------------------

  const [commandErrors, setCommandErrors] = useState([] as BarMessage[])
  const recreateDatabase = useCallback(() => {
    if (window.confirm('DBを再作成します。データは全て削除されます。よろしいですか？')) {
      fetch(`${apiDomain}/HalappDebug/recreate-database`, {
        method: 'POST',
      }).then(async response => {
        setCommandErrors([])
        dispatch({ type: 'pushMsg', msg: await response.text() })
      }).catch(err => {
        const text = err?.message || JSON.stringify(err)
        setCommandErrors([...commandErrors, { uuid: UUID.generate(), text }])
      })
    }
  }, [apiDomain, dispatch, commandErrors])

  // --------------------------------------

  const [settingErrors, setSettingErrors] = useState([] as BarMessage[])
  const defaultValues = useCallback(async () => {
    const response = await fetch(`${apiDomain}/HalappDebug/secret-settings`)
    if (response.ok) {
      const data = await response.text()
      return objectToFieldValues(JSON.parse(data))
    } else {
      const text = await response.text()
      setSettingErrors([...settingErrors, { uuid: UUID.generate(), text }])
      return {}
    }
  }, [settingErrors, apiDomain])

  const { control, register, handleSubmit } = useForm({ defaultValues })
  const { fields, append, remove } = useFieldArray({ name: 'db', control })

  const onSaveSettings: SubmitHandler<FieldValues> = useCallback(data => {
    fetch(`${apiDomain}/HalappDebug/secret-settings`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(fieldValuesToObject(data))
    }).then(async response => {
      setSettingErrors([])
      dispatch({ type: 'pushMsg', msg: await response.text() })
    }).catch(err => {
      const text = JSON.stringify(err)
      setSettingErrors([...settingErrors, { uuid: UUID.generate(), text }])
    })
  }, [settingErrors, apiDomain, dispatch])
  const onError = useCallback(() => {
    setSettingErrors([...settingErrors, { uuid: UUID.generate(), text: 'ERROR!' }])
  }, [settingErrors])

  return (
    <div className="page-content-root">

      <SettingSection title="基本設定">
        <Setting label="サーバーURL">
          <input type="text" value={apiDomain} onChange={e => dispatch({ type: 'changeDomain', value: e.target.value })} className="border" />
        </Setting>
      </SettingSection>

      {process.env.NODE_ENV === 'development' &&
        <SettingSection title="シークレット設定（開発環境でのみ有効）">
          <InlineMessageBar value={settingErrors} onChange={setSettingErrors} />
          <form onSubmit={handleSubmit(onSaveSettings, onError)}>
            <Setting label="接続先DB" multiRow>
              <div className="flex-1 flex flex-col items-stretch space-y-1">
                {fields.map((field, index) => (
                  <div key={field.id} className="flex items-center space-x-1">
                    <input {...register(`currentDb`)} type="radio" value={index} />
                    <input {...register(`db.${index}.name`, { required: true })} className="border basis-32 min-w-0" />
                    <input {...register(`db.${index}.connStr`, { required: true })} className="border flex-1" />
                    <IconButton icon={XMarkIcon} onClick={e => { remove(index); e.preventDefault() }} />
                  </div>
                ))}
                <IconButton outline icon={PlusIcon} onClick={e => { append(e); e.preventDefault() }} className="self-start">追加</IconButton>
              </div>
            </Setting>
            <IconButton icon={BookmarkSquareIcon}>保存</IconButton>
          </form>
        </SettingSection>}

      {process.env.NODE_ENV === 'development' &&
        <SettingSection title="デバッグ用コマンド（開発環境でのみ有効）">
          <InlineMessageBar value={commandErrors} onChange={setCommandErrors} />
          <Setting label="DB再作成">
            <IconButton onClick={recreateDatabase}>実行</IconButton>
          </Setting>
        </SettingSection>}

    </div>
  )
}

const fieldValuesToObject = (data: FieldValues) => {
  const db = (data['db'] as { name: string, connStr: string }[]).map(({ name, connStr }) => ({ name, connStr }))
  const strCurrentDb = data['currentDb'] as string | null
  const numCurrentDb = strCurrentDb == null ? NaN : Number.parseInt(strCurrentDb)
  const currentDb = isNaN(numCurrentDb) ? null : db[numCurrentDb]?.name

  return { db, currentDb }
}

const objectToFieldValues = (obj: { [key: string]: {} }): any => {
  const db = obj['db'] as { name: string, connStr: string }[]
  const numCurrentDb = db.findIndex(item => item.name === obj['currentDb'])
  const currentDb = numCurrentDb === -1 ? null : String(numCurrentDb)

  return { db, currentDb }
}

const SettingSection = ({ title, children }: {
  title?: string
  children?: React.ReactNode
}) => {
  return (
    <section className="flex flex-col items-stretch space-y-2 border border-neutral-300 p-2">
      {title && <h2 className="text-sm opacity-50 font-semibold select-none">{title}</h2>}
      {children}
    </section>
  )
}

const Setting = ({ label, children, multiRow }: {
  label?: string
  children?: React.ReactNode
  multiRow?: true
}) => {
  return (
    <div className={`flex flex-row space-x-2 ${(multiRow ? 'items-start' : 'items-center')}`}>
      <span className="text-sm opacity-50 select-none basis-32">
        {label}
      </span>
      {children}
    </div>
  )
}

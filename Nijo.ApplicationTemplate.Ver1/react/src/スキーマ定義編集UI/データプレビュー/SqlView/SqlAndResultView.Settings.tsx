import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../../input"
import * as Layout from "../../../layout"
import * as Icon from "@heroicons/react/24/outline"
import useEvent from "react-use-event-hook"
import { SqlTextarea } from "../parts/SqlTextarea"
import * as UI from "../../UI"

export type SqlAndResultViewSettingsFormData = {
  title: string
  sql: string
}

export type SqlAndResultViewSettingsProps = {
  initialSettings: SqlAndResultViewSettingsFormData
  onApply: (updatedSettings: SqlAndResultViewSettingsFormData) => void
  onCancel: () => void
}

export const SqlAndResultViewSettings = ({ initialSettings, onApply, onCancel }: SqlAndResultViewSettingsProps) => {
  const formMethods = ReactHookForm.useForm<SqlAndResultViewSettingsFormData>({
    defaultValues: initialSettings,
  })
  const { control, handleSubmit, formState: { isDirty } } = formMethods

  const handleClickApply = useEvent(handleSubmit((data) => {
    onApply(data)
  }))

  const handleClickCancel = useEvent(() => {
    onCancel()
  })

  return (
    <UI.SettingDialog
      isDirty={isDirty}
      onApply={handleClickApply}
      onCancel={handleClickCancel}
      title="クエリ設定"
      className="w-[90vw] max-w-2xl h-auto"
    >
      <ReactHookForm.FormProvider {...formMethods}>
        <form onSubmit={handleClickApply} className="flex flex-col h-full">
          <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
            <div>
              <label className="block text-sm font-medium mb-1">名前</label>
              <ReactHookForm.Controller
                name="title"
                control={control}
                render={({ field }) => (
                  <input
                    {...field}
                    type="text"
                    className="w-full border border-gray-500 px-1 py-px"
                    placeholder="クエリの名前を入力"
                  />
                )}
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">SQL</label>
              <ReactHookForm.Controller
                name="sql"
                control={control}
                render={({ field }) => (
                  <SqlTextarea
                    {...field}
                    className="w-full h-48 border border-gray-500 p-1"
                    placeholder="SQLクエリを入力"
                  />
                )}
              />
            </div>
          </div>
        </form>
      </ReactHookForm.FormProvider>
    </UI.SettingDialog>
  )
}

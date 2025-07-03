import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../../input"
import * as Layout from "../../../layout"
import * as Icon from "@heroicons/react/24/outline"
import useEvent from "react-use-event-hook"
import { SqlTextarea } from "../UI/SqlTextarea"

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
    <Layout.ModalDialog
      open
      className="relative w-[90vw] max-w-2xl h-auto bg-white flex flex-col gap-1 relative border border-gray-400"
      onOutsideClick={handleClickCancel}
    >
      <ReactHookForm.FormProvider {...formMethods}>
        <form onSubmit={handleClickApply} className="flex flex-col h-full">
          <h1 className="font-bold select-none text-gray-700 px-4 py-2 border-b border-gray-200">
            クエリ設定
          </h1>

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

          <div className="flex justify-end items-center gap-2 py-2 px-4 border-t border-gray-200">
            <Input.IconButton onClick={handleClickCancel}>キャンセル</Input.IconButton>
            <Input.IconButton submit fill>適用</Input.IconButton>
          </div>
        </form>
      </ReactHookForm.FormProvider>
    </Layout.ModalDialog>
  )
}

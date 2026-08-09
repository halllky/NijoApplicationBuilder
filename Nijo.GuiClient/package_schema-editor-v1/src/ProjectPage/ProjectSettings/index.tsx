import React from "react";
import * as ReactHookForm from "react-hook-form";
import FormLayout, { LabelProps } from "@nijo/ui-components/layout/FormLayout";
import useEvent from "react-use-event-hook";
import { ProjectOptionPropertyInfo, EditingProject } from "../../backend";
import { usePersonalSettings } from "../../PersonalSettings";
import { PersonalSettings } from "../../PersonalSettings/PersonalSettings";
import { Allotment, LayoutPriority } from "allotment";
import { PreviewSection } from "./PreviewSection";
import { useSchemaEditorRule } from "../SchemaEditorRuleContext";

/**
 * プロジェクト設定タブの内容
 */
function ProjectSettings({ formMethods, projectDir }: {
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
  projectDir: string | null
}) {

  const { personalSettings, save } = usePersonalSettings()

  return (
    <Allotment proportionalLayout={false} separator={false}>
      {/* 目次 */}
      <Allotment.Pane preferredSize={200} className="border-r border-gray-400">
        <div className="h-full w-full overflow-y-auto bg-gray-50 p-2">
          <SideMenuLink hash="project-options">
            プロジェクト設定
          </SideMenuLink>
          <SideMenuLink hash="preview">
            プレビュー
          </SideMenuLink>
          <SideMenuLink hash="personal-settings">
            個人用設定
          </SideMenuLink>
        </div>
      </Allotment.Pane>

      {/* コンテンツ */}
      <Allotment.Pane priority={LayoutPriority.High}>
        <div className="p-4 h-full overflow-y-auto">
          <FormLayout.Root
            labelComponent={FormLayoutLabel}
            labelWidthPx={240}
          >
            <ProjectOptionsSection
              formMethods={formMethods}
            />

            <FormLayout.Separator />

            <PreviewSection
              formMethods={formMethods}
              projectDir={projectDir}
            />

            <FormLayout.Separator />

            <PersonalSettingSection
              personalSettings={personalSettings}
              save={save}
            />

            <div className="pb-96" />
          </FormLayout.Root>
        </div>
      </Allotment.Pane>
    </Allotment>
  )
}

export default React.memo(ProjectSettings)

/**
 * プロジェクト設定セクション
 */
const ProjectOptionsSection: React.FC<{
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
}> = ({ formMethods }) => {
  const { register } = formMethods
  const { projectOptionPropertyInfos } = useSchemaEditorRule()

  return (
    <FormLayout.Section labelEnd={(
      <div id="project-options" className="flex flex-col scroll-mt-2">
        <h2 className="text-lg font-bold">プロジェクト設定</h2>
        <span className="text-xs text-gray-600">
          プロジェクト全体に適用される設定項目
        </span>
      </div>
    )}>
      {projectOptionPropertyInfos.map((propInfo, index) => (
        <ProjectSettingField
          key={propInfo?.propertyName || index}
          propertyInfo={propInfo}
          register={register}
        />
      ))}
    </FormLayout.Section>
  )
}

/**
 * 個別の設定項目フィールド
 */
const ProjectSettingField: React.FC<{
  propertyInfo: ProjectOptionPropertyInfo
  register: ReactHookForm.UseFormRegister<any>
}> = ({ propertyInfo, register }) => {

  const fieldName: ReactHookForm.Path<EditingProject> = `projectOptions.${propertyInfo.propertyName}`

  return (
    <>
      <FormLayout.Field label={propertyInfo.propertyName}>
        <div className="flex flex-wrap items-start gap-x-2 gap-y-px my-1">
          {(() => {
            switch (propertyInfo.propertyType) {
              case 'bool':
                return (
                  <input
                    type="checkbox"
                    {...register(fieldName)}
                    className="h-4 w-4 my-1"
                  />
                )
              case 'int':
                return (
                  <input
                    type="number"
                    {...register(fieldName, {
                      valueAsNumber: true,
                      setValueAs: (value: string) => {
                        const numValue = parseInt(value);
                        return isNaN(numValue) ? 0 : numValue;
                      }
                    })}
                    placeholder={String(propertyInfo.defaultValue || '0')}
                    className="basis-80 px-1 py-px border border-gray-500"
                  />
                )
              default:
                return (
                  <input
                    type="text"
                    {...register(fieldName)}
                    placeholder={String(propertyInfo.defaultValue || '')}
                    className="basis-80 px-1 py-px border border-gray-500"
                  />
                )
            }
          })()}
          <span className="flex-1 min-w-80 text-xs text-gray-500">
            {propertyInfo.description}
          </span>
        </div>
      </FormLayout.Field>
    </>
  )
}


/**
 * 個人設定セクション
 */
const PersonalSettingSection: React.FC<{
  personalSettings: PersonalSettings
  save: <TPath extends ReactHookForm.Path<PersonalSettings>>(
    path: TPath,
    value: ReactHookForm.PathValue<PersonalSettings, TPath>
  ) => void
}> = ({ personalSettings, save }) => {

  const handleHideGridButtonsChange = useEvent((e: React.ChangeEvent<HTMLInputElement>) => {
    save('hideGridButtons', e.target.checked)
  })

  return (
    <FormLayout.Section labelEnd={(
      <div id="personal-settings" className="flex flex-col scroll-mt-2">
        <h2 className="text-lg font-bold">個人用設定</h2>
        <span className="text-xs text-gray-600">
          自身にのみ適用される設定項目
        </span>
      </div>
    )}>
      <FormLayout.Field label="hideGridButtons">
        <div className="flex flex-col gap-px my-2">
          <input
            type="checkbox"
            checked={personalSettings.hideGridButtons ?? false}
            onChange={handleHideGridButtonsChange}
            className="h-4 w-4"
          />
          <span className="text-xs text-gray-500">
            グリッドの操作説明ボタンを非表示にする
          </span>
        </div>
      </FormLayout.Field>
      <FormLayout.Field label="autoGenerateCode">
        <div className="flex flex-col gap-px my-2">
          <input
            type="checkbox"
            checked={personalSettings.autoGenerateCode ?? false}
            onChange={(e) => save('autoGenerateCode', e.target.checked)}
            className="h-4 w-4"
          />
          <span className="text-xs text-gray-500">
            保存時にソースコードの自動生成をかけ直す
          </span>
        </div>
      </FormLayout.Field>
    </FormLayout.Section>
  )
}

/** ルート集約の属性名の表示用 */
const FormLayoutLabel: React.ElementType<LabelProps> = ({ className, ...rest }) => {
  return (
    <FormLayout.DefaultLabel
      {...rest}
      className={`py-1 text-sm break-all ${className ?? ''}`}
    />
  )
}

/**
 * 目次のリンク
 */
function SideMenuLink({ hash, children }: {
  hash: string
  children?: React.ReactNode
}) {

  const handleClick = () => {
    const el = document.getElementById(hash)
    if (el) el.scrollIntoView({ block: 'start', behavior: 'smooth' })
  }

  return (
    <button type="button" onClick={handleClick} className="px-2 py-1 w-full text-sm text-left truncate hover:bg-gray-200 cursor-pointer select-none">
      {children}
    </button>
  )
}

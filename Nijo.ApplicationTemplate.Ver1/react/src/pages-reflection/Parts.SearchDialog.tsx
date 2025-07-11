import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import * as Input from "../input"
import { Panel, PanelResizeHandle } from "react-resizable-panels"
import { PanelGroup } from "react-resizable-panels"
import { MetadataForPage } from "../__autoGenerated/util/metadata-for-page"
import { ReflectionForm } from "./Parts.Form"
import { MetadataSchema } from "./getSchema"
import { FieldErrorView } from "../input"
import { ReflectionFormContext } from "./Parts.FormContext"

/**
 * 外部参照のコンポーネント（詳細画面用）
 */
export const RefTo = ({ control, valuesMemberPath, metadata }: {
  control: ReactHookForm.Control<ReactHookForm.FieldValues>
  /** react hook form の name属性のルールに従ったパス */
  valuesMemberPath: string
  metadata: MetadataForPage.RefMetadata
}) => {
  const { schema } = React.useContext(ReflectionFormContext)

  // 参照先の集約の主キー
  const refToKeys = React.useMemo(() => {
    const refToAggregateMetadata = schema.findRefTo(metadata.refTo)
    return Object
      .entries(refToAggregateMetadata.members)
      .filter(x => x[1].isKey)
  }, [metadata, schema])

  return (
    <ReactHookForm.Controller
      control={control}
      name={valuesMemberPath}
      render={({ field }) => (
        <div className="flex flex-col">
          <div className="flex justify-start gap-1">

            {/* キー項目のテキストボックス */}
            {refToKeys.map(([key, member]) => (
              <div key={key}>
                {/* TODO: 入力機能は後で作る */}
                <input
                  type="text"
                  placeholder={member.displayName}
                  className={`border border-gray-300 px-1 w-24 placeholder:text-gray-300 ${field.disabled ? '' : 'bg-white'}`}
                />
              </div>
            ))}

            {/* 虫眼鏡ボタン */}
            <Input.IconButton icon={Icon.MagnifyingGlassIcon} mini outline hideText>
              検索
            </Input.IconButton>
          </div>

          <FieldErrorView name={valuesMemberPath} />
        </div>
      )}
    />
  )
}

/**
 * 外部参照の検索条件用コンポーネント（検索条件用）
 */
export const RefToSearchCondition = ({ control, valuesMemberPath, metadata }: {
  control: ReactHookForm.Control<ReactHookForm.FieldValues>
  /** react hook form の name属性のルールに従ったパス */
  valuesMemberPath: string
  metadata: MetadataForPage.RefMetadata
}) => {

  const { schema } = React.useContext(ReflectionFormContext)

  // 参照先の集約の主キー
  const refToKeys = React.useMemo(() => {
    const refToAggregateMetadata = schema.findRefTo(metadata.refTo)
    return Object
      .entries(refToAggregateMetadata.members)
      .filter(x => x[1].isKey)
  }, [metadata, schema])

  return (
    <ReactHookForm.Controller
      control={control}
      name={valuesMemberPath}
      render={({ field }) => (
        <div className="flex flex-col">
          <div className="flex justify-start gap-1">

            {/* キー項目のテキストボックス */}
            {refToKeys.map(([key, member]) => (
              <div key={key}>
                {/* TODO: 入力機能は後で作る */}
                <input
                  type="text"
                  placeholder={member.displayName}
                  className={`border border-gray-300 px-1 w-24 placeholder:text-gray-300 ${field.disabled ? '' : 'bg-white'}`}
                />
              </div>
            ))}

            {/* 虫眼鏡ボタン */}
            <Input.IconButton icon={Icon.MagnifyingGlassIcon} mini outline hideText>
              検索
            </Input.IconButton>
          </div>

          <FieldErrorView name={valuesMemberPath} />
        </div>
      )}
    />
  )
}

/**
 * 外部参照の検索ダイアログのコンテンツを作成して返す。
 * ほぼMultiViewのコンテンツと同じだが、PageFrameは使わない。
 */
export const createSearchDialogContents = (
  metadata: MetadataForPage.RefMetadata,
  rootAggregatePhysicalName: string,
  schema: MetadataSchema,
  formMethods: ReactHookForm.UseFormReturn<ReactHookForm.FieldValues>
): React.JSX.Element => {

  const refToAggregateMetadata = schema.findRefTo(metadata.refTo)

  return (
    <div>
      <h1>SearchDialog</h1>

      <PanelGroup direction="vertical">
        <Panel collapsible minSize={8}>
          <div className="h-full overflow-y-auto">
            <ReflectionForm
              mode="search-condition"
              metadataPhysicalName={rootAggregatePhysicalName}
              metadata={refToAggregateMetadata}
              schema={schema}
              formMethods={formMethods}
            />
          </div>
        </Panel>

        <PanelResizeHandle className="h-1 bg-gray-300" />

        <Panel>

        </Panel>
      </PanelGroup>
    </div>
  )
}
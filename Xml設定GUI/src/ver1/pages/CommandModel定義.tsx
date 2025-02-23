import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import { UUID } from "uuidjs"
import { ApplicationData, CommandModel定義, 属性種類定義 } from "../types"
import { Outliner, PageFrame, DataTable2, useColumnDef, MarkdownTextarea, SectionTitle } from "../ui-components"

export default function () {
  const { key0 } = ReactRouter.useParams()
  const form = ReactHookForm.useFormContext<ApplicationData>()
  const commandModels = ReactHookForm.useFieldArray({ name: 'CommandModel定義', control: form.control })
  const [index, model] = React.useMemo(() => {
    const ix = commandModels.fields.findIndex(model => model.uniqueId === key0)
    return ix === -1
      ? [undefined, undefined]
      : [ix, commandModels.fields[ix]]
  }, [key0, commandModels])

  return (
    <PageFrame title={model?.表示名}>
      {index === undefined ? (
        <span>読み込み中...</span>
      ) : (
        <AfterLoaded index={index} rhfMethods={form} />
      )}
    </PageFrame>
  )
}

const AfterLoaded = ({ index, rhfMethods: { control } }: {
  index: number
  rhfMethods: ReactHookForm.UseFormReturn<ApplicationData>
}) => {

  const summary = ReactHookForm.useController({ name: `CommandModel定義.${index}.処理概要`, control })
  const trigger = ReactHookForm.useController({ name: `CommandModel定義.${index}.トリガー`, control })

  // パラメータ
  const parameterColumns = useColumnDef<ReactHookForm.FieldArrayWithId<ApplicationData, `CommandModel定義.${number}.パラメータ`>>(({ text, memberType }) => [
    text({ id: '00', getValue: row => row.表示名, setValue: (r, v) => r.表示名 = v }),
    memberType({ id: '01', header: '種類', getValue: r => r.属性種類UniqueId, setValue: (r, v) => r.属性種類UniqueId = v }),
  ])
  const createNewParameter = React.useCallback((): ReactHookForm.FieldArrayWithId<ApplicationData, `CommandModel定義.${number}.パラメータ`> => ({
    id: UUID.generate(),
    uniqueId: UUID.generate(),
    属性種類ごとの定義: {},
  }), [])

  // 戻り値
  const returnValueColumns = useColumnDef<ReactHookForm.FieldArrayWithId<ApplicationData, `CommandModel定義.${number}.戻り値`>>(({ text, memberType }) => [
    text({ id: '00', getValue: row => row.表示名, setValue: (r, v) => r.表示名 = v }),
    memberType({ id: '01', header: '種類', getValue: r => r.属性種類UniqueId, setValue: (r, v) => r.属性種類UniqueId = v }),
  ])
  const createNewReturnValue = React.useCallback((): ReactHookForm.FieldArrayWithId<ApplicationData, `CommandModel定義.${number}.戻り値`> => ({
    id: UUID.generate(),
    uniqueId: UUID.generate(),
    属性種類ごとの定義: {},
  }), [])

  // 処理詳細
  const detailColumnHeaders = React.useMemo(() => [
    '正常系処理',
    '異常系処理',
  ], [])

  return (
    <div className="grid grid-cols-[7rem,1fr] gap-y-2">

      <SectionTitle>
        処理概要
      </SectionTitle>
      <MarkdownTextarea
        {...summary.field}
        placeholder="処理概要を記載してください"
      />

      <SectionTitle>
        トリガー
      </SectionTitle>
      <MarkdownTextarea
        {...trigger.field}
        placeholder="この処理が実行されるタイミングを記載してください"
      />

      <DataTable2
        title="パラメータ"
        name={`CommandModel定義.${index}.パラメータ`}
        columns={parameterColumns}
        createNewItem={createNewParameter}
        control={control}
        className="col-span-full"
      />

      <DataTable2
        title="戻り値"
        name={`CommandModel定義.${index}.戻り値`}
        columns={returnValueColumns}
        createNewItem={createNewReturnValue}
        control={control}
        className="col-span-full"
      />

      <div className="flex flex-col col-span-full">
        <SectionTitle>
          処理詳細
        </SectionTitle>
        <Outliner
          name={`CommandModel定義.${index}.処理詳細`}
          className="col-span-full"
          columnHeaders={detailColumnHeaders}
          control={control}
        />
      </div>
    </div>
  )
}

const PropLabel = (props: {
  children?: React.ReactNode
}) => {
  return (
    <div className="py-1 select-none font-bold">
      {props.children}
    </div>
  )
}

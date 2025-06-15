import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import { UUID } from "uuidjs"
import { ApplicationData, DataModel定義, 属性種類定義 } from "../types"
import { Outliner, PageFrame, DataTable2, useColumnDef, MarkdownTextarea, SectionTitle } from "../ui-components"
import { SingleLineTextBox } from "../ui-components/SingleLineTextBox"

export default function () {
  const { key0 } = ReactRouter.useParams()
  const form = ReactHookForm.useFormContext<ApplicationData>()
  const commandModels = ReactHookForm.useFieldArray({ name: 'DataModel定義', control: form.control })
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

  const displayName = ReactHookForm.useController({ name: `DataModel定義.${index}.表示名`, control })
  const physicalName = ReactHookForm.useController({ name: `DataModel定義.${index}.物理名`, control })
  const dbName = ReactHookForm.useController({ name: `DataModel定義.${index}.DB名`, control })
  const latinName = ReactHookForm.useController({ name: `DataModel定義.${index}.ラテン名`, control })
  const lifeCycle = ReactHookForm.useController({ name: `DataModel定義.${index}.ライフサイクル定義`, control })

  // 項目定義
  const membersColumns = useColumnDef<ReactHookForm.FieldArrayWithId<ApplicationData, `DataModel定義.${number}.項目定義`>>(({ text, memberType }) => [
    text({ id: '00', getValue: row => row.表示名, setValue: (r, v) => r.表示名 = v }),
    memberType({ id: '01', header: '種類', getValue: r => r.属性種類UniqueId, setValue: (r, v) => r.属性種類UniqueId = v }),
  ])
  const createNewMember = React.useCallback((): ReactHookForm.FieldArrayWithId<ApplicationData, `DataModel定義.${number}.項目定義`> => ({
    id: UUID.generate(),
    uniqueId: UUID.generate(),
    属性種類ごとの定義: {},
  }), [])


  return (
    <div className="grid grid-cols-[7rem,1fr] gap-y-2">

      <SectionTitle>表示名</SectionTitle>
      <SingleLineTextBox {...displayName.field} placeholder="表示名を記載してください" />

      <SectionTitle>物理名</SectionTitle>
      <SingleLineTextBox {...physicalName.field} placeholder="物理名を記載してください" />

      <SectionTitle>DB名</SectionTitle>
      <SingleLineTextBox {...dbName.field} placeholder="DB名を記載してください" />

      <SectionTitle>ラテン名</SectionTitle>
      <SingleLineTextBox {...latinName.field} placeholder="ラテン名を記載してください" />

      <DataTable2
        title="項目定義"
        name={`DataModel定義.${index}.項目定義`}
        columns={membersColumns}
        createNewItem={createNewMember}
        control={control}
        className="col-span-full"
      />

      <SectionTitle className="col-span-full">
        キー以外のインデックス
      </SectionTitle>
      <div className="col-span-full text-color-5">
        なし
      </div>

      <SectionTitle className="col-span-full">
        ライフサイクル定義
      </SectionTitle>
      <MarkdownTextarea
        {...lifeCycle.field}
        placeholder="このデータ1件が生まれてから消えるまでの流れを記述してください。"
        className="col-span-full border border-color-4"
      />

      <Outliner
        name={`DataModel定義.${index}.ライフサイクル定義注釈`}
        className="col-span-full"
        hideHeader
        control={control}
      />

      <SectionTitle className="col-span-full">
        その他制約定義
      </SectionTitle>
      <Outliner
        name={`DataModel定義.${index}.その他制約定義`}
        className="col-span-full"
        hideHeader
        control={control}
      />
    </div>
  )
}

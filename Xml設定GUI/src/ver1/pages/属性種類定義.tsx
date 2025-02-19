import React from "react"
import * as ReactHookForm from "react-hook-form"
import { UUID } from "uuidjs"
import { ApplicationData, 属性種類定義 } from "../types"
import { Outliner, PageFrame, DataTable2, useColumnDef, MarkdownTextarea } from "../ui-components"

export default function () {

  const { control } = ReactHookForm.useFormContext<ApplicationData>()
  const zenpan = ReactHookForm.useController({ name: '属性種類定義.全般', control })

  return (
    <PageFrame title="属性項目定義">
      <MarkdownTextarea
        {...zenpan.field}
        placeholder="属性項目定義全般に関する仕様を書いてください"
        className="text-sm"
      />

      <hr className="border-t border-color-4" />
      <文字系属性 />

      <hr className="border-t border-color-4" />
      <数値系属性 />

      <hr className="border-t border-color-4" />
      <時間系属性 />

      <hr className="border-t border-color-4" />
      <区分系属性 />

      <hr className="border-t border-color-4" />
      <その他の属性 />
    </PageFrame>
  )
}

const 文字系属性 = () => {
  type GridRow = ReactHookForm.FieldArrayWithId<ApplicationData, '属性種類定義.文字系属性'>

  const { control } = ReactHookForm.useFormContext<ApplicationData>()

  const columns = useColumnDef<GridRow>(({ text, specification }) => [
    text({ id: '00', getValue: row => row.型名, setValue: (r, v) => r.型名 = v }),
    text({ id: '01', header: '説明', defaultWidthPx: 240, getValue: r => r.説明, setValue: (r, v) => r.説明 = v }),
    specification({ id: '02', headerGroupName: '型', header: 'C#', getValue: r => r.型["C#"], setValue: (r, v) => r.型["C#"] = v }),
    specification({ id: '03', headerGroupName: '型', header: 'DB', getValue: r => r.型.DB, setValue: (r, v) => r.型.DB = v }),
    specification({ id: '04', headerGroupName: '型', header: 'TypeScript', getValue: row => row.型.TypeScript, setValue: (r, v) => r.型.TypeScript = v }),
    specification({ id: '05', headerGroupName: '制約', header: 'MaxLength', getValue: r => r.制約.MaxLength, setValue: (r, v) => r.制約.MaxLength = v }),
    specification({ id: '06', headerGroupName: '制約', header: '文字種', getValue: r => r.制約.文字種, setValue: (r, v) => r.制約.文字種 = v }),
    specification({ id: '07', headerGroupName: '検索処理定義', header: '検索処理の挙動', getValue: r => r.検索処理定義.検索処理の挙動, setValue: (r, v) => r.検索処理定義.検索処理の挙動 = v }),
    specification({ id: '08', headerGroupName: '検索処理定義', header: '全文検索の対象', getValue: r => r.検索処理定義.全文検索の対象か否か, setValue: (r, v) => r.検索処理定義.全文検索の対象か否か = v }),
    specification({ id: '09', headerGroupName: 'ノーマライズ', header: 'UIフォーカスアウト時', getValue: r => r.ノーマライズ.UIフォーカスアウト時, setValue: (r, v) => r.ノーマライズ.UIフォーカスアウト時 = v }),
    specification({ id: '10', headerGroupName: 'ノーマライズ', header: '登録時', getValue: r => r.ノーマライズ.登録時, setValue: (r, v) => r.ノーマライズ.登録時 = v }),
  ])

  return (
    <>
      <DataTable2
        title="文字系属性"
        name="属性種類定義.文字系属性"
        control={control}
        columns={columns}
        createNewItem={createNew文字系属性}
      />
      <Outliner
        name="属性種類定義.文字系属性注釈"
        control={control}
      />
    </>
  )
}

const 数値系属性 = () => {
  type GridRow = ReactHookForm.FieldArrayWithId<ApplicationData, '属性種類定義.数値系属性'>

  const { control } = ReactHookForm.useFormContext<ApplicationData>()

  const columns = useColumnDef<GridRow>(({ text, specification }) => [
    text({ id: '00', getValue: row => row.型名, setValue: (r, v) => r.型名 = v }),
    text({ id: '01', header: '説明', defaultWidthPx: 240, getValue: r => r.説明, setValue: (r, v) => r.説明 = v }),
    specification({ id: '02', headerGroupName: '型', header: 'C#', getValue: r => r.型["C#"], setValue: (r, v) => r.型["C#"] = v }),
    specification({ id: '03', headerGroupName: '型', header: 'DB', getValue: r => r.型.DB, setValue: (r, v) => r.型.DB = v }),
    specification({ id: '04', headerGroupName: '型', header: 'TypeScript', getValue: row => row.型.TypeScript, setValue: (r, v) => r.型.TypeScript = v }),
    specification({ id: '05', headerGroupName: '制約', header: 'トータル桁数', getValue: r => r.制約.トータル桁数, setValue: (r, v) => r.制約.トータル桁数 = v }),
    specification({ id: '06', headerGroupName: '制約', header: '小数部桁数', getValue: r => r.制約.小数部桁数, setValue: (r, v) => r.制約.小数部桁数 = v }),
    specification({ id: '07', headerGroupName: '表示形式', header: '3桁毎カンマ有無', getValue: r => r.表示形式["3桁毎カンマ有無"], setValue: (r, v) => r.表示形式["3桁毎カンマ有無"] = v }),
    specification({ id: '08', headerGroupName: '表示形式', header: 'プレフィックス/サフィックス', getValue: r => r.表示形式["prefix/suffix"], setValue: (r, v) => r.表示形式["prefix/suffix"] = v }),
    specification({ id: '09', headerGroupName: '表示形式', header: '負の数の表現', getValue: r => r.表示形式.負の数の表現, setValue: (r, v) => r.表示形式.負の数の表現 = v }),
    specification({ id: '10', headerGroupName: '検索処理定義', header: '検索処理の挙動', getValue: r => r.検索処理定義.検索処理の挙動, setValue: (r, v) => r.検索処理定義.検索処理の挙動 = v }),
    specification({ id: '11', headerGroupName: '検索処理定義', header: '全文検索の対象', getValue: r => r.検索処理定義.全文検索の対象か否か, setValue: (r, v) => r.検索処理定義.全文検索の対象か否か = v }),
    specification({ id: '12', headerGroupName: 'ノーマライズ', header: 'UIフォーカスアウト時', getValue: r => r.ノーマライズ.UIフォーカスアウト時, setValue: (r, v) => r.ノーマライズ.UIフォーカスアウト時 = v }),
    specification({ id: '13', headerGroupName: 'ノーマライズ', header: '登録時', getValue: r => r.ノーマライズ.登録時, setValue: (r, v) => r.ノーマライズ.登録時 = v }),
  ])

  return (
    <>
      <DataTable2
        title="数値系属性"
        name="属性種類定義.数値系属性"
        control={control}
        columns={columns}
        createNewItem={createNew数値系属性}
      />
      <Outliner
        name="属性種類定義.数値系属性注釈"
        control={control}
      />
    </>
  )
}

const 時間系属性 = () => {
  type GridRow = ReactHookForm.FieldArrayWithId<ApplicationData, '属性種類定義.時間系属性'>

  const { control } = ReactHookForm.useFormContext<ApplicationData>()

  const columns = useColumnDef<GridRow>(({ text, specification }) => [
    text({ id: '00', getValue: row => row.型名, setValue: (r, v) => r.型名 = v }),
    text({ id: '01', header: '説明', defaultWidthPx: 240, getValue: r => r.説明, setValue: (r, v) => r.説明 = v }),
    specification({ id: '02', headerGroupName: '型', header: 'C#', getValue: r => r.型["C#"], setValue: (r, v) => r.型["C#"] = v }),
    specification({ id: '03', headerGroupName: '型', header: 'DB', getValue: r => r.型.DB, setValue: (r, v) => r.型.DB = v }),
    specification({ id: '04', headerGroupName: '型', header: 'TypeScript', getValue: row => row.型.TypeScript, setValue: (r, v) => r.型.TypeScript = v }),
    specification({ id: '05', headerGroupName: '制約', header: '値の範囲', getValue: r => r.制約.値の範囲, setValue: (r, v) => r.制約.値の範囲 = v }),
    specification({ id: '10', headerGroupName: '検索処理定義', header: '検索処理の挙動', getValue: r => r.検索処理定義.検索処理の挙動, setValue: (r, v) => r.検索処理定義.検索処理の挙動 = v }),
    specification({ id: '11', headerGroupName: '検索処理定義', header: '全文検索の対象', getValue: r => r.検索処理定義.全文検索の対象か否か, setValue: (r, v) => r.検索処理定義.全文検索の対象か否か = v }),
    specification({ id: '12', headerGroupName: 'ノーマライズ', header: 'UIフォーカスアウト時', getValue: r => r.ノーマライズ.UIフォーカスアウト時, setValue: (r, v) => r.ノーマライズ.UIフォーカスアウト時 = v }),
    specification({ id: '13', headerGroupName: 'ノーマライズ', header: '登録時', getValue: r => r.ノーマライズ.登録時, setValue: (r, v) => r.ノーマライズ.登録時 = v }),
  ])

  return (
    <>
      <DataTable2
        title="時間系属性"
        name="属性種類定義.時間系属性"
        control={control}
        columns={columns}
        createNewItem={createNew時間系属性}
      />
      <Outliner
        name="属性種類定義.時間系属性注釈"
        control={control}
      />
    </>
  )
}

const 区分系属性 = () => {
  type GridRow = ReactHookForm.FieldArrayWithId<ApplicationData, '属性種類定義.区分系属性'>

  const { control } = ReactHookForm.useFormContext<ApplicationData>()

  const columns = useColumnDef<GridRow>(({ text, specification }) => [
    text({ id: '00', getValue: row => row.型名, setValue: (r, v) => r.型名 = v }),
    text({ id: '01', header: '説明', defaultWidthPx: 240, getValue: r => r.説明, setValue: (r, v) => r.説明 = v }),
    specification({ id: '02', headerGroupName: '型', header: 'C#', getValue: r => r.型["C#"], setValue: (r, v) => r.型["C#"] = v }),
    specification({ id: '03', headerGroupName: '型', header: 'DB', getValue: r => r.型.DB, setValue: (r, v) => r.型.DB = v }),
    specification({ id: '04', headerGroupName: '型', header: 'TypeScript', getValue: row => row.型.TypeScript, setValue: (r, v) => r.型.TypeScript = v }),
    specification({ id: '05', headerGroupName: '区分の種類', header: '区分の種類', getValue: r => r.区分の種類, setValue: (r, v) => r.区分の種類 = v }),
    specification({ id: '10', headerGroupName: '検索処理定義', header: '検索処理の挙動', getValue: r => r.検索処理定義.検索処理の挙動, setValue: (r, v) => r.検索処理定義.検索処理の挙動 = v }),
    specification({ id: '11', headerGroupName: '検索処理定義', header: '全文検索の対象', getValue: r => r.検索処理定義.全文検索の対象か否か, setValue: (r, v) => r.検索処理定義.全文検索の対象か否か = v }),
  ])

  return (
    <>
      <DataTable2
        title="区分系属性"
        name="属性種類定義.区分系属性"
        control={control}
        columns={columns}
        createNewItem={createNew区分系属性}
      />
      <Outliner
        name="属性種類定義.区分系属性注釈"
        control={control}
      />
    </>
  )
}

const その他の属性 = () => {
  type GridRow = ReactHookForm.FieldArrayWithId<ApplicationData, '属性種類定義.その他の属性'>

  const { control } = ReactHookForm.useFormContext<ApplicationData>()

  const columns = useColumnDef<GridRow>(({ text, specification }) => [
    text({ id: '00', getValue: row => row.型名, setValue: (r, v) => r.型名 = v }),
    text({ id: '01', header: '説明', defaultWidthPx: 240, getValue: r => r.説明, setValue: (r, v) => r.説明 = v }),
    specification({ id: '02', headerGroupName: '型', header: 'C#', getValue: r => r.型["C#"], setValue: (r, v) => r.型["C#"] = v }),
    specification({ id: '03', headerGroupName: '型', header: 'DB', getValue: r => r.型.DB, setValue: (r, v) => r.型.DB = v }),
    specification({ id: '04', headerGroupName: '型', header: 'TypeScript', getValue: row => row.型.TypeScript, setValue: (r, v) => r.型.TypeScript = v }),
    specification({ id: '05', headerGroupName: '制約', header: '制約', getValue: r => r.制約, setValue: (r, v) => r.制約 = v }),
    specification({ id: '10', headerGroupName: '検索処理定義', header: '検索処理の挙動', getValue: r => r.検索処理定義.検索処理の挙動, setValue: (r, v) => r.検索処理定義.検索処理の挙動 = v }),
    specification({ id: '11', headerGroupName: '検索処理定義', header: '全文検索の対象', getValue: r => r.検索処理定義.全文検索の対象か否か, setValue: (r, v) => r.検索処理定義.全文検索の対象か否か = v }),
  ])

  return (
    <>
      <DataTable2
        title="その他の属性"
        name="属性種類定義.その他の属性"
        control={control}
        columns={columns}
        createNewItem={createNewその他の属性}
      />
      <Outliner
        name="属性種類定義.その他の属性注釈"
        control={control}
      />
    </>
  )
}

// *****************************************

const createNew文字系属性 = (): ReactHookForm.FieldArrayWithId<属性種類定義.Whole, '文字系属性'> => ({
  id: UUID.generate(),
  uniqueId: UUID.generate(),
  ノーマライズ: {},
  制約: {},
  型: {},
  検索処理定義: {},
})

const createNew数値系属性 = (): ReactHookForm.FieldArrayWithId<属性種類定義.Whole, '数値系属性'> => ({
  id: UUID.generate(),
  uniqueId: UUID.generate(),
  ノーマライズ: {},
  制約: {},
  型: {},
  表示形式: {},
  検索処理定義: {},
})

const createNew時間系属性 = (): ReactHookForm.FieldArrayWithId<属性種類定義.Whole, '時間系属性'> => ({
  id: UUID.generate(),
  uniqueId: UUID.generate(),
  ノーマライズ: {},
  制約: {},
  型: {},
  検索処理定義: {},
})

const createNew区分系属性 = (): ReactHookForm.FieldArrayWithId<属性種類定義.Whole, '区分系属性'> => ({
  id: UUID.generate(),
  uniqueId: UUID.generate(),
  型: {},
  検索処理定義: {},
})

const createNewその他の属性 = (): ReactHookForm.FieldArrayWithId<属性種類定義.Whole, 'その他の属性'> => ({
  id: UUID.generate(),
  uniqueId: UUID.generate(),
  型: {},
  検索処理定義: {},
})

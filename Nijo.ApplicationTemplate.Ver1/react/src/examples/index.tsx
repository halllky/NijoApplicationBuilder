import React from 'react'
import * as ReactRouter from 'react-router-dom'
import * as ReactHookForm from 'react-hook-form'
import * as Input from '../input'
import * as Layout from '../layout'
import { RouteObjectWithSideMenuSetting } from '../routes'
import { WordExample } from './WordExample'
import { NumberInputExample } from './NumberInputExample'
import { HyperLinkExample } from './HyperLinkExample'
import { IconButtonExample } from './IconButtonExample'
import { DateInputExample } from './DateInputExample'
import { EditableGridExample } from './EditableGridExample'
import { VFormExample } from './VFormExample'
import { PageFrameExample } from './PageFrameExample'

export const getExamplePagesRoutes = (): RouteObjectWithSideMenuSetting => ({
  path: "/examples/",
  element: <ReactRouter.Outlet />,
  sideMenuLabel: "【開発用】UI実装例",
  children: [
    { index: true, element: <ComponentExampleIndex /> },
    { path: "word", element: <WordExample /> },
    { path: "number-input", element: <NumberInputExample /> },
    { path: "hyperlink", element: <HyperLinkExample /> },
    { path: "icon-button", element: <IconButtonExample /> },
    { path: "date-input", element: <DateInputExample /> },
    { path: "editable-grid", element: <EditableGridExample /> },
    { path: "vform", element: <VFormExample /> },
    { path: "page-frame", element: <PageFrameExample /> },
  ]
})

/** UIコンポーネントへのリンク一覧を表示するコンポーネント */
export default function ComponentExampleIndex() {

  const rhfMethods = ReactHookForm.useForm<ExampleIndexFormType>({
    defaultValues: getDefaultValues(),
  })
  const { control } = rhfMethods
  const hyperLink = ReactHookForm.useWatch({ control, name: 'hyperlink' })

  return (
    <Layout.PageFrame
      headerContent={(
        <Layout.PageFrameTitle>
          UIコンポーネント仕様書兼実装例カタログ
        </Layout.PageFrameTitle>
      )}
      className="flex flex-col gap-4 p-2"
    >
      <p className="text-sm">
        各コンポーネントの詳細な仕様や実装例はそれぞれのリンク先画面を参照してください。
      </p>
      <div className="flex flex-col gap-4 px-12 py-2">
        <h2 className="text-sm text-gray-500">入力フォーム</h2>
        <LinkAndBasicExample title="文字列入力" to="word">
          <Input.Word name="word" control={control} />
        </LinkAndBasicExample>
        <LinkAndBasicExample title="数値入力" to="number-input">
          <Input.NumberInput name="number" control={control} />
        </LinkAndBasicExample>
        <LinkAndBasicExample title="日付入力" to="date-input">
          <Input.DateInput name="date" control={control} />
        </LinkAndBasicExample>
        <LinkAndBasicExample title="ボタン" to="icon-button">
          <div className="flex flex-wrap gap-2">
            <Input.IconButton>ボタン例</Input.IconButton>
            <Input.IconButton fill>ボタン例</Input.IconButton>
            <Input.IconButton outline>ボタン例</Input.IconButton>
          </div>
        </LinkAndBasicExample>
        <LinkAndBasicExample title="ハイパーリンク" to="hyperlink">
          <Input.HyperLink href={hyperLink ?? ''}>リンク例</Input.HyperLink>
        </LinkAndBasicExample>

        <hr className="border-gray-300" />

        <h2 className="text-sm text-gray-500">レイアウト</h2>
        <LinkAndBasicExample title="ページフレーム" to="page-frame">
          <span className="text-sm text-gray-500">
            ※サンプル割愛※
          </span>
        </LinkAndBasicExample>
        <LinkAndBasicExample title="グリッドレイアウト" to="editable-grid">
          <ComponentExampleIndex_EditableGrid rhfMethods={rhfMethods} />
        </LinkAndBasicExample>
        <LinkAndBasicExample title="フォームレイアウト" to="vform">
          <ComponentExampleIndex_VForm rhfMethods={rhfMethods} />
        </LinkAndBasicExample>
      </div>
    </Layout.PageFrame>
  )
}

const LinkAndBasicExample = (props: {
  title: string
  to: string
  children?: React.ReactNode
}) => {
  return (
    <div className="flex items-start gap-2 ml-8">
      <Input.HyperLink to={props.to} className="basis-40">
        {props.title}
      </Input.HyperLink>
      {props.children}
    </div>
  )
}

// --------------------------

type ExampleIndexFormType = {
  word?: string
  number?: number
  date?: string
  hyperlink?: string
  editableGrid: ExampleIndexFormChildrenType[]
  vform: {
    名前: string
    年齢: number
    住所: string
  }
}
type ExampleIndexFormChildrenType = {
  id?: string
  name?: string
  age?: number
}
const getDefaultValues = (): ExampleIndexFormType => ({
  word: 'ABC',
  number: 123,
  date: '2025/01/01',
  hyperlink: 'http://example.com',
  editableGrid: [
    { id: '1', name: '山田太郎', age: 20 },
    { id: '2', name: '佐藤花子', age: 25 },
  ],
  vform: {
    名前: '山田太郎',
    年齢: 20,
    住所: '東京都千代田区永田町1-7-1',
  },
})

// --------------------------

const ComponentExampleIndex_EditableGrid = ({ rhfMethods }: {
  rhfMethods: ReactHookForm.UseFormReturn<ExampleIndexFormType>
}) => {

  const { fields } = ReactHookForm.useFieldArray({
    control: rhfMethods.control,
    name: 'editableGrid',
  })
  const getColumnDefs: Layout.GetColumnDefsFunction<ExampleIndexFormChildrenType> = React.useCallback(cellType => [
    cellType.text('id', 'ID'),
    cellType.text('name', '名前'),
    cellType.number('age', '年齢'),
  ], [])

  return (
    <Layout.EditableGrid
      rows={fields}
      getColumnDefs={getColumnDefs}
    />
  )
}

// --------------------------

const ComponentExampleIndex_VForm = ({ rhfMethods: { control } }: {
  rhfMethods: ReactHookForm.UseFormReturn<ExampleIndexFormType>
}) => {

  return (
    <Layout.VForm3.Root labelWidth="4rem" className="border border-gray-300 p-2">
      <Layout.VForm3.BreakPoint>
        <Layout.VForm3.Item label="名前">
          <Input.Word name="vform.名前" control={control} />
        </Layout.VForm3.Item>
        <Layout.VForm3.Item label="年齢">
          <Input.NumberInput name="vform.年齢" control={control} />
        </Layout.VForm3.Item>
        <Layout.VForm3.Item label="住所">
          <Input.Word name="vform.住所" control={control} />
        </Layout.VForm3.Item>
      </Layout.VForm3.BreakPoint>
    </Layout.VForm3.Root>
  )
}

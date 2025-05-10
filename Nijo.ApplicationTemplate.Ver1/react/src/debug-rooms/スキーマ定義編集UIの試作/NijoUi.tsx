import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactResizablePanels from "react-resizable-panels"
import * as Layout from "../../layout"
import * as Input from "../../input"
import useEvent from "react-use-event-hook"
import { ApplicationState } from "./types"
import { NijoUiSideMenu } from "./NijoUiSideMenu"
import { PageRootAggregate } from "./NijoUi.RootAggregate"
import { AttrDefsProvider } from "./useAttrDefs"

const SERVER_DOMAIN = import.meta.env.DEV
  ? 'https://localhost:8081'
  : ''

/**
 * nijo.xmlをUIで編集できる画面の試作。
 * 最終的には、独立した WebView2 アプリケーションとして分離する予定。
 */
export const NijoUi = ({ className }: {
  className?: string
}) => {

  // 画面初期表示時、サーバーからスキーマ情報を読み込む
  const [schema, setSchema] = React.useState<ApplicationState>()
  const [loadError, setLoadError] = React.useState<string>()
  const load = useEvent(async () => {
    try {
      // Visual Studio で Nijo.csproj の run-ui-service コマンドを実行したときのポート
      const response = await fetch(`${SERVER_DOMAIN}/load`)
      const schema = await response.json()
      setSchema(schema)
    } catch (error) {
      console.error(error)
      setLoadError(error instanceof Error ? error.message : `不明なエラー(${error})`)
    }
  })
  React.useEffect(() => {
    load()
  }, [])

  // 保存処理
  const handleSave = useEvent(async (applicationState: ApplicationState) => {
    try {
      const response = await fetch(`${SERVER_DOMAIN}/save`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(applicationState),
      })
      if (!response.ok) {
        const body = await response.json() as string[]
        console.error(body)
        window.alert(`保存に失敗しました:\n${body.join('\n')}`)
        return
      }
      window.alert('保存に成功しました')
    } catch (error) {
      console.error(error)
      window.alert(`保存に失敗しました: ${error instanceof Error ? error.message : `不明なエラー(${error})`}`)
    }
  })

  // 読み込み中
  if (schema === undefined && loadError === undefined) {
    return <Layout.NowLoading />
  }

  // 読み込み完了
  if (schema !== undefined) {
    return (
      <AfterLoaded
        defaultValues={schema}
        onSave={handleSave}
        className={className}
      />
    )
  }

  // 上記以外は読み込みエラーとみなす
  return (
    <div className={className}>
      読み込みでエラーが発生しました: {loadError}
    </div>
  )
}

/** 画面初期表示時の読み込み完了後 */
const AfterLoaded = ({ defaultValues, onSave, className }: {
  defaultValues: ApplicationState
  onSave: (applicationState: ApplicationState) => void
  className?: string
}) => {

  const form = ReactHookForm.useForm<ApplicationState>({
    defaultValues: defaultValues,
  })

  // 選択中のルート集約
  const [selectedRootAggregateIndex, setSelectedRootAggregateIndex] = React.useState<number | null>(null)
  const [selectedRootAggregateId, setSelectedRootAggregateId] = React.useState<string | undefined>(undefined)
  const handleSelected = useEvent((rootAggregateIndex: number) => {
    setSelectedRootAggregateIndex(rootAggregateIndex)
    setSelectedRootAggregateId(form.getValues(`xmlElementTrees.${rootAggregateIndex}.xmlElements.0.id`))
  })

  return (
    <AttrDefsProvider control={form.control}>
      <ReactResizablePanels.PanelGroup direction="horizontal" className={className}>

        {/* サイドメニュー */}
        <ReactResizablePanels.Panel defaultSize={20}>
          <NijoUiSideMenu
            onSave={onSave}
            formMethods={form}
            selectedRootAggregateId={selectedRootAggregateId}
            onSelected={handleSelected}
          />
        </ReactResizablePanels.Panel>

        <ReactResizablePanels.PanelResizeHandle />

        {/* メインコンテンツ */}
        <ReactResizablePanels.Panel>
          {selectedRootAggregateIndex !== null && (
            <PageRootAggregate
              key={selectedRootAggregateIndex}
              rootAggregateIndex={selectedRootAggregateIndex}
              formMethods={form}
              className="pl-1 pt-1"
            />
          )}
        </ReactResizablePanels.Panel>

      </ReactResizablePanels.PanelGroup>
    </AttrDefsProvider>
  )
}

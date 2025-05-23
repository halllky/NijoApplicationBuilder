import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import * as ReactResizablePanels from "react-resizable-panels"
import * as Layout from "../layout"
import * as Input from "../input"
import useEvent from "react-use-event-hook"
import { ApplicationState, NijoUiOutletContextType } from "./スキーマ定義編集/types"
import { NijoUiSideMenu } from "./NijoUiSideMenu"
import { PageRootAggregate } from "./スキーマ定義編集/RootAggregatePage"
import { AttrDefsProvider } from "./スキーマ定義編集/AttrDefContext"
import { getNavigationUrl, NIJOUI_CLIENT_ROUTE_PARAMS } from "./routing"
import NijoUiErrorMessagePane from "./NijoUiErrorMessagePane"
import { useValidationContextProvider, ValidationContext } from "./スキーマ定義編集/ValidationContext"

export const SERVER_DOMAIN = import.meta.env.DEV
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

  const form = ReactHookForm.useForm<ApplicationState>({ defaultValues: defaultValues })
  const validationContext = useValidationContextProvider(form.getValues)

  // 選択中のルート集約
  const navigate = ReactRouter.useNavigate()
  const urlParams = ReactRouter.useParams()
  const selectedRootAggregateId = urlParams[NIJOUI_CLIENT_ROUTE_PARAMS.AGGREGATE_ID]
  const handleSelected = useEvent((rootAggregateIndex: number) => {
    const aggregateId = form.getValues(`xmlElementTrees.${rootAggregateIndex}.xmlElements.0.uniqueId`)
    navigate(getNavigationUrl({ aggregateId }))
  })

  // Outletコンテキストの値
  const outletContextValue: NijoUiOutletContextType = React.useMemo(() => ({
    formMethods: form,
    validationContext,
    selectedRootAggregateId,
  }), [form, validationContext, selectedRootAggregateId])

  return (
    <AttrDefsProvider control={form.control}>
      <ValidationContext.Provider value={validationContext}>
        <ReactResizablePanels.PanelGroup direction="vertical" className={className}>

          <ReactResizablePanels.Panel>
            <ReactResizablePanels.PanelGroup direction="horizontal">

              {/* サイドメニュー */}
              <ReactResizablePanels.Panel defaultSize={20} minSize={8} collapsible>
                <NijoUiSideMenu
                  onSave={onSave}
                  formMethods={form}
                  selectedRootAggregateId={selectedRootAggregateId}
                  onSelected={handleSelected}
                />
              </ReactResizablePanels.Panel>

              <ReactResizablePanels.PanelResizeHandle className="w-1" />

              {/* メインコンテンツ */}
              <ReactResizablePanels.Panel>
                <ReactRouter.Outlet context={outletContextValue} />
              </ReactResizablePanels.Panel>

            </ReactResizablePanels.PanelGroup>
          </ReactResizablePanels.Panel>

          <ReactResizablePanels.PanelResizeHandle className="h-2" />

          {/* エラーメッセージ表示欄 */}
          <ReactResizablePanels.Panel defaultSize={10} minSize={6} collapsible>
            <NijoUiErrorMessagePane
              getValues={form.getValues}
              validationResult={validationContext.validationResult}
              className="h-full"
            />
          </ReactResizablePanels.Panel>

        </ReactResizablePanels.PanelGroup>
      </ValidationContext.Provider>
    </AttrDefsProvider>
  )
}

// ----------------------------

/** Outlet経由で表示されるメインコンテンツエリアのコンポーネント */
export const NijoUiMainContent = () => {

  const { formMethods, selectedRootAggregateId } = ReactRouter.useOutletContext<NijoUiOutletContextType>()
  const xmlElementTrees = ReactHookForm.useWatch({ name: 'xmlElementTrees', control: formMethods.control })
  const selectedRootAggregateIndex = React.useMemo((): number | undefined => {
    if (!selectedRootAggregateId) return undefined;
    if (!xmlElementTrees) return undefined; // 初期ロード時など xmlElementTrees が未定義の場合
    return xmlElementTrees.findIndex(tree => tree.xmlElements[0].uniqueId === selectedRootAggregateId);
  }, [selectedRootAggregateId, xmlElementTrees]);

  if (selectedRootAggregateId === undefined) {
    return (
      <div className="p-1 text-sm text-gray-500">
        左側のメニューから項目を選択してください。
      </div>
    );
  }

  if (selectedRootAggregateIndex === -1) {
    return (
      <div className="p-1 text-sm text-gray-500">
        対象の集約が見つかりません。（ID: {selectedRootAggregateId}）
      </div>
    );
  }

  if (selectedRootAggregateIndex !== undefined && selectedRootAggregateIndex !== -1) {
    return (
      <PageRootAggregate
        key={selectedRootAggregateId} // URL更新のたびに再描画させる
        rootAggregateIndex={selectedRootAggregateIndex}
        formMethods={formMethods}
        className="pl-1 pt-1"
      />
    );
  }

  return null; // 上記以外は何も表示しない
};

import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactResizablePanels from "react-resizable-panels"
import * as Layout from "../../layout"
import * as Input from "../../input"
import useEvent from "react-use-event-hook"
import { ApplicationState } from "./types"
import { NijoUiSideMenu } from "./NijoUiSideMenu"
import { getDefaultValues } from "./getDefaultValues"
import { PageRootAggregate } from "./Page.RootAggregate"

/**
 * nijo.xmlをUIで編集できる画面の試作。
 * 最終的には、独立した WebView2 アプリケーションとして分離する予定。
 */
export const NijoUi = ({ className }: {
  className?: string
}) => {

  const form = ReactHookForm.useForm<ApplicationState>({
    defaultValues: getDefaultValues(),
  })

  // 選択中のルート集約
  const [selectedRootAggregateIndex, setSelectedRootAggregateIndex] = React.useState<number | null>(null)
  const handleSelected = useEvent((rootAggregateIndex: number) => {
    setSelectedRootAggregateIndex(rootAggregateIndex)
  })

  return (
    <ReactResizablePanels.PanelGroup direction="horizontal" className={className}>

      {/* サイドメニュー */}
      <ReactResizablePanels.Panel defaultSize={20}>
        <NijoUiSideMenu formMethods={form} onSelected={handleSelected} />
      </ReactResizablePanels.Panel>

      <ReactResizablePanels.PanelResizeHandle />

      {/* メインコンテンツ */}
      <ReactResizablePanels.Panel>
        {selectedRootAggregateIndex !== null && (
          <PageRootAggregate
            key={selectedRootAggregateIndex}
            rootAggregateIndex={selectedRootAggregateIndex}
            formMethods={form}
            className="p-1"
          />
        )}
      </ReactResizablePanels.Panel>

    </ReactResizablePanels.PanelGroup>
  )
}
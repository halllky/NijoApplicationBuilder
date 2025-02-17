import * as ReactHookForm from "react-hook-form"
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import 属性種類定義 from "./pages/属性種類定義"
import { ApplicationData } from "./types"
import { getDefaultSpecification } from "./DEFAULT_DATA"

import "./index.css"

export default function () {

  const form = ReactHookForm.useForm<ApplicationData>({
    defaultValues: getDefaultSpecification(),
  })

  return (
    <ReactHookForm.FormProvider {...form}>
      <PanelGroup direction="horizontal" className="h-full w-full">
        <Panel className="bg-color-1 border-r border-color-3" defaultSize={20}>

        </Panel>

        <PanelResizeHandle className="w-2" />

        <Panel>
          <属性種類定義 />
        </Panel>
      </PanelGroup>
    </ReactHookForm.FormProvider>
  )
}

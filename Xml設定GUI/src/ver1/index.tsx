import * as ReactHookForm from "react-hook-form"
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import 属性種類定義 from "./pages/属性種類定義"
import { ApplicationData } from "./types"
import { getDefaultSpecification } from "./DEFAULT_DATA"

import "./index.css"
import { Outliner, OutlinerItem } from "./ui-components"
import { UUID } from "uuidjs"

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
          {/* <OutlinerTest /> */}
        </Panel>
      </PanelGroup>
    </ReactHookForm.FormProvider>
  )
}



export const OutlinerTest = () => {
  const { control } = ReactHookForm.useForm<{ items?: OutlinerItem[] }>({
    defaultValues: { items: getOutlinerTestData() },
  })

  return (
    <Outliner
      name="items"
      control={control}
      className="h-full"
    />
  )
}

const getOutlinerTestData = (): OutlinerItem[] => [
  { uniqueId: UUID.generate(), indent: 0, text: 'aaa' },
  { uniqueId: UUID.generate(), indent: 0, text: 'bbb' },
  { uniqueId: UUID.generate(), indent: 1, bullet: '・', text: 'ccc' },
  { uniqueId: UUID.generate(), indent: 1, bullet: '・', text: 'dd' },
  { uniqueId: UUID.generate(), indent: 2, bullet: '※1', text: 'eeee' },
  { uniqueId: UUID.generate(), indent: 2, bullet: '※2', text: 'f' },
  { uniqueId: UUID.generate(), indent: 0, text: 'agaa' },
]

import { PageFrame, PageFrameTitle } from "../../layout";
import { NijoUi } from "./NijoUi";

export default function () {
  return (
    <PageFrame
      headerContent={(
        <PageFrameTitle>スキーマ定義編集UI</PageFrameTitle>
      )}
      className="p-8"
    >
      <NijoUi className="h-full w-full border border-gray-500" />
    </PageFrame>
  )
}

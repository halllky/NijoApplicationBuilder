import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import { UUID } from "uuidjs"
import {
  EditingProject,
  EditingRootAggregate,
  MODEL_CONSTANT,
} from "../../backend"
import * as UI from '../../UI'
import { SingleConstantEditor } from "./SingleConstantEditor"

/**
 * 定数定義グリッド
 *
 * 複数の定数定義をリスト形式で表示し、それぞれを編集可能にする。
 */
function ConstantsGrid(props: {
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
}) {
  const { control, setValue, getValues } = props.formMethods
  const constants = ReactHookForm.useWatch({
    control,
    name: "constants"
  }) ?? []

  const handleAddConstant = () => {
    const newConstant: EditingRootAggregate = {
      uniqueId: UUID.generate(),
      physicalName: "",
      model: MODEL_CONSTANT,
      attributes: {},
      uniqueConstraints: [],
      members: [],
    }

    // 末尾に追加
    const current = getValues("constants") ?? []
    setValue("constants", [...current, newConstant])
  }

  return (
    <div className="flex flex-col gap-2 py-2 p-4">
      {constants.map((constant, index) => (
        <SingleConstantEditor
          key={constant.uniqueId ?? index}
          index={index}
          formMethods={props.formMethods}
        />
      ))}
      <div>
        <UI.Button icon={Icon.PlusIcon} onClick={handleAddConstant}>
          新しい定数定義ブロックを追加
        </UI.Button>
      </div>
    </div>
  )
}

export default React.memo(ConstantsGrid)

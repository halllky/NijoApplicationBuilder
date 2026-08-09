import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/solid"
import { UUID } from "uuidjs"
import {
  GeneratedProjectInGui,
  ATTR_TYPE,
  TYPE_CONSTANT_MODEL,
  XmlElementItem,
} from "../../types"
import * as UI from '../../UI'
import { SingleConstantEditor } from "./SingleConstantEditor"

/**
 * 定数定義グリッド
 *
 * 複数の定数定義をリスト形式で表示し、それぞれを編集可能にする。
 */
function ConstantsGrid(props: {
  formMethods: ReactHookForm.UseFormReturn<GeneratedProjectInGui>
}) {
  const { control, setValue, getValues } = props.formMethods
  const xmlElementTrees = ReactHookForm.useWatch({
    control,
    name: "xmlElementTrees"
  }) ?? []

  // 定数定義のインデックスのみを抽出
  const constantIndexes = React.useMemo(() => {
    return xmlElementTrees
      .map((tree, index) => ({ tree, index }))
      .filter(({ tree }) => tree.xmlElements?.[0]?.attributes?.[ATTR_TYPE] === TYPE_CONSTANT_MODEL)
      .map(({ index }) => index)
  }, [xmlElementTrees])

  const handleAddConstant = () => {
    const newConstant: XmlElementItem = {
      uniqueId: UUID.generate(),
      indent: 0,
      localName: "",
      value: undefined,
      attributes: { [ATTR_TYPE]: TYPE_CONSTANT_MODEL },
      comment: undefined,
    }
    const newTree = { xmlElements: [newConstant] }

    // 末尾に追加
    const current = getValues("xmlElementTrees") ?? []
    setValue("xmlElementTrees", [...current, newTree])
  }

  return (
    <div className="flex flex-col gap-2 py-2 p-4">
      {constantIndexes.map(index => (
        <SingleConstantEditor
          key={xmlElementTrees[index].xmlElements[0]?.uniqueId ?? index}
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

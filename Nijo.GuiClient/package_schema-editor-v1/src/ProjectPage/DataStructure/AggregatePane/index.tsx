import React from "react";
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import { ATTR_TYPE, ATTR_IS_GENERIC_LOOKUP_TABLE, GeneratedProjectInGui, TYPE_COMMAND_MODEL } from "../../../types";
import * as UI from "../../../UI"
import { Allotment, LayoutPriority } from "allotment";
import DecsendantsGrid from "./DecsendantsGrid";
import RootAggregateAttrs from "./RootAggregateAttrs";
import GenericLookupTableCategoriesPane from "./GenericLookupTableCategoriesPane";

/**
 * ルート集約1個分の編集ペイン
 */
function AggregatePane(props: {
  selectedRootAggregateIndex: number
  formMethods: ReactHookForm.UseFormReturn<GeneratedProjectInGui>
  className?: string
  onRequestDelete?: () => void
  orientation?: 'horizontal' | 'vertical'
  onSwitchOrientation?: () => void
}) {
  const {
    selectedRootAggregateIndex,
    formMethods: { register, getValues, control },
    className,
    onRequestDelete,
    orientation,
    onSwitchOrientation,
  } = props

  const handleDelete = () => {
    const localName = getValues(`xmlElementTrees.${selectedRootAggregateIndex}.xmlElements.0.localName`)
    if (!confirm(`「${localName}」を削除しますか？`)) return
    onRequestDelete?.()
  }

  const rootAggregateModelType = ReactHookForm.useWatch({ name: `xmlElementTrees.${selectedRootAggregateIndex}.xmlElements.0.attributes.${ATTR_TYPE}`, control })
  const isGenericLookupTable = ReactHookForm.useWatch({ name: `xmlElementTrees.${selectedRootAggregateIndex}.xmlElements.0.attributes.${ATTR_IS_GENERIC_LOOKUP_TABLE}`, control }) === 'True'

  return (
    <div className={`flex flex-col gap-1 ${className ?? ''}`}>

      {/* ヘッダ */}
      <div className="flex flex-wrap items-center gap-1 p-1">

        {/* 分割方向切り替え */}
        {onSwitchOrientation && (
          <UI.Button
            icon={orientation === 'vertical' ? Icon.ArrowsRightLeftIcon : Icon.ArrowsUpDownIcon}
            mini
            hideText
            onClick={onSwitchOrientation}
          >
            分割方向切り替え
          </UI.Button>
        )}

        {/* ルート集約名 */}
        {/* TODO: GUI上ではDisplayNameを編集し、LocalNameへの変換はサーバー側で行うようにする */}
        <UI.WordTextBox
          {...register(`xmlElementTrees.${selectedRootAggregateIndex}.xmlElements.0.localName`)}
          className="flex-1 font-bold"
        />

        {/* モデル */}
        <ReactHookForm.Controller
          control={control}
          name={`xmlElementTrees.${selectedRootAggregateIndex}.xmlElements.0.attributes.${ATTR_TYPE}`}
          render={({ field }) => (
            <UI.ModelTypeSelector {...field} className="min-w-48" />
          )}
        />

        {/* 削除ボタン */}
        <UI.Button icon={Icon.TrashIcon} mini hideText onClick={handleDelete}>
          削除
        </UI.Button>
      </div>

      <Allotment
        vertical
        proportionalLayout={false} // 一部のペインのみ伸縮するようにする
        className="px-1"
      >
        {/* ルート集約の属性 */}
        <Allotment.Pane preferredSize={120} snap minSize={80}>
          <div className="w-full h-full p-1 bg-gray-200 border-t border-x border-gray-300 overflow-auto">
            <RootAggregateAttrs
              selectedRootAggregateIndex={selectedRootAggregateIndex}
              formMethods={props.formMethods}
              className="w-full"
            />
          </div>
        </Allotment.Pane>

        {/* 子孫集約 */}
        <Allotment.Pane
          priority={LayoutPriority.High}
          minSize={80}
          visible={rootAggregateModelType !== TYPE_COMMAND_MODEL}
        >
          <DecsendantsGrid
            selectedRootAggregateIndex={selectedRootAggregateIndex}
            formMethods={props.formMethods}
            className="w-full h-full py-1"
          />
        </Allotment.Pane>

        {/* 汎用参照テーブルのカテゴリ定義 */}
        {isGenericLookupTable && (
          <Allotment.Pane
            preferredSize={200}
            minSize={80}
          >
            <div className="w-full h-full p-1 bg-gray-100 border-t border-x border-gray-300 overflow-auto">
              <GenericLookupTableCategoriesPane
                selectedRootAggregateIndex={selectedRootAggregateIndex}
                formMethods={props.formMethods}
                className="w-full"
              />
            </div>
          </Allotment.Pane>
        )}
      </Allotment>

    </div>
  )
}

export default React.memo(AggregatePane)

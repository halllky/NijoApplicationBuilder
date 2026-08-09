import { Allotment, LayoutPriority } from "allotment";
import React from "react";
import * as ReactHookForm from "react-hook-form"
import { usePersonalSettings } from "../../PersonalSettings";
import { ATTR_TYPE, RootAggregateXmlTree, GeneratedProjectInGui } from "../../types";
import { Button } from "../../UI";
import { Diagram, DiagramRef } from "./Diagram";
import AggregatePane from "./AggregatePane";
import { NewRootAddDialog } from "./NewRootAddDialog";
import { UUID } from "uuidjs";
import { PlusIcon } from "@heroicons/react/24/solid";

export type DataStructureTabRef = {
  selectRootAggregate: (rootOrDescendantXmlElementUniqueId: string | undefined | null) => void
}

/**
 * データ構造定義タブ。
 *
 * スキーマ定義の有向グラフを表示する。
 * 特定のルート集約が選択されている場合はその集約の編集ペインを表示する。
 */
function DataStructureTab({ visible, formMethods, dataStructureRef, diagramRef }: {
  visible: boolean
  formMethods: ReactHookForm.UseFormReturn<GeneratedProjectInGui>
  dataStructureRef: React.RefObject<DataStructureTabRef | null>
  diagramRef: React.RefObject<DiagramRef | null>
}) {

  // 個人設定
  const { personalSettings, save: savePersonalSettings } = usePersonalSettings()

  // 選択中のルート集約
  const [selectedRootAggregateIndex, setSelectedRootAggregateIndex] = React.useState<number | undefined>(undefined);
  const selectRootAggregate = (rootOrDescendantXmlElementUniqueId: string | undefined | null, scroll: boolean) => {
    if (!rootOrDescendantXmlElementUniqueId) {
      setSelectedRootAggregateIndex(undefined)
      setAggPaneVisible(false)
      return;
    }

    // ルート集約編集ペインの表示
    const xmlElementTrees = formMethods.getValues("xmlElementTrees")
    const index = xmlElementTrees?.findIndex(tree => tree.xmlElements?.some(el => el.uniqueId === rootOrDescendantXmlElementUniqueId))
    if (index === undefined || index === -1) return;
    setSelectedRootAggregateIndex(index)
    setAggPaneVisible(true)

    // ダイアグラムで当該ノードを選択して表示領域の中央に移動する。
    // 詳細ペインの表示でダイアグラムの中心座標が変わるので少し待つ。
    // またルート集約の新規作成のタイミングでのフォーカスだとまだ配列中に存在しないので待機後にルート集約をとりなおす
    if (scroll) {
      window.setTimeout(() => {
        if (!diagramRef.current?.graphViewRef.current) return;
        const rootUniqueId = formMethods.getValues("xmlElementTrees")
          ?.find(tree => tree.xmlElements?.some(el => el.uniqueId === rootOrDescendantXmlElementUniqueId))
          ?.xmlElements?.[0].uniqueId
        if (!rootUniqueId) return;

        diagramRef.current.graphViewRef.current.panToNode(rootUniqueId)
      }, 300)
    }
  }
  React.useImperativeHandle(dataStructureRef, () => ({
    selectRootAggregate: id => selectRootAggregate(id, true),
  }))

  // 選択中のルート集約を画面右側に表示する
  const [aggPaneVisible, setAggPaneVisible] = React.useState(false)
  const aggPaneOrientation = personalSettings.aggPaneOrientation ?? 'horizontal'
  const handleSwitchAggPaneOrientation = React.useCallback(() => {
    savePersonalSettings('aggPaneOrientation', aggPaneOrientation === 'horizontal' ? 'vertical' : 'horizontal')
  }, [aggPaneOrientation, savePersonalSettings])

  // 新規ルート集約作成ダイアログ
  const { append, remove } = ReactHookForm.useFieldArray({ name: "xmlElementTrees", control: formMethods.control })
  const [isNewRootDialogOpen, setIsNewRootDialogOpen] = React.useState(false)
  const handleRegisterNewRoot = (name: string, modelType: string) => {
    const previousLength = formMethods.getValues("xmlElementTrees")?.length ?? 0
    const newRoot: RootAggregateXmlTree = {
      xmlElements: [{
        uniqueId: UUID.generate(),
        indent: 0,
        localName: name,
        attributes: { [ATTR_TYPE]: modelType },
      }],
    }

    append(newRoot)
    setIsNewRootDialogOpen(false)

    // 新規作成したルート集約を選択状態にする
    // レンダリングを待つためにsetTimeoutを入れる
    setTimeout(() => {
      selectRootAggregate(newRoot.xmlElements[0].uniqueId, true)
      setSelectedRootAggregateIndex(previousLength) // append後の長さはlength+1なのでindexはlengthになる
      setAggPaneVisible(true)
    }, 0)
  }

  // ルート集約削除
  const handleDeleteRootAggregate = React.useCallback(() => {
    if (selectedRootAggregateIndex !== undefined) {
      remove(selectedRootAggregateIndex)
    }
    setSelectedRootAggregateIndex(undefined)
    setAggPaneVisible(false)
  }, [remove, selectedRootAggregateIndex])

  return (
    <Allotment
      key={aggPaneOrientation} // 配置方向変更時にAllotmentを再生成してレイアウトをリセット
      vertical={aggPaneOrientation === 'vertical'}
      proportionalLayout={false} // 特定のペインだけ伸縮させる
      separator={false}
      className={visible ? "" : "hidden"}
    >
      {/* ダイアグラム */}
      <Allotment.Pane priority={LayoutPriority.High}>
        <Diagram
          formMethods={formMethods}
          onSelectedRootAggregateChanged={id => selectRootAggregate(id, false)}
          diagramRef={diagramRef}
          className="h-full w-full"
        >
          <Button onClick={() => setIsNewRootDialogOpen(true)} icon={PlusIcon} fill>
            新規作成
          </Button>
          <NewRootAddDialog
            open={isNewRootDialogOpen}
            onClose={() => setIsNewRootDialogOpen(false)}
            onRegister={handleRegisterNewRoot}
          />
        </Diagram>
      </Allotment.Pane>

      {/* ルート集約編集ペイン */}
      <Allotment.Pane preferredSize="50%" visible={aggPaneVisible}>
        {selectedRootAggregateIndex !== undefined && (
          <AggregatePane
            key={selectedRootAggregateIndex}
            selectedRootAggregateIndex={selectedRootAggregateIndex}
            formMethods={formMethods}
            className={`h-full w-full border-gray-400 ${aggPaneOrientation === 'vertical' ? 'border-t' : 'border-l'}`}
            onRequestDelete={handleDeleteRootAggregate}
            orientation={aggPaneOrientation}
            onSwitchOrientation={handleSwitchAggPaneOrientation}
          />
        )}
      </Allotment.Pane>
    </Allotment>
  )
}

export default React.memo(DataStructureTab)

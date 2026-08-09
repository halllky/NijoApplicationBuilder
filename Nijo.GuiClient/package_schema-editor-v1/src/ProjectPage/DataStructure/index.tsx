import { Allotment, LayoutPriority } from "allotment";
import React from "react";
import * as ReactHookForm from "react-hook-form"
import { usePersonalSettings } from "../../PersonalSettings";
import { EditingProject, EditingRootAggregate, MODEL_COMMAND } from "../../backend";
import { Button } from "../../UI";
import { Diagram, DiagramRef } from "./Diagram";
import AggregatePane from "./AggregatePane";
import { NewRootAddDialog } from "./NewRootAddDialog";
import { UUID } from "uuidjs";
import { PlusIcon } from "@heroicons/react/24/solid";
import { RootAggregateLocation, findRootAggregateLocation } from "../rootAggregateLocation";

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
  formMethods: ReactHookForm.UseFormReturn<EditingProject>
  dataStructureRef: React.RefObject<DataStructureTabRef | null>
  diagramRef: React.RefObject<DiagramRef | null>
}) {

  // 個人設定
  const { personalSettings, save: savePersonalSettings } = usePersonalSettings()

  // 選択中のルート集約
  const [rootLocation, setRootLocation] = React.useState<RootAggregateLocation | undefined>(undefined);
  const selectRootAggregate = (rootOrDescendantUniqueId: string | undefined | null, scroll: boolean) => {
    if (!rootOrDescendantUniqueId) {
      setRootLocation(undefined)
      setAggPaneVisible(false)
      return;
    }

    // ルート集約編集ペインの表示
    const location = findRootAggregateLocation(formMethods.getValues(), rootOrDescendantUniqueId)
    if (!location) return;
    setRootLocation(location)
    setAggPaneVisible(true)

    // ダイアグラムで当該ノードを選択して表示領域の中央に移動する。
    // 詳細ペインの表示でダイアグラムの中心座標が変わるので少し待つ。
    // またルート集約の新規作成のタイミングでのフォーカスだとまだ配列中に存在しないので待機後にルート集約をとりなおす
    if (scroll) {
      window.setTimeout(() => {
        if (!diagramRef.current?.graphViewRef.current) return;
        const freshProject = formMethods.getValues()
        const freshLocation = findRootAggregateLocation(freshProject, rootOrDescendantUniqueId)
        if (!freshLocation) return;
        const rootUniqueId = freshProject[freshLocation.list][freshLocation.index]?.uniqueId
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
  const dataStructuresArray = ReactHookForm.useFieldArray({ name: "dataStructures", control: formMethods.control })
  const commandsArray = ReactHookForm.useFieldArray({ name: "commands", control: formMethods.control })
  const [isNewRootDialogOpen, setIsNewRootDialogOpen] = React.useState(false)
  const handleRegisterNewRoot = (name: string, modelType: string) => {
    const list: RootAggregateLocation['list'] = modelType === MODEL_COMMAND ? 'commands' : 'dataStructures'
    const targetArray = list === 'dataStructures' ? dataStructuresArray : commandsArray
    const previousLength = formMethods.getValues(list)?.length ?? 0
    const newUniqueId = UUID.generate()
    const newRoot: EditingRootAggregate = {
      uniqueId: newUniqueId,
      physicalName: name,
      model: modelType,
      attributes: {},
      uniqueConstraints: [],
      members: [],
    }

    targetArray.append(newRoot)
    setIsNewRootDialogOpen(false)

    // 新規作成したルート集約を選択状態にする
    // レンダリングを待つためにsetTimeoutを入れる
    setTimeout(() => {
      selectRootAggregate(newUniqueId, true)
      setRootLocation({ list, index: previousLength }) // append後の長さはlength+1なのでindexはlengthになる
      setAggPaneVisible(true)
    }, 0)
  }

  // ルート集約削除
  const handleDeleteRootAggregate = React.useCallback(() => {
    if (rootLocation !== undefined) {
      if (rootLocation.list === 'dataStructures') dataStructuresArray.remove(rootLocation.index)
      else commandsArray.remove(rootLocation.index)
    }
    setRootLocation(undefined)
    setAggPaneVisible(false)
  }, [dataStructuresArray, commandsArray, rootLocation])

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
        {rootLocation !== undefined && (
          <AggregatePane
            key={`${rootLocation.list}-${rootLocation.index}`}
            rootLocation={rootLocation}
            formMethods={formMethods}
            className={`h-full w-full border-gray-400 ${aggPaneOrientation === 'vertical' ? 'border-t' : 'border-l'}`}
            onRequestDelete={handleDeleteRootAggregate}
            onRootLocationChanged={setRootLocation}
            orientation={aggPaneOrientation}
            onSwitchOrientation={handleSwitchAggPaneOrientation}
          />
        )}
      </Allotment.Pane>
    </Allotment>
  )
}

export default React.memo(DataStructureTab)

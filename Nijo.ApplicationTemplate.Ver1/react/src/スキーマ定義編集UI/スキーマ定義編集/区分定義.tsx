import React from "react"
import useEvent from "react-use-event-hook"
import * as Icon from "@heroicons/react/24/outline"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../input"
import * as Layout from "../../layout"
import * as Util from "../../util"
import { asTree, ATTR_DISPLAY_NAME, ATTR_TYPE, SchemaDefinitionGlobalState, TYPE_STATIC_ENUM_MODEL, TYPE_VALUE_OBJECT_MODEL, XmlElementItem } from "./types"
import { useSaveLoad } from "./useSaveLoad"
import { PageFrame } from "../PageFrame"
import { useValidation } from "./useValidation"
import { createAttributeCell, createLocalNameCell, GridRowType } from "./index.Grid"
import NijoUiErrorMessagePane from "./index.ErrorMessage"
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import { UUID } from "uuidjs"
import { usePersonalSettings } from "../PersonalSettings"

/**
 * 静的区分、値オブジェクトの定義を行なう。
 */
export const 区分定義 = () => {
  const {
    schema,
    loadError,
    reloadSchema,
    saveSchema,
  } = useSaveLoad()

  // 読み込みエラー
  if (loadError) {
    return (
      <div>
        読み込みでエラーが発生しました: {loadError}
        <Input.IconButton onClick={reloadSchema}>
          再読み込み
        </Input.IconButton>
      </div>
    )
  }

  // 読み込み中
  if (schema === undefined) {
    return <Layout.NowLoading />
  }

  return (
    <AfterLoaded
      formDefaultValues={schema}
      reloadSchema={reloadSchema}
      executeSave={saveSchema}
    />
  )
}

/**
 * 静的区分の編集欄
 */
const AfterLoaded = ({ formDefaultValues, reloadSchema, executeSave }: {
  formDefaultValues: SchemaDefinitionGlobalState
  reloadSchema: () => Promise<void>
  executeSave: (values: SchemaDefinitionGlobalState) => Promise<{ ok: boolean, error?: string }>
}) => {

  const formMethods = ReactHookForm.useForm<{ staticEnums: XmlElementItem[] }>()
  const { getValues, control, formState: { isDirty } } = formMethods
  const { fields, insert, remove, update, append, move } = ReactHookForm.useFieldArray({ name: 'staticEnums', control })

  // defaultValues が変わるたび（初回表示時、保存時）にデフォルト値を更新する
  React.useEffect(() => {
    const result: XmlElementItem[] = []
    for (const modelPageForm of formDefaultValues.xmlElementTrees) {
      const root = modelPageForm.xmlElements[0]
      const type = root?.attributes[ATTR_TYPE]
      if (type === TYPE_STATIC_ENUM_MODEL) {
        result.push(...modelPageForm.xmlElements.map(el => ({
          ...el,
          id: el.uniqueId,
        })))
      }
    }
    formMethods.reset({ staticEnums: result })
  }, [formDefaultValues])


  // 編集中の最新の値と defaultValues を結合し、スキーマ定義全体を取得する
  const getFullSchema = useGetFullSchema(formDefaultValues, getValues)

  // バリデーション
  const { getValidationResult, trigger, validationResultList } = useValidation(getFullSchema)

  // 静的区分のグリッドの列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<GridRowType> = React.useCallback(cellType => {
    const columns: Layout.EditableGridColumnDef<GridRowType>[] = []

    // LocalName
    columns.push(createLocalNameCell(cellType, getValidationResult))

    // Attributes（Type以外）
    for (const attrDef of Array.from(formDefaultValues.attributeDefs.values())) {
      // Typeは既に表示しているのでスキップ
      if (attrDef.attributeName === ATTR_TYPE) continue;

      // 静的区分で設定可能な属性のみをフィルタリング
      if (!attrDef.availableModels.includes(TYPE_STATIC_ENUM_MODEL)) continue;

      columns.push({
        ...createAttributeCell(attrDef, cellType, getValidationResult),
        // 区分種類はDisplayName以外編集不可
        isReadOnly: attrDef.attributeName === ATTR_DISPLAY_NAME
          ? undefined
          : (cell => cell.indent === 0),
      })
    }

    return columns
  }, [formDefaultValues, getValidationResult])

  const handleChangeRow: Layout.RowChangeEvent<GridRowType> = useEvent(e => {
    for (const { rowIndex, newRow } of e.changedRows) {
      update(rowIndex, newRow)
    }
  })

  // 保存
  const [saveButtonText, setSaveButtonText] = React.useState('保存(Ctrl + S)')
  const [nowSaving, setNowSaving] = React.useState(false)
  const [saveError, setSaveError] = React.useState<string>()
  const handleSave = useEvent(async () => {
    if (nowSaving) return;
    trigger()
    setSaveError(undefined)
    setNowSaving(true)
    const currentValues = getFullSchema()
    const result = await executeSave(currentValues)
    if (result.ok) {
      await reloadSchema()
      setSaveButtonText('保存しました。')
      window.setTimeout(() => {
        setSaveButtonText('保存(Ctrl + S)')
      }, 2000)
    } else {
      setSaveError(result.error)
    }
    setNowSaving(false)
  })
  Util.useCtrlS(handleSave)

  const staticEnumGridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null)

  // 区分値を挿入
  const handleInsertStaticEnumValue = useEvent(() => {
    const selectedFirstRow = staticEnumGridRef.current?.getSelectedRows()[0]
    if (selectedFirstRow === undefined) return;

    // 先頭には挿入不可
    if (selectedFirstRow.rowIndex === 0) return;

    insert(selectedFirstRow.rowIndex, {
      uniqueId: UUID.generate(),
      indent: 1,
      attributes: {},
    })
  })

  // 区分値を下挿入
  const handleInsertStaticEnumValueBelow = useEvent(() => {
    const selectedFirstRow = staticEnumGridRef.current?.getSelectedRows()[0]
    if (selectedFirstRow === undefined) return;

    insert(selectedFirstRow.rowIndex + 1, {
      uniqueId: UUID.generate(),
      indent: 1,
      attributes: {},
    })
  })

  // 区分値を削除
  const handleDeleteStaticEnumValue = useEvent(() => {
    const selectedRows = staticEnumGridRef.current?.getSelectedRows()
    if (selectedRows === undefined) return;

    const removeRowIndexes: number[] = []
    for (const x of selectedRows) {
      // 区分種類は削除不可
      if (x.row.indent === 0) continue;

      removeRowIndexes.push(x.rowIndex)
    }
    remove(removeRowIndexes)
  })

  // 区分種類を作成
  const handleCreateStaticEnum = useEvent(() => {
    const newName = window.prompt('新しい区分種類の名前を入力してください。')
    if (!newName) return;

    const newEnum: XmlElementItem = {
      uniqueId: UUID.generate(),
      localName: newName,
      indent: 0,
      attributes: {
        [ATTR_TYPE]: TYPE_STATIC_ENUM_MODEL,
      },
    }
    append(newEnum)
  })

  // 区分種類を削除
  const handleDeleteStaticEnum = useEvent(() => {
    const selectedRow = staticEnumGridRef.current?.getSelectedRows()[0]
    if (selectedRow === undefined) return;

    const treeHelper = asTree(fields)
    const root = treeHelper.getRoot(selectedRow.row)

    if (!window.confirm(`「${root.localName}」を削除しますか？`)) return;

    const tree = [root, ...treeHelper.getDescendants(root)]
    const removeRowIndexes = tree.map(x => fields.indexOf(x))
    remove(removeRowIndexes)
  })

  // 区分値を上移動
  const handleMoveUpStaticEnumValue = useEvent(() => {
    const selectedRows = staticEnumGridRef.current?.getSelectedRows()
    if (selectedRows === undefined) return;

    // 区分種類は移動不可
    if (selectedRows.some(x => x.row.indent === 0)) return;

    // 移動元・移動先のインデックスを計算する。
    // 選択範囲の上にある行を選択範囲の最後の位置に移動させることで上移動を実現する。
    const moveFrom = selectedRows[0].rowIndex - 1
    const moveTo = selectedRows[selectedRows.length - 1].rowIndex

    // 区分種類は移動不可
    if (fields[moveFrom] === undefined || fields[moveFrom].indent === 0) return;

    move(moveFrom, moveTo)
    staticEnumGridRef.current?.selectRow(moveFrom, moveTo - 1)
  })

  // 区分値を下移動
  const handleMoveDownStaticEnumValue = useEvent(() => {
    const selectedRows = staticEnumGridRef.current?.getSelectedRows()
    if (selectedRows === undefined) return;

    // 区分種類は移動不可
    if (selectedRows.some(x => x.row.indent === 0)) return;

    // 移動元・移動先のインデックスを計算する。
    // 選択範囲の下にある行を選択範囲の最初の位置に移動させることで下移動を実現する。
    const moveFrom = selectedRows[selectedRows.length - 1].rowIndex + 1
    const moveTo = selectedRows[0].rowIndex

    // 区分種類は移動不可
    if (fields[moveFrom] === undefined || fields[moveFrom].indent === 0) return;

    move(moveFrom, moveTo)
    staticEnumGridRef.current?.selectRow(moveTo + 1, moveFrom)
  })

  // 静的区分のグリッドのキー操作
  const handleKeyDown: Layout.EditableGridKeyboardEventHandler = useEvent(e => {
    if (e.key === 'Enter') {
      if (e.ctrlKey || e.metaKey) {
        handleInsertStaticEnumValueBelow()
      } else {
        handleInsertStaticEnumValue()
      }
      return { handled: true }
    }
    if (e.key === 'Delete' && e.shiftKey) {
      handleDeleteStaticEnumValue()
      return { handled: true }
    }
    if (e.key === 'ArrowUp' && e.altKey) {
      handleMoveUpStaticEnumValue()
      return { handled: true }
    }
    if (e.key === 'ArrowDown' && e.altKey) {
      handleMoveDownStaticEnumValue()
      return { handled: true }
    }
    return { handled: false }
  })

  const { personalSettings } = usePersonalSettings()

  return (
    <PageFrame
      title="区分定義"
      shouldBlock={isDirty}
      headerComponent={(
        <>
          <div className="flex-1"></div>
          <div className="basis-36 flex justify-end">
            <Input.IconButton fill onClick={handleSave} loading={nowSaving}>{saveButtonText}</Input.IconButton>
          </div>
        </>
      )}
    >
      <PanelGroup direction="vertical">
        <Panel defaultSize={80} className="flex flex-col gap-1">
          <div className="flex flex-wrap items-center gap-1">
            <h2 className="text-sm select-none">静的区分</h2>
            {!personalSettings.hideGridButtons && (
              <>
                <div className="basis-4"></div>
                <Input.IconButton outline mini onClick={handleInsertStaticEnumValue}>
                  区分値を挿入(Enter)
                </Input.IconButton>
                <Input.IconButton outline mini onClick={handleInsertStaticEnumValueBelow}>
                  区分値を下挿入(Ctrl + Enter)
                </Input.IconButton>
                <Input.IconButton outline mini onClick={handleDeleteStaticEnumValue}>
                  区分値を削除(Shift + Delete)
                </Input.IconButton>
                <Input.IconButton outline mini onClick={handleMoveUpStaticEnumValue}>
                  上移動(Alt + ↑)
                </Input.IconButton>
                <Input.IconButton outline mini onClick={handleMoveDownStaticEnumValue}>
                  下移動(Alt + ↓)
                </Input.IconButton>
              </>
            )}
            <div className="basis-4"></div>
            <Input.IconButton outline mini onClick={handleCreateStaticEnum}>
              新しい種類を作成
            </Input.IconButton>
            <Input.IconButton outline mini onClick={handleDeleteStaticEnum}>
              種類を削除
            </Input.IconButton>
          </div>

          {/* 静的区分: 保存エラー */}
          {saveError && (
            <div className="text-rose-500 text-sm">
              {saveError}
            </div>
          )}

          {/* 静的区分編集グリッド */}
          <Layout.EditableGrid
            ref={staticEnumGridRef}
            rows={fields}
            getColumnDefs={getColumnDefs}
            onChangeRow={handleChangeRow}
            onKeyDown={handleKeyDown}
            className="border border-gray-300"
          />
        </Panel>

        <PanelResizeHandle className="h-2" />

        <Panel>

        </Panel>

      </PanelGroup>

      {/* バリデーションエラー */}
      <NijoUiErrorMessagePane
        getValues={getFullSchema}
        validationResultList={validationResultList}
        selectRootAggregate={undefined}
      />
    </PageFrame>
  )
}

// ---------------------------------

/** 編集中の最新の値と defaultValues を結合し、スキーマ定義全体を取得する */
const useGetFullSchema = (
  formDefaultValues: SchemaDefinitionGlobalState,
  getValues: ReactHookForm.UseFormGetValues<{ staticEnums: XmlElementItem[] }>
) => {
  return React.useCallback((): SchemaDefinitionGlobalState => {
    // defaultValues を処理しやすい形に変換
    const clone = window.structuredClone(formDefaultValues)
    const rootIdIndexMap = new Map(clone.xmlElementTrees.map((tree, index) => [tree.xmlElements[0]?.uniqueId ?? '', index]))

    const latestValues = getValues('staticEnums') ?? []
    const treeHelper = asTree(latestValues)
    const handledRootIds = new Set<string>()

    for (const element of latestValues) {
      // 区分値は区分種類と一緒に処理されるのでスキップ
      if (element.indent > 0) continue;

      // 処理済みの区分種類のIDを控えておく
      const root = element
      handledRootIds.add(root.uniqueId)

      const indexInDefaultValues = rootIdIndexMap.get(root.uniqueId)
      if (indexInDefaultValues !== undefined) {
        // defaultValues に存在する区分種類の場合は編集中の最新の値で置き換える
        clone.xmlElementTrees[indexInDefaultValues] = {
          xmlElements: [root, ...treeHelper.getDescendants(root)]
        }

      } else {
        // defaultValues に存在しない区分種類の場合はスキーマ定義の末尾に追加
        clone.xmlElementTrees.push({
          xmlElements: [root, ...treeHelper.getDescendants(root)],
        })
      }
    }

    // defaultValues には在ったが編集中の最新の値に存在しない区分種類は削除
    clone.xmlElementTrees = clone.xmlElementTrees.filter(tree => {
      const type = tree.xmlElements[0]?.attributes[ATTR_TYPE]
      if (type !== TYPE_STATIC_ENUM_MODEL) return true;
      return handledRootIds.has(tree.xmlElements[0]?.uniqueId ?? '')
    })

    return clone
  }, [formDefaultValues, getValues])
}
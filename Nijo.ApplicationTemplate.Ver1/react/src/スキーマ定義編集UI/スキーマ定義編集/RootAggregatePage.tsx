import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import * as ReactTable from "@tanstack/react-table"
import * as Icon from "@heroicons/react/24/solid"
import * as Input from "../../input"
import * as Layout from "../../layout"
import { SchemaDefinitionGlobalState, SchemaDefinitionOutletContextType, ATTR_TYPE, XmlElementAttribute, XmlElementItem } from "./types"
import useEvent from "react-use-event-hook"
import { UUID } from "uuidjs"
import { useAttrDefs } from "./AttrDefContext"
import { TYPE_COLUMN_DEF } from "./getAttrTypeColumnDef"
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import NijoUiErrorMessagePane from "./NijoUiErrorMessagePane"

/**
 * Data, Query, Command のルート集約1件を表示・編集するページ。
 */
export const PageRootAggregate = ({ rootAggregateIndex, formMethods, className }: {
  rootAggregateIndex: number
  formMethods: ReactHookForm.UseFormReturn<SchemaDefinitionGlobalState>
  className?: string
}) => {
  const gridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null)
  const { control } = formMethods
  const { fields, insert, remove, update } = ReactHookForm.useFieldArray({ control, name: `xmlElementTrees.${rootAggregateIndex}.xmlElements` })
  const attributeDefs = useAttrDefs()
  const {
    validationContext: {
      getValidationResult,
      trigger,
      validationResult,
    },
  } = ReactRouter.useOutletContext<SchemaDefinitionOutletContextType>()

  // メンバーグリッドの列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<GridRowType> = React.useCallback(cellType => {
    const columns: Layout.EditableGridColumnDef<GridRowType>[] = []

    // LocalName
    columns.push(createLocalNameCell(cellType, getValidationResult))

    // Type
    columns.push(createAttributeCell(TYPE_COLUMN_DEF, cellType, getValidationResult))

    // Attributes（Type以外）
    // ルート集約のモデルタイプを取得（最初の行のType属性）
    const rootModelType = fields[0]?.attributes[ATTR_TYPE]

    // rootModelTypeに対応する属性のみをフィルタリング
    columns.push(...Array.from(attributeDefs.values())
      .filter(attrDef => attrDef.attributeName !== ATTR_TYPE)
      .filter(attrDef => rootModelType && attrDef.availableModels.includes(rootModelType))
      .map(attrDef => createAttributeCell(attrDef, cellType, getValidationResult)))

    // コメント
    columns.push(cellType.text('comment', 'コメント', { defaultWidth: 400 }))

    return columns
  }, [attributeDefs, fields, getValidationResult])

  // 行挿入
  const handleInsertRow = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange()
    if (!selectedRange) {
      // 選択範囲が無い場合はルート集約の直下に1行挿入
      insert(0, { uniqueId: UUID.generate(), indent: 0, localName: '', attributes: {} })
    } else {
      // 選択範囲がある場合は選択されている行と同じだけの行を選択範囲の前に挿入。
      // ただしルート集約が選択範囲に含まれる場合はルート集約の下に挿入する
      const insertPosition = selectedRange.startRow <= 0
        ? 1 // ルート集約の直下
        : selectedRange.startRow // 選択範囲の前
      const indent = fields[insertPosition]?.indent ?? 1
      const insertRows = Array.from({ length: selectedRange.endRow - selectedRange.startRow + 1 }, (_, i) => ({
        uniqueId: UUID.generate(),
        indent,
        localName: '',
        attributes: {},
      }) satisfies XmlElementItem)
      insert(insertPosition, insertRows)
    }
  })

  // 下挿入
  const handleInsertRowBelow = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange()
    if (!selectedRange) {
      // 選択範囲が無い場合はルート集約の直下に1行挿入
      insert(0, { uniqueId: UUID.generate(), indent: 0, localName: '', attributes: {} })
    } else {
      // 選択範囲がある場合は選択されている行と同じだけの行を選択範囲の下に挿入
      const insertPosition = selectedRange.endRow + 1
      const indent = fields[insertPosition]?.indent ?? 1
      const insertRows = Array.from({ length: selectedRange.endRow - selectedRange.startRow + 1 }, (_, i) => ({
        uniqueId: UUID.generate(),
        indent,
        localName: '',
        attributes: {},
      }) satisfies XmlElementItem)
      insert(insertPosition, insertRows)
    }
  })

  // 行削除。selectedRangeに含まれる行を削除する。ただしルート集約は削除しない
  const handleDeleteRow = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange()
    if (!selectedRange) {
      return
    }
    let removedIndexes = Array.from({ length: selectedRange.endRow - selectedRange.startRow + 1 }, (_, i) => selectedRange.startRow + i)
    removedIndexes = removedIndexes.filter(index => index !== 0) // ルート集約は削除しない
    remove(removedIndexes)
  })

  // インデント下げ。選択範囲に含まれる行のインデントを1ずつ減らす。ただしルート集約は0固定、それ以外の要素の最小インデントは1。
  const handleIndentDown = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows) return
    for (const x of selectedRows) {
      if (x.rowIndex === 0) continue // ルート集約は0固定
      update(x.rowIndex, { ...x.row, indent: Math.max(1, x.row.indent - 1) })
    }
  })
  // インデント上げ。選択範囲に含まれる行のインデントを1ずつ増やす。ただしルート集約は0固定
  const handleIndentUp = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows) return
    for (const x of selectedRows) {
      if (x.rowIndex === 0) continue // ルート集約は0固定
      update(x.rowIndex, { ...x.row, indent: x.row.indent + 1 })
    }
  })

  // セル編集 or クリップボード貼り付け
  const handleChangeRow: Layout.RowChangeEvent<GridRowType> = useEvent(e => {
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow)
    }
    trigger()
  })

  return (
    <PanelGroup className={`h-full ${className ?? ''}`} direction="vertical">
      <Panel className="flex flex-col gap-1">
        <div className="flex flex-wrap gap-1 items-center">
          <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleInsertRow}>行挿入</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleInsertRowBelow}>下挿入</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.TrashIcon} onClick={handleDeleteRow}>行削除</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini icon={Icon.ChevronDoubleLeftIcon} onClick={handleIndentDown}>インデント下げ</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.ChevronDoubleRightIcon} onClick={handleIndentUp}>インデント上げ</Input.IconButton>
          <div className="flex-1"></div>
        </div>
        <div className="flex-1 overflow-y-auto">
          <Layout.EditableGrid
            ref={gridRef}
            rows={fields}
            getColumnDefs={getColumnDefs}
            onChangeRow={handleChangeRow}
            className="h-full border-y border-l border-gray-300"
          />
        </div>
      </Panel>

      <PanelResizeHandle className="h-1" />

      <Panel defaultSize={20} minSize={8} collapsible>
        <NijoUiErrorMessagePane
          getValues={formMethods.getValues}
          validationResult={validationResult}
          className="h-full"
        />
      </Panel>
    </PanelGroup>
  )
}

// --------------------------------------------

/** メンバーグリッドの行の型 */
type GridRowType = ReactHookForm.FieldArrayWithId<SchemaDefinitionGlobalState, `xmlElementTrees.${number}.xmlElements`>

// --------------------------------------------

/** LocalName のセルのレイアウト */
const createLocalNameCell = (
  cellType: Layout.ColumnDefFactories<GridRowType>,
  getValidationResult: SchemaDefinitionOutletContextType['validationContext']['getValidationResult']
) => {
  return cellType.text('localName', '', {
    defaultWidth: 220,
    isFixed: true,
    renderCell: (context: ReactTable.CellContext<GridRowType, unknown>) => {
      const indent = context.row.original.indent
      const bold = indent === 0 ? 'font-bold' : '' // ルート集約は太字

      // エラー情報を取得
      const validation = getValidationResult(context.row.original.uniqueId)
      const hasOwnError = validation?._own?.length > 0
      const bgColor = hasOwnError ? 'bg-amber-300/50' : ''

      return (
        <div className={`flex-1 inline-flex text-left truncate ${bgColor}`}>

          {/* インデント */}
          {Array.from({ length: indent }).map((_, i) => (
            <React.Fragment key={i}>
              {/* インデントのテキスト */}
              <div className="basis-[20px] min-w-[20px] relative leading-none">
                {i >= 1 && (
                  // インデントを表す縦線
                  <div className="absolute top-[-1px] bottom-[-1px] left-0 border-l border-gray-400 border-dotted leading-none"></div>
                )}
              </div>
            </React.Fragment>
          ))}

          <span className={`flex-1 truncate ${bold}`}>
            {context.cell.getValue() as string}
          </span>
        </div>
      )
    }
  })
}

/** 属性のセル */
const createAttributeCell = (
  attrDef: XmlElementAttribute,
  cellType: Layout.ColumnDefFactories<GridRowType>,
  getValidationResult: SchemaDefinitionOutletContextType['validationContext']['getValidationResult']
) => {
  return cellType.other(attrDef.displayName, {
    defaultWidth: 120,
    // 編集開始時処理
    onStartEditing: e => {
      e.setEditorInitialValue(e.row.attributes[attrDef.attributeName] ?? '')
    },
    // 編集終了時処理
    onEndEditing: e => {
      const clone = window.structuredClone(e.row)
      if (e.value.trim() === '') {
        delete clone.attributes[attrDef.attributeName]
      } else {
        clone.attributes[attrDef.attributeName] = e.value
      }
      e.setEditedRow(clone)
    },
    // セルのレンダリング
    renderCell: context => {
      const value = context.row.original.attributes[attrDef.attributeName]
      // エラー情報を取得
      const validationResult = getValidationResult(context.row.original.uniqueId)
      const hasError = validationResult?.[attrDef.attributeName]?.length > 0

      return (
        <PlainCell className={hasError ? 'bg-amber-300/50' : ''}>
          {value}
        </PlainCell>
      )
    },
  })
}

// -----------------------------

const PlainCell = ({ children, className }: {
  children?: React.ReactNode
  className?: string
}) => {
  return (
    <div className={`flex-1 inline-flex text-left truncate ${className ?? ''}`}>
      <span className="flex-1 truncate">
        {children}
      </span>
    </div>
  )
}

import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactTable from "@tanstack/react-table"
import * as Icon from "@heroicons/react/24/solid"
import * as Input from "../../input"
import * as Layout from "../../layout"
import { SchemaDefinitionGlobalState, ATTR_TYPE, XmlElementAttribute, XmlElementItem, ATTR_IS_KEY, TYPE_DATA_MODEL } from "./types"
import * as UI from '../UI'
import useEvent from "react-use-event-hook"
import { UUID } from "uuidjs"
import { TYPE_COLUMN_DEF } from "./getAttrTypeColumnDef"
import { GetValidationResultFunction, ValidationTriggerFunction } from "./useValidation"
import { CellEditorWithMention } from "./CellEditorWithMention"
import { usePersonalSettings } from "../PersonalSettings"

// スキーマ定義データを提供するContext
export const SchemaDefinitionContext = React.createContext<SchemaDefinitionGlobalState | null>(null)

/** コメント列のID */
export const COLUMN_ID_COMMENT = ':comment:'

/**
 * Data, Query, Command のルート集約1件を表示・編集するページ。
 */
export const PageRootAggregate = ({ rootAggregateIndex, formMethods, getValidationResult, trigger, attributeDefs, showLessColumns, className }: {
  rootAggregateIndex: number
  formMethods: ReactHookForm.UseFormReturn<SchemaDefinitionGlobalState>
  getValidationResult: GetValidationResultFunction
  trigger: ValidationTriggerFunction
  attributeDefs: Map<string, XmlElementAttribute>
  /** 名前、Type、キー、コメントのみを表示する */
  showLessColumns: boolean
  className?: string
}) => {
  const gridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null)
  const { control, watch } = formMethods
  const { fields, insert, remove, update, move } = ReactHookForm.useFieldArray({ control, name: `xmlElementTrees.${rootAggregateIndex}.xmlElements` })

  // スキーマ定義全体のデータを取得（メンション機能で使用）
  const schemaDefinitionData = watch()

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

    for (const attrDef of Array.from(attributeDefs.values())) {
      if (attrDef.attributeName === ATTR_TYPE) continue;

      // rootModelTypeに対応する属性のみをフィルタリング
      if (!rootModelType || !attrDef.availableModels.includes(rootModelType)) continue;

      // 主要な列のみ表示の場合、DataModelのキー以外の属性は表示しない
      if (showLessColumns && (rootModelType !== TYPE_DATA_MODEL || attrDef.attributeName !== ATTR_IS_KEY)) continue;

      columns.push(createAttributeCell(attrDef, cellType, getValidationResult))
    }

    // コメント
    columns.push(cellType.text('comment', 'コメント', {
      columnId: COLUMN_ID_COMMENT,
      defaultWidth: 400,
      renderCell: context => {
        const value = context.cell.getValue() as string
        return (
          <div className="flex-1 inline-flex text-left truncate">
            <UI.ReadOnlyMentionText className="flex-1 truncate">
              {value}
            </UI.ReadOnlyMentionText>
          </div>
        )
      }
    }))

    return columns
  }, [attributeDefs, fields, getValidationResult, showLessColumns])

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

  // 選択行を上に移動
  const handleMoveUp = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows) return
    if (selectedRows.some(x => x.rowIndex === 0)) return; // ルート集約は移動不可

    const startRow = selectedRows[0].rowIndex
    const endRow = startRow + selectedRows.length - 1
    if (startRow <= 1) return // ルート集約より上には移動できない

    // 選択範囲の外側（1つ上）の行を選択範囲の下に移動させる
    move(startRow - 1, endRow)
    // 行選択
    gridRef.current?.selectRow(startRow - 1, endRow - 1)
  })

  // 選択行を下に移動
  const handleMoveDown = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows()
    if (!selectedRows) return
    if (selectedRows.some(x => x.rowIndex === 0)) return; // ルート集約は移動不可

    const startRow = selectedRows[0].rowIndex
    const endRow = startRow + selectedRows.length - 1
    if (endRow >= fields.length - 1) return

    // 選択範囲の外側（1つ下）の行を選択範囲の上に移動させる
    move(endRow + 1, startRow)
    // 行選択
    gridRef.current?.selectRow(startRow + 1, endRow + 1)
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

  // グリッドのキーボード操作
  const handleKeyDown: Layout.EditableGridKeyboardEventHandler = useEvent((e, isEditing) => {
    // 編集中の処理の制御はCellEditorに任せる
    if (isEditing) return { handled: false }

    if (!e.ctrlKey && e.key === 'Enter') {
      // 行挿入(Enter)
      handleInsertRow()
    } else if (e.ctrlKey && e.key === 'Enter') {
      // 下挿入(Ctrl + Enter)
      handleInsertRowBelow()
    } else if (e.shiftKey && e.key === 'Delete') {
      // 行削除(Shift + Delete)
      handleDeleteRow()
    } else if (e.altKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      // 上下に移動(Alt + ↑↓)
      if (e.key === 'ArrowUp') {
        handleMoveUp()
      } else if (e.key === 'ArrowDown') {
        handleMoveDown()
      }
    } else if (e.shiftKey && e.key === 'Tab') {
      // インデント下げ(Shift + Tab)
      handleIndentDown()
    } else if (e.key === 'Tab') {
      // インデント上げ(Tab)
      handleIndentUp()
    } else {
      return { handled: false }
    }
    return { handled: true }
  })

  const { personalSettings } = usePersonalSettings()

  return (
    <SchemaDefinitionContext.Provider value={schemaDefinitionData}>
      <div className={`flex flex-col gap-1 ${className ?? ''}`}>
        {!personalSettings.hideGridButtons && (
          <div className="flex flex-wrap gap-1 items-center">
            <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleInsertRow}>行挿入(Enter)</Input.IconButton>
            <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleInsertRowBelow}>下挿入(Ctrl + Enter)</Input.IconButton>
            <Input.IconButton outline mini icon={Icon.TrashIcon} onClick={handleDeleteRow}>行削除(Shift + Delete)</Input.IconButton>
            <div className="basis-2"></div>
            <Input.IconButton outline mini icon={Icon.ChevronDoubleLeftIcon} onClick={handleIndentDown}>インデント下げ(Shift + Tab)</Input.IconButton>
            <Input.IconButton outline mini icon={Icon.ChevronDoubleRightIcon} onClick={handleIndentUp}>インデント上げ(Tab)</Input.IconButton>
            <Input.IconButton outline mini icon={Icon.ChevronUpIcon} onClick={handleMoveUp}>上に移動(Alt + ↑)</Input.IconButton>
            <Input.IconButton outline mini icon={Icon.ChevronDownIcon} onClick={handleMoveDown}>下に移動(Alt + ↓)</Input.IconButton>
            <div className="flex-1"></div>
          </div>
        )}
        <div className="flex-1 overflow-y-auto">
          <Layout.EditableGrid
            ref={gridRef}
            rows={fields}
            getColumnDefs={getColumnDefs}
            editorComponent={CellEditorWithMention}
            onChangeRow={handleChangeRow}
            onKeyDown={handleKeyDown}
            className="h-full border-y border-l border-gray-300"
          />
        </div>
      </div>
    </SchemaDefinitionContext.Provider>
  )
}

// --------------------------------------------

/** メンバーグリッドの行の型 */
type GridRowType = ReactHookForm.FieldArrayWithId<SchemaDefinitionGlobalState, `xmlElementTrees.${number}.xmlElements`>

// --------------------------------------------

/** LocalName のセルのレイアウト */
const createLocalNameCell = (
  cellType: Layout.ColumnDefFactories<GridRowType>,
  getValidationResult: GetValidationResultFunction
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
                  <div className="absolute top-[-1px] bottom-[-1px] left-0 border-l border-gray-300 border-dotted leading-none"></div>
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
  getValidationResult: GetValidationResultFunction
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

import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactTable from "@tanstack/react-table"
import * as Icon from "@heroicons/react/24/solid"
import * as Input from "../../input"
import * as Layout from "../../layout"
import { ApplicationState, ATTR_TYPE, XmlElementItem } from "./types"
import useEvent from "react-use-event-hook"

/**
 * Data, Query, Command のルート集約1件を表示・編集するページ。
 */
export const PageRootAggregate = ({ rootAggregateIndex, formMethods, className }: {
  rootAggregateIndex: number
  formMethods: ReactHookForm.UseFormReturn<ApplicationState>
  className?: string
}) => {
  const { control, getValues } = formMethods
  const { fields, append, remove } = ReactHookForm.useFieldArray({ control, name: `xmlElementTrees.${rootAggregateIndex}.xmlElements` })

  // ルート集約
  const rootAggregate = React.useMemo(() => {
    return getValues(`xmlElementTrees.${rootAggregateIndex}.xmlElements.0`)
  }, [rootAggregateIndex, getValues])

  // メンバーグリッドの列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<GridRowType> = React.useCallback(cellType => {
    return [
      // LocalName
      cellType.text('localName', '項目名', {
        defaultWidth: 220,
        isFixed: true,
        renderCell: renderLocalNameCell,
      }),
      // Type
      cellType.other('種類', {
        defaultWidth: 120,
        isFixed: true,
        renderCell: renderTypeCell,
      }),
    ]
  }, [])

  // この集約を削除する
  const handleDelete = useEvent(() => {
    if (confirm(`${rootAggregate.localName}を削除しますか？`)) {
      remove(rootAggregateIndex)
    }
  })

  return (
    <div className={`h-full flex flex-col gap-1 ${className}`}>
      <div className="flex gap-1 items-center">
        <h1>{rootAggregate.localName}</h1>
        <div className="flex-1"></div>
        <Input.IconButton outline mini hideText icon={Icon.TrashIcon} onClick={handleDelete}>削除</Input.IconButton>
      </div>
      <div className="flex-1 overflow-y-auto">
        <Layout.EditableGrid
          rows={fields}
          getColumnDefs={getColumnDefs}
          className="h-full"
        />
      </div>
    </div>
  )
}

// --------------------------------------------

/** メンバーグリッドの行の型 */
type GridRowType = ReactHookForm.FieldArrayWithId<ApplicationState, `xmlElementTrees.${number}.xmlElements`>

// --------------------------------------------

/** LocalName のセルのレイアウト */
const renderLocalNameCell = (context: ReactTable.CellContext<ReactHookForm.FieldArrayWithId<ApplicationState, `xmlElementTrees.${number}.xmlElements`, "id">, unknown>) => {

  return (
    <div className="max-w-full inline-flex text-center leading-none">

      {/* インデント */}
      <div style={{ width: context.row.original.indent * 20 }}></div>

      <span className="flex-1 truncate">
        {context.cell.getValue() as string}
      </span>
    </div>
  )
}

/** 種類のセルのレイアウト */
const renderTypeCell = (context: ReactTable.CellContext<ReactHookForm.FieldArrayWithId<ApplicationState, `xmlElementTrees.${number}.xmlElements`, "id">, unknown>) => {

  const type = context.row.original.attributes?.get(ATTR_TYPE)

  return (
    <div className="inline-flex text-center leading-none">
      {type}
    </div>
  )
}
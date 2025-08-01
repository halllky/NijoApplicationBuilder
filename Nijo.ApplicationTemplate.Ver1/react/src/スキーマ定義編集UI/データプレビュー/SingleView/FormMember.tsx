import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Icon from "@heroicons/react/24/outline"
import * as Input from "../../../input"
import * as Layout from "../../../layout"
import { EditableDbRecord, DbTableSingleItemEditor, ReloadTrigger, TableMetadataHelper } from "../types"
import useQueryEditorServerApi from "../useQueryEditorServerApi"
import { DataModelMetadata } from "../../../__autoGenerated/util"
import useEvent from "react-use-event-hook"
import { DataPreviewGlobalContext } from "../DataPreviewGlobalContext"
import { DbRecordSelectorDialog, DbRecordSelectorDialogProps } from "../parts/DbRecordSelectorDialog"
import { useForeignKeyLookup } from "../parts/useForeignKeyLookup"
import { SingleViewContext } from "./SingleView"
import { AggregateFormView } from "./Form"
import { AggregateGridView } from "./Grid"

/**
 * フォーム表示における集約のメンバー1個分を編集するコンポーネント
 */
export const AggregateMemberFormView = ({ record, onChangeRecord, member, nextMember, owner, ownerName, ownerIsReadOnly }: {
  record: EditableDbRecord | undefined,
  onChangeRecord: (value: EditableDbRecord) => void,
  member: DataModelMetadata.AggregateMember | DataModelMetadata.Aggregate
  /** memberの1つ次 */
  nextMember: DataModelMetadata.AggregateMember | DataModelMetadata.Aggregate | undefined
  owner: DataModelMetadata.Aggregate
  ownerName: string
  ownerIsReadOnly: boolean
}) => {

  const { tableMetadataHelper } = React.useContext(SingleViewContext)

  const { getDbRecords } = useQueryEditorServerApi()

  // ラベル列の横幅
  const { control: dataPreviewControl } = React.useContext(DataPreviewGlobalContext)
  const rootPath = tableMetadataHelper?.getRoot(owner)?.path ?? ''
  const singleViewLabelWidth = ReactHookForm.useWatch({ control: dataPreviewControl, name: `design.${rootPath}.singleViewLabelWidth` })
  const labelCssProperties: React.CSSProperties = React.useMemo(() => {
    const labelWidth = singleViewLabelWidth || '10em'
    return { flexBasis: labelWidth, minWidth: labelWidth }
  }, [singleViewLabelWidth])

  // 読み取り専用判定
  const isReadOnly = React.useMemo(() => {
    // 親が読み取り専用の場合は、子も読み取り専用
    if (ownerIsReadOnly) return true

    // 表示されないので未定義
    if (member.type === "root") return undefined
    if (member.type === "child") return undefined
    if (member.type === "children") return undefined
    if (member.type === "parent-key") return undefined
    if (member.type === "own-column") return member.isPrimaryKey && record?.existsInDb
    if (member.type === "ref-key") return member.isPrimaryKey && record?.existsInDb
    if (member.type === "ref-parent-key") return member.isPrimaryKey && record?.existsInDb
  }, [member, record?.existsInDb, ownerIsReadOnly])

  // テキストボックスの値変更時
  const handleChangeText: React.ChangeEventHandler<HTMLInputElement> = useEvent(e => {
    if (!record) return;
    if (member.type !== "own-column" && member.type !== "ref-key" && member.type !== "ref-parent-key") return;
    const clone = window.structuredClone(record)
    clone.values[member.columnName] = e.target.value === '' ? null : e.target.value
    onChangeRecord(clone)
  })

  // メンバーの表示名を取得
  const memberSettings = ReactHookForm.useWatch({
    control: dataPreviewControl,
    name: `design.${rootPath}.membersDesign.${member.refToRelationName || member.columnName}`
  })
  const displayName = (member.type === "own-column" || member.type === "parent-key")
    ? (memberSettings?.displayName || member.columnName)
    : (member.type === "ref-key" || member.type === "ref-parent-key"
      ? (memberSettings?.singleViewRefDisplayColumnNamesDisplayNames?.[member.refToColumnName ?? ''] || member.columnName)
      : (memberSettings?.multiViewRefDisplayColumnNamesDisplayNames?.[member.refToColumnName ?? ''] || member.columnName))

  // 参照キーの検索
  const [refKeySearchDialogProps, setRefKeySearchDialogProps] = React.useState<DbRecordSelectorDialogProps | null>(null)
  const handleSearch = useEvent(() => {
    if (member.type !== "ref-key" && member.type !== "ref-parent-key") return;
    if (!tableMetadataHelper) return;
    const refToTableMetadata = tableMetadataHelper.getRefTo(member)
    if (!refToTableMetadata) return;
    setRefKeySearchDialogProps({
      tableMetadata: refToTableMetadata,
      tableMetadataHelper: tableMetadataHelper,
      onSelect: keys => {
        if (!record) return;

        // 参照先テーブルが複合キーである可能性を考慮し、当該参照先の全キーに代入
        const clone = window.structuredClone(record)
        let index = 0
        for (const m of owner.members) {
          if (m.type !== "ref-key" && m.type !== "ref-parent-key" || m.refToRelationName !== member.refToRelationName) continue;
          const value = keys[index] as string
          clone.values[m.columnName] = value === '' ? null : value
          index++
        }
        onChangeRecord(clone)
        setRefKeySearchDialogProps(null)
      },
      onCancel: () => {
        setRefKeySearchDialogProps(null)
      },
    })
  })

  // ルート集約はAggregateViewで処理する。ここには来ない
  if (member.type === "root") {
    console.error(`ルート集約はAggregateViewで処理する。ここには来ない: ${member.columnName}`)
    return null
  }

  // 親の主キー（非表示）
  if (member.type === "parent-key") {
    return undefined
  }

  // テーブル自身の属性のカラム、または外部参照のキー項目
  if (member.type === "own-column" || member.type === "ref-key" || member.type === "ref-parent-key") {

    // 参照先が切り替わるタイミングで当該参照先の名称などの追加項目を表示する
    const showRefToAdditionalColumns = member.refToAggregatePath && (nextMember === undefined || nextMember.refToAggregatePath !== member.refToAggregatePath)

    return (
      <>
        <div className="flex gap-1 items-center">
          <div
            className="text-sm break-all select-none text-gray-600"
            style={labelCssProperties}
          >
            {displayName}
          </div>
          <div className={`flex-1 flex border ${isReadOnly ? 'border-transparent' : 'bg-white border-gray-500'}`}>
            {(member.type === "ref-key" || member.type === "ref-parent-key") && !isReadOnly && (
              <Input.IconButton icon={Icon.MagnifyingGlassIcon} hideText mini onClick={handleSearch}>
                検索
              </Input.IconButton>
            )}
            <input
              type="text"
              value={record?.values[member.columnName] ?? ''}
              onChange={handleChangeText}
              spellCheck={false}
              placeholder="NULL"
              className="flex-1 px-1 outline-none placeholder:text-gray-300"
              readOnly={isReadOnly}
            />
          </div>
          {refKeySearchDialogProps && (
            <DbRecordSelectorDialog {...refKeySearchDialogProps} />
          )}
        </div>

        {/* 参照先テーブルの主キー以外の属性 */}
        {showRefToAdditionalColumns && tableMetadataHelper && (
          <div className="flex gap-1 items-center">
            <div style={labelCssProperties}></div>
            <RefKeyAdditionalColumns
              member={member}
              record={record}
              owner={owner}
              tableMetadataHelper={tableMetadataHelper}
              getDbRecords={getDbRecords}
            />
          </div>
        )}
      </>
    )
  }

  if (member.type === "child") {
    const childAggregate = tableMetadataHelper?.allAggregates().find(a => a.tableName === member.tableName)
    if (!childAggregate) {
      return null // 画面初期化前の場合
    }
    return (
      <AggregateFormView
        itemIndexInDbRecordArray={0}
        aggregate={childAggregate}
        owner={record ?? null}
        ownerIsReadOnly={ownerIsReadOnly}
        onChangeDefinition={undefined}
      />
    )
  }

  if (member.type === "children") {
    return (
      <AggregateGridView
        itemIndexInForm={0}
        owner={record ?? null}
        ownerMetadata={owner}
        childrenMetadata={member}
        ownerName={ownerName}
        ownerIsReadOnly={ownerIsReadOnly}
      />
    )
  }

  return (
    <div>
      未実装: {member.type}
    </div>
  )
}

/**
 * ref-key と ref-parent-key の参照先テーブルの追加カラムを表示するコンポーネント
 */
const RefKeyAdditionalColumns = ({
  member,
  record,
  owner,
  tableMetadataHelper,
  getDbRecords
}: {
  member: DataModelMetadata.AggregateMember
  record: EditableDbRecord | undefined
  owner: DataModelMetadata.Aggregate
  tableMetadataHelper: TableMetadataHelper
  getDbRecords: ReturnType<typeof useQueryEditorServerApi>['getDbRecords']
}) => {
  // 設定から refDisplayColumnNames を取得
  const { control } = React.useContext(DataPreviewGlobalContext)
  const rootPath = tableMetadataHelper.getRoot(owner)?.path ?? ''
  const refDisplayColumnNames = ReactHookForm.useWatch({ control, name: `design.${rootPath}.membersDesign.${member.refToRelationName}.singleViewRefDisplayColumnNames` })

  // 設定から refDisplayColumnNamesDisplayNames を取得
  const refDisplayColumnNamesDisplayNames = ReactHookForm.useWatch({ control, name: `design.${rootPath}.membersDesign.${member.refToRelationName}.singleViewRefDisplayColumnNamesDisplayNames` })

  // 参照先テーブルの情報を取得
  const refToAggregate = React.useMemo(() => {
    if (!member.refToAggregatePath) return null
    return tableMetadataHelper.allAggregates().find(a => a.path === member.refToAggregatePath) ?? null
  }, [member.refToAggregatePath, tableMetadataHelper])

  // 参照先テーブルのキー値が変更されたタイミングでデータを取得
  const { trigger } = React.useContext(SingleViewContext)
  const { data: refData, isLoading, error } = useForeignKeyLookup({
    record,
    foreignKeyRelationName: member.refToRelationName!,
    ownerTableMetadata: owner,
    refToAggregate,
    getDbRecords,
    trigger,
  })

  // 表示するカラムがない場合は何も表示しない
  if (!refDisplayColumnNames || refDisplayColumnNames.length === 0 || !refToAggregate) {
    return null
  }

  return (
    <div className="flex-1 flex-col pt-px pb-2 flex gap-px">
      {error && (
        <span className="text-sm text-rose-600">
          {error}
        </span>
      )}
      {refDisplayColumnNames.map(columnName => {
        const displayName = refDisplayColumnNamesDisplayNames?.[columnName] || columnName
        return (
          <div key={columnName} className="flex gap-x-2 flex-wrap items-center">
            <span className="text-xs text-gray-500">{displayName}:</span>
            <span className="select-text text-gray-800">
              {isLoading ? '…' : refData?.values[columnName]}
              &nbsp;
            </span>
          </div>
        )
      })}
    </div>
  )
}

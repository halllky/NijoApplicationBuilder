import React from 'react'
import useEvent from 'react-use-event-hook'
import * as Icon from '@heroicons/react/24/solid'
import * as Layout from './__autoGenerated/collection'
import * as Input from './__autoGenerated/input'
import * as Util from './__autoGenerated/util'
import { AggregateOrMemberTypeKey, GRID_COL, GridRow, OptionalAttributeKey, PageState } from './types'
import { TypeComboReturns } from './useTypeCombo'
import { useValidationErrorContext } from './useValidationError'
import { useFlattenArrayTree } from './useFlattenArrayTree'

/** グリッド列定義 */
export const useColumnDef = (
  optionalAttributes: PageState['optionalAttributes'],
  { typeComboSource, typeComboProps }: TypeComboReturns,
  expandableRows: ReturnType<typeof useFlattenArrayTree>['expandableRows'],
  collapsedRowIds: ReturnType<typeof useFlattenArrayTree>['collapsedRowIds'],
  toggleCollapsing: ReturnType<typeof useFlattenArrayTree>['toggleCollapsing'],
) => {

  return React.useMemo((): Layout.DataTableColumn<GridRow>[] => {

    // -------------------------------------
    // 集約名（階層構造）
    const displayNameColumn: Layout.DataTableColumn<GridRow> = {
      id: GRID_COL.DISPLAY_NAME,
      ...textCell(
        row => row.displayName ?? '',
        (row, value) => { row.displayName = value },
      ),
      defaultWidthPx: 240,
      render: row => {
        const errors = useValidationErrorContext(row.uniqueId, GRID_COL.DISPLAY_NAME)

        let icon: React.ElementType | undefined
        if (!expandableRows.has(row.uniqueId)) {
          icon = undefined
        } else if (collapsedRowIds.has(row.uniqueId)) {
          icon = Icon.ChevronRightIcon
        } else {
          icon = Icon.ChevronDownIcon
        }

        const handleCollapse = useEvent(() => {
          toggleCollapsing(row.uniqueId)
        })
        return (
          <CellText errors={errors} className="flex overflow-x-hidden gap-1">
            <div style={{ flexBasis: row.depth * 28 }}></div>
            <span className="flex-1 inline-flex items-center gap-px overflow-x-hidden whitespace-nowrap text-ellipsis">
              <Input.IconButton icon={icon} onClick={handleCollapse} inline hideText>折りたたみ</Input.IconButton>
              {row.displayName}
            </span>
          </CellText>
        )
      },
    }

    // -------------------------------------
    // 集約またはメンバーの型
    const setValueTypeCell = (row: GridRow, value: unknown) => {
      if (typeComboSource?.some(t => t.key === value)) {
        row.type = value as AggregateOrMemberTypeKey
      } else {
        row.type = undefined
      }
    }
    const typeColumns: Layout.DataTableColumn<GridRow> = {
      id: GRID_COL.TYPE,
      header: '種類',
      onClipboardCopy: row => row.type ?? '',
      render: row => {
        const errors = useValidationErrorContext(row.uniqueId, GRID_COL.TYPE)
        return (
          <CellText errors={errors}>
            {typeComboSource?.find(t => t.key === row.type)?.displayName ?? row.type}
          </CellText>
        )
      },
      editSetting: {
        type: 'combo',
        comboProps: typeComboProps,
        onStartEditing: row => row.type,
        onEndEditing: setValueTypeCell,
        onClipboardPaste: setValueTypeCell,
      },
    }

    // -------------------------------------
    // 集約またはメンバーの型の詳細情報（step や variation-item の数値指定）
    const typeDetailColumn: Layout.DataTableColumn<GridRow> = {
      id: GRID_COL.TYPE_DETAIL,
      ...textCell(
        row => row.typeDetail ?? '',
        (row, value) => { row.typeDetail = value },
      ),
      defaultWidthPx: 40,
      render: row => {
        const errors = useValidationErrorContext(row.uniqueId, GRID_COL.TYPE_DETAIL)
        return (
          <CellText errors={errors}>
            {row.typeDetail}
          </CellText>
        )
      },
    }

    // -------------------------------------
    // オプション列
    const optionColumns: Layout.DataTableColumn<GridRow>[] = []
    for (let i = 0; i < (optionalAttributes?.length ?? 0); i++) {
      const def = optionalAttributes![i]
      if (def.type === 'boolean') {
        // 真偽値型のオプション
        const setBooleanValue = (row: GridRow, key: OptionalAttributeKey, value: boolean) => {
          const objValue = { key: key, value: '' }
          if (!row.attrValues) {
            row.attrValues = value ? [objValue] : []
          } else {
            const ix = row.attrValues?.findIndex(v => v.key === key)
            const arr = [...row.attrValues]
            if (value) {
              if (ix === -1) {
                arr.push(objValue)
              } else {
                arr.splice(ix, 1, objValue)
              }
            } else if (!value && ix !== -1) {
              arr.splice(ix, 1)
            }
            row.attrValues = arr
          }
        }
        const editSetting: Layout.ColumnEditSetting<GridRow, { key: 'T' | 'F', text: string }> = {
          type: 'combo',
          onStartEditing: row => row.attrValues?.some(v => v.key === def.key)
            ? { key: 'T', text: '✓' }
            : { key: 'F', text: '' },
          onEndEditing: (row, value) => {
            setBooleanValue(row, def.key, value?.key === 'T')
          },
          onClipboardPaste: (row, value) => {
            const normalized = Util.normalize(value).toLowerCase()
            const blnValue =
              normalized === 't'
              || normalized === 'true'
              || normalized === '1'
              || normalized === '✓'
            setBooleanValue(row, def.key, blnValue)
          },
          comboProps: {
            onFilter: async () => [{ key: 'T', text: '✓' }, { key: 'F', text: '' }],
            getOptionText: opt => opt.text,
            getValueFromOption: opt => opt,
            getValueText: value => value.text,
          }
        }
        optionColumns.push({
          id: def.key,
          header: def.displayName,
          headerTitle: `${def.displayName ?? ''}\n${def.helpText ?? ''}`,
          defaultWidthPx: 60,
          onClipboardCopy: row => row.attrValues?.some(v => v.key === def.key) ? 'true' : '',
          render: row => {
            const errors = useValidationErrorContext(row.uniqueId, def.key)
            return (
              <CellText errors={errors}>
                {row.attrValues?.some(v => v.key === def.key) ? '✓' : ''}
              </CellText>
            )
          },
          editSetting: editSetting as Layout.ColumnEditSetting<GridRow, unknown>,
        })
        continue
      }

      const setStringValue = (row: GridRow, key: OptionalAttributeKey, value: string) => {
        if (!row.attrValues) {
          row.attrValues = value ? [{ key, value }] : []
        } else {
          const ix = row.attrValues?.findIndex(v => v.key === key)
          const arr = [...row.attrValues]
          if (value) {
            if (ix === -1) {
              arr.push({ key, value })
            } else {
              arr.splice(ix, 1, { key, value })
            }
          } else if (!value && ix !== -1) {
            arr.splice(ix, 1)
          }
          row.attrValues = arr
        }
      }

      if (def.type === 'number') {
        optionColumns.push({
          id: def.key,
          header: def.displayName,
          headerTitle: `${def.displayName ?? ''}\n${def.helpText ?? ''}`,
          defaultWidthPx: 60,
          onClipboardCopy: row => row.attrValues?.find(v => v.key === def.key)?.value ?? '',
          render: row => {
            const errors = useValidationErrorContext(row.uniqueId, def.key)
            return (
              <CellText errors={errors}>
                {row.attrValues?.find(v => v.key === def.key)?.value}
              </CellText>
            )
          },
          editSetting: {
            type: 'text',
            onStartEditing: row => row.attrValues?.find(v => v.key === def.key)?.value,
            onEndEditing: (row, value) => {
              const num = value ? Number(Util.normalize(value)) : NaN
              setStringValue(row, def.key, isNaN(num) ? '' : num.toString())
            },
            onClipboardPaste: (row, text) => {
              const num = text ? Number(Util.normalize(text)) : NaN
              setStringValue(row, def.key, isNaN(num) ? '' : num.toString())
            },
          },
        })

      } else {
        optionColumns.push({
          id: def.key,
          header: def.displayName,
          headerTitle: `${def.displayName ?? ''}\n${def.helpText ?? ''}`,
          defaultWidthPx: 60,
          onClipboardCopy: row => row.attrValues?.find(v => v.key === def.key)?.value ?? '',
          render: row => {
            const errors = useValidationErrorContext(row.uniqueId, def.key)
            return (
              <CellText errors={errors}>
                {row.attrValues?.find(v => v.key === def.key)?.value}
              </CellText>
            )
          },
          editSetting: {
            type: 'text',
            onStartEditing: row => row.attrValues?.find(v => v.key === def.key)?.value,
            onEndEditing: (row, value) => {
              setStringValue(row, def.key, value ?? '')
            },
            onClipboardPaste: (row, text) => {
              setStringValue(row, def.key, text)
            },
          },
        })
      }
    }
    // -------------------------------------
    // コメント
    const commentColumn: Layout.DataTableColumn<GridRow> = {
      id: GRID_COL.COMMENT,
      header: '備考',
      defaultWidthPx: 240,
      onClipboardCopy: row => row.comment ?? '',
      render: row => {
        const errors = useValidationErrorContext(row.uniqueId, GRID_COL.COMMENT)
        return (
          <CellText errors={errors}>
            {row.comment}
          </CellText>
        )
      },
      editSetting: {
        type: 'multiline-text',
        onStartEditing: row => row.comment,
        onClipboardPaste: (row, text) => row.comment = text,
        onEndEditing: (row, value) => row.comment = value,
      },
    }

    // -------------------------------------
    return [
      displayNameColumn,
      typeColumns,
      typeDetailColumn,
      ...optionColumns,
      commentColumn,
    ]
  }, [typeComboSource, typeComboProps, optionalAttributes, expandableRows, collapsedRowIds, toggleCollapsing])
}

const CellText = ({ errors, className, children }: {
  errors?: string[]
  className?: string
  children?: React.ReactNode
}) => {
  return (
    <span
      title={errors?.join('\n')}
      className={`block px-1 text-ellipsis whitespace-nowrap overflow-x-hidden ${errors ? 'bg-orange-100' : ''} ${className}`}
    >
      {children}
      &nbsp;
    </span>
  )
}

const textCell = (
  getValue: (row: GridRow) => string,
  setValue: (row: GridRow, value: string | undefined) => void
): Pick<Layout.DataTableColumn<GridRow>, 'editSetting' | 'onClipboardCopy'> => ({
  onClipboardCopy: getValue,
  editSetting: {
    type: 'text',
    onClipboardPaste: setValue,
    onEndEditing: setValue,
    onStartEditing: getValue,
  },
})

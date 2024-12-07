import React from 'react'
import * as Layout from './__autoGenerated/collection'
import * as Input from './__autoGenerated/input'
import * as Util from './__autoGenerated/util'
import { AggregateOrMemberTypeDef, EnumComboOption, GridRow, PageState, RefToAggregateOption, ValueObjectComboOption } from './types'
import { UseFlattenArrayTreeReturns } from './useFlattenArrayTree'

type TypeComboOption = AggregateOrMemberTypeDef | RefToAggregateOption | EnumComboOption | ValueObjectComboOption
export type TypeComboReturns = ReturnType<typeof useTypeCombo>

/**
 * 集約または集約メンバーの型を選択するコンボボックスの定義
 */
export const useTypeCombo = (
  aggregateOrMemberTypes: PageState['aggregateOrMemberTypes'],
  flattenGridRows: GridRow[],
  getDescendants: UseFlattenArrayTreeReturns['getDescendants']
) => {

  // コンボボックスのデータソース
  const comboSource = React.useMemo((): TypeComboOption[] => {
    if (flattenGridRows.length === 0) return []

    const source: TypeComboOption[] = []
    if (aggregateOrMemberTypes) source.push(...aggregateOrMemberTypes)

    // -------------------------------
    // ref-to

    // 参照可能な集約に絞り込む。例えばコマンドやenumは参照できない。
    const referencableRootNodes = flattenGridRows
      .filter(agg => agg.depth === 0
        // この値はC#側と統一させる必要があるのでルールを変える時は注意
        && (agg.type === 'write-model-2'
          || agg.type === 'read-model-2'
          || agg.type === 'write-model-2 generate-default-read-model'))

    // ルート集約以外にも、子孫のうち child, children, variation-item は参照可能
    for (const rootNode of referencableRootNodes) {
      const descendants = [rootNode, ...getDescendants(rootNode)]
      const onlyAggregate = descendants
        .filter(agg => agg.depth === 0
          // この値はC#側と統一させる必要があるのでルールを変える時は注意
          || agg.type === 'child'
          || agg.type === 'children'
          || agg.type === 'variation-item')
      source.push(...onlyAggregate.map<RefToAggregateOption>(agg => ({
        key: `ref-to:${agg.uniqueId}`,
        displayName: `ref-to:${agg.attrValues?.find(a => a.key === 'physical-name')?.value ?? agg.displayName}`,
      })))
    }

    // -------------------------------
    // enum
    const enums = flattenGridRows
      .filter(agg => agg.depth === 0
        // この値はC#側と統一させる必要があるのでルールを変える時は注意
        && agg.type === 'enum')
    source.push(...enums.map<EnumComboOption>(en => ({
      key: `enum:${en.uniqueId}`,
      displayName: en.attrValues?.find(a => a.key === 'physical-name')?.value ?? en.displayName,
    })))

    // -------------------------------
    // value-object
    const valueObjects = flattenGridRows
      .filter(agg => agg.depth === 0
        // この値はC#側と統一させる必要があるのでルールを変える時は注意
        && agg.type === 'value-object')
    source.push(...valueObjects.map<ValueObjectComboOption>(en => ({
      key: `value-object:${en.uniqueId}`,
      displayName: en.attrValues?.find(a => a.key === 'physical-name')?.value ?? en.displayName,
    })))

    // -------------------------------
    return source
  }, [getDescendants, flattenGridRows, aggregateOrMemberTypes])

  const typeComboProps = React.useMemo((): Input.ComboProps<TypeComboOption, GridRow['type']> => ({
    onFilter: keyword => {
      if (keyword) {
        const lower = keyword.toLowerCase()
        return Promise.resolve(comboSource.filter(t => t.displayName?.toLowerCase().includes(lower)) ?? [])
      } else {
        return Promise.resolve(comboSource)
      }
    },
    getOptionText: opt => (
      <div className="flex flex-col">
        <span>{opt.displayName}</span>
        <span className="text-color-6 text-xs max-w-min min-w-full">{opt.helpText}</span>
      </div>
    ),
    getValueFromOption: opt => opt.key,
    getValueText: value => comboSource.find(t => t.key === value)?.displayName ?? value ?? '',
  }), [comboSource])

  return {
    typeComboSource: comboSource,
    typeComboProps: typeComboProps as Input.ComboProps<unknown, unknown>,
  }
}

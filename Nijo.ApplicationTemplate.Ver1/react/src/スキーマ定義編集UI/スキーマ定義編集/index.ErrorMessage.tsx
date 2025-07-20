import React from "react"
import { SchemaDefinitionGlobalState, asTree } from "./types"
import { ValidationResultListItem } from "./useValidation"

/**
 * エラーメッセージ表示欄。
 * すべての要素のエラーメッセージを羅列する。
 */
export default function ({ getValues, validationResultList, selectRootAggregate, className }: {
  getValues: () => SchemaDefinitionGlobalState
  validationResultList: ValidationResultListItem[]
  selectRootAggregate: ((aggregateId: string) => void) | undefined
  className?: string
}) {
  const handleClick = (elementId: string) => {
    if (!selectRootAggregate) return;

    const xmlElementTrees = getValues().xmlElementTrees
    if (!xmlElementTrees) return

    for (const tree of xmlElementTrees) {
      const targetElement = tree.xmlElements.find(el => el.uniqueId === elementId)
      if (targetElement) {
        const treeUtils = asTree(tree.xmlElements)
        const rootElement = treeUtils.getRoot(targetElement)
        const aggregateId = rootElement.uniqueId
        selectRootAggregate(aggregateId)
        return
      }
    }
  }

  return (
    <div className={`block overflow-y-auto ${className ?? ''}`}>
      {validationResultList.length > 0 && (
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left">
              <th className="text-xs font-normal px-px pb-1 text-amber-600" colSpan={4}>
                エラー（{validationResultList.length}件）
              </th>
            </tr>
          </thead>
          <tbody>
            {validationResultList.map((info, i) => (
              <tr
                key={i}
                className="cursor-pointer hover:bg-amber-50"
                onClick={() => handleClick(info.id)}
              >
                <td className="px-px  truncate text-amber-600">{info.rootAggregateName}</td>
                <td className="px-px  truncate text-amber-600">{info.elementName}</td>
                <td className="px-px  truncate text-amber-600">{info.attributeName}</td>
                <td className="px-px truncate text-amber-600">{info.message}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

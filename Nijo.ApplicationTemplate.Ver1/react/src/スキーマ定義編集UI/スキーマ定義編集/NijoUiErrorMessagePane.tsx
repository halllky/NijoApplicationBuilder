import React from "react"
import * as ReactHookForm from "react-hook-form"
import { SchemaDefinitionGlobalState, asTree } from "./types"
import { ValidationResult } from "./ValidationContext"

/**
 * エラーメッセージ表示欄。
 * すべての要素のエラーメッセージを羅列する。
 */
export default function ({ getValues, validationResult, selectRootAggregate, className }: {
  getValues: ReactHookForm.UseFormGetValues<SchemaDefinitionGlobalState>
  validationResult: ValidationResult | undefined
  selectRootAggregate: (aggregateId: string) => void
  className?: string
}) {
  // エラーメッセージをプレーンな配列に変換する。
  const errorInfos = React.useMemo(() => {
    const xmlElementTrees = getValues(`xmlElementTrees`) ?? []
    if (!xmlElementTrees.length) return []

    // IDから当該要素の情報を引き当てるための辞書
    const elementMap = new Map(xmlElementTrees.flatMap(tree => tree.xmlElements).map(el => [el.uniqueId, el]))
    const treeUtilsMap = new Map(xmlElementTrees.map(tree => [tree, asTree(tree.xmlElements)]))

    const infos: Array<{
      id: string
      rootAggregateName: string
      elementName: string
      attributeName: string
      message: string
    }> = []

    for (const [id, obj] of Object.entries(validationResult ?? {})) {
      const element = elementMap.get(id)
      if (!element) continue

      const tree = xmlElementTrees.find(t => t.xmlElements.some(el => el.uniqueId === id))
      if (!tree) continue

      const treeUtils = treeUtilsMap.get(tree)
      if (!treeUtils) continue

      const rootElement = treeUtils.getRoot(element)
      const rootAggregateName = rootElement.localName

      for (const [objKey, attrMessages] of Object.entries(obj)) {
        const attributeName = objKey === '_own' ? '' : objKey
        infos.push(...attrMessages.map(x => ({
          id,
          rootAggregateName: rootAggregateName ?? '',
          elementName: element.localName ?? '',
          attributeName,
          message: x,
        })))
      }
    }
    return infos
  }, [getValues, validationResult])

  const handleClick = (elementId: string) => {
    const xmlElementTrees = getValues('xmlElementTrees')
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
      {errorInfos.length > 0 && (
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left">
              <th className="text-xs font-normal px-px pb-1 text-amber-600" colSpan={4}>
                エラー（{errorInfos.length}件）
              </th>
            </tr>
          </thead>
          <tbody>
            {errorInfos.map((info, i) => (
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

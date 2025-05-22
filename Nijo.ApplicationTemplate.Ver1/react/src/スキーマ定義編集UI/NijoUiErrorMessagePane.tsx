import React from "react"
import * as ReactHookForm from "react-hook-form"
import { ApplicationState, asTree } from "./types"
import { useValidationContext } from "./useValidationContext"
import { useNavigate } from "react-router-dom"
import { getNavigationUrl } from "./index"

/**
 * エラーメッセージ表示欄。
 * すべての要素のエラーメッセージを羅列する。
 */
export default function ({ getValues, className }: {
  getValues: ReactHookForm.UseFormGetValues<ApplicationState>
  className?: string
}) {
  const { validationResult } = useValidationContext()
  const navigate = useNavigate()

  // エラーメッセージをプレーンな配列に変換する。
  const errorInfos = React.useMemo(() => {

    // IDから当該要素の名前を引き当てるための辞書
    const xmlElements = getValues(`xmlElementTrees`)?.flatMap(x => x.xmlElements) ?? []
    const idToName = new Map(xmlElements.map(x => [x.uniqueId, x.localName]))

    const infos: Array<{ id: string, message: string }> = []
    for (const [id, obj] of Object.entries(validationResult)) {
      const name = idToName.get(id)

      for (const [objKey, attrMessages] of Object.entries(obj)) {
        const attrName = objKey === '_own' ? '' : `${objKey}.`
        infos.push(...attrMessages.map(x => ({
          id,
          message: `${name ?? ''}${attrName}: ${x}`,
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
        navigate(getNavigationUrl({ aggregateId }))
        return
      }
    }
  }

  return (
    <ul className={`block overflow-y-auto ${className ?? ''}`}>
      {errorInfos.map((info, i) => (
        <ErrorMessage key={i} onClick={() => handleClick(info.id)}>
          {info.message}
        </ErrorMessage>
      ))}
    </ul>
  )
}

const ErrorMessage = ({ children, onClick }: {
  children: React.ReactNode
  onClick: () => void
}) => {
  return (
    <li
      className="text-sm truncate text-amber-600 cursor-pointer hover:bg-amber-50"
      onClick={onClick}
    >
      {children}
    </li>
  )
}

import React from "react"
import useEvent from "react-use-event-hook"
import { asTree, SchemaDefinitionGlobalState } from "./types"
import { SERVER_DOMAIN } from "../../routes"

/** スキーマ定義のバリデーション機能 */
export const useValidation = (
  /** 編集中の最新の値を取得する関数 */
  getEditingValues: () => SchemaDefinitionGlobalState
) => {
  // 短時間で繰り返し実行するとサーバーに負担がかかるため、
  // 最後にリクエストした時間から一定時間以内はリクエストをしないようにする。
  const [isRequestPrevented, setIsRequestPrevented] = React.useState(false)

  // サーバーから返ってくる検証結果。
  // 独特な形をしているのでReact Hook Formのエラーとは別に管理する。
  const [validationResult, setValidationResult] = React.useState<ValidationResult>({})

  // 入力検証を実行する。
  const trigger: ValidationTriggerFunction = useEvent(async () => {
    // 最後にリクエストした時間から一定時間以内はリクエストをしない
    if (isRequestPrevented) return;
    setIsRequestPrevented(true)
    window.setTimeout(() => {
      setIsRequestPrevented(false)
    }, 1000)

    // サーバーに問い合わせ。ステータスコード202ならエラーあり。200ならエラーなしなのでエラーをクリアする。
    const result = await fetch(`${SERVER_DOMAIN}/validate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(getEditingValues()),
    })
    if (result.status === 202) {
      const errors: ValidationResult = await result.json()
      setValidationResult(errors)
    } else {
      setValidationResult({})
    }
  })

  // 特定のXML要素に対する検証結果を取得する処理
  const getValidationResult: GetValidationResultFunction = React.useCallback(id => {
    return validationResult?.[id] ?? { _own: [] }
  }, [validationResult])

  // 検証結果を表形式で表示するときのためのデータを生成する。
  const validationResultList = React.useMemo(() => {
    const xmlElementTrees = getEditingValues().xmlElementTrees ?? []
    if (!xmlElementTrees.length) return []

    // IDから当該要素の情報を引き当てるための辞書
    const elementMap = new Map(xmlElementTrees.flatMap(tree => tree.xmlElements).map(el => [el.uniqueId, el]))
    const treeUtilsMap = new Map(xmlElementTrees.map(tree => [tree, asTree(tree.xmlElements)]))

    const infos: ValidationResultListItem[] = []

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
  }, [getEditingValues, validationResult])

  return {
    /** 検証結果を表形式で表示するときのためのデータ */
    validationResultList,
    /** 特定のXML要素に対する検証結果を取得する関数 */
    getValidationResult,
    /** 検証を実行する */
    trigger,
  }
}

/**
 * サーバーから返ってくる検証結果の型。
 * この形は、C#側の ToReactErrorObject で生成されるJsonObjectの型と一致する。
 */
export type ValidationResult = {
  [uniqueId: string]: ValidationResultToElement
}

/**
 * 特定のXML要素に対する検証結果を取得する関数。
 * @param id 対象のXML要素のID
 */
export type GetValidationResultFunction = (id: string) => ValidationResultToElement

/**
 * 検証を実行する関数。
 */
export type ValidationTriggerFunction = () => Promise<void>

/**
 * 検証結果を表形式で表示するときのための1行分のデータ。
 */
export type ValidationResultListItem = {
  id: string
  rootAggregateName: string
  elementName: string
  attributeName: string
  message: string
}

/**
 * サーバーから返ってくる検証結果のうち特定のXML要素に対するもの。
 */
export type ValidationResultToElement = {
  /** この要素自体に対するエラー */
  _own: string[]
  /** この要素の属性に対するエラー */
  [attributeName: string]: string[]
}

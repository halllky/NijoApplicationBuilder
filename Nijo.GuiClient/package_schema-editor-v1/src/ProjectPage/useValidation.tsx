import React from "react"
import * as ReactHookForm from "react-hook-form"
import * as ReactRouter from "react-router-dom"
import { EditingProject, ValidationErrorMap } from "../backend"
import { validateProject } from "../backend/api"
import { NIJOUI_CLIENT_ROUTE_PARAMS } from "../routing"

//#region 内部コンテキスト

type ValidationContextType = {
  errorFlagContext: {
    subscribe: (xmlElementUniqueId: string, attributeName: string | undefined, setHasError: (value: ErrorStateByField) => void) => void
    unsubscribe: (setHasError: (value: ErrorStateByField) => void) => void
  }
  resultListContext: {
    subscribe: (setResultList: (value: ValidationResultListItem[]) => void) => void
    unsubscribe: (setResultList: (value: ValidationResultListItem[]) => void) => void
  }
}

type HasErrorSubscriber = {
  xmlElementUniqueId: string
  attributeName: string | undefined
  previousHasError: boolean
}

type ErrorStateByField = {
  hasError: boolean
  errorMessages: string[]
}

const ValidationContextInternal = React.createContext<ValidationContextType>({
  errorFlagContext: {
    subscribe: () => { },
    unsubscribe: () => { },
  },
  resultListContext: {
    subscribe: () => { },
    unsubscribe: () => { },
  },
})

//#endregion 内部コンテキスト


//#region プロバイダー

export function ValidationContextProvider(props: {
  watch: ReactHookForm.UseFormWatch<EditingProject>
  children?: React.ReactNode
}) {

  // watchを使って変更検知を行う
  const [isTriggered, setIsTriggered] = React.useState(false)
  const watchedValuesRef = React.useRef<ReactHookForm.DeepPartial<EditingProject> | null>(null)
  React.useEffect(() => {
    const subscription = props.watch(values => {
      watchedValuesRef.current = values
      setIsTriggered(true)
    })
    return () => subscription.unsubscribe()
  }, [props.watch])

  // 短時間で繰り返し実行するとサーバーに負担がかかるため、
  // 前のリクエストが完了するまでは新しいリクエストをしないようにする。
  const [isValidating, setIsValidating] = React.useState(false)

  // エラー状態変更時に最小限のコンポーネントのみ再レンダリングするための購読機能
  const resultListSubscribersRef = React.useRef(new Set<(value: ValidationResultListItem[]) => void>())
  const hasErrorSubscribersRef = React.useRef(new Map<(value: ErrorStateByField) => void, HasErrorSubscriber>())

  // 最新の検証結果を保持
  const latestStateRef = React.useRef<{ errors: ValidationErrorMap, validationResultList: ValidationResultListItem[] }>({ errors: {}, validationResultList: [] })

  // 再レンダリングの抑制のため、コンテキストの値は常に同じオブジェクトを返すようにする。
  const contextValue = React.useMemo((): ValidationContextType => ({
    errorFlagContext: {
      subscribe: (xmlElementUniqueId, attributeName, setHasError) => {
        hasErrorSubscribersRef.current.set(setHasError, {
          xmlElementUniqueId,
          attributeName,
          previousHasError: false,
        })
        // 登録時に現在の状態を即座に反映
        const fieldErrors = latestStateRef.current.errors?.[xmlElementUniqueId]?.[attributeName ?? '_own'] ?? []
        const hasError = fieldErrors.length > 0
        if (hasError) {
          setHasError({ hasError: true, errorMessages: fieldErrors })
          const subscriber = hasErrorSubscribersRef.current.get(setHasError)
          if (subscriber) subscriber.previousHasError = true
        }
      },
      unsubscribe: setHasError => hasErrorSubscribersRef.current.delete(setHasError),
    },
    resultListContext: {
      subscribe: setResultList => {
        resultListSubscribersRef.current.add(setResultList)
        // 登録時に現在の状態を即座に反映
        setResultList(latestStateRef.current.validationResultList)
      },
      unsubscribe: setResultList => resultListSubscribersRef.current.delete(setResultList),
    },
  }), [])

  // サーバーへの問い合わせを実行
  const [searchParams] = ReactRouter.useSearchParams()
  const projectDir = searchParams.get(NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR)
  React.useEffect(() => {
    if (!isTriggered) return
    if (isValidating) return

    setIsValidating(true);
    setIsTriggered(false);

    (async () => {
      try {
        const result = await validateProject(projectDir, watchedValuesRef.current as EditingProject, new AbortController().signal)
        const errors: ValidationErrorMap = result.ok ? result.value : {}

        // メッセージ一覧
        const validationResultList = convertToValidationResultListItemList(watchedValuesRef.current as EditingProject, errors)
        for (const setResultList of resultListSubscribersRef.current) {
          setResultList(validationResultList)
        }

        // フィールドごとのエラー情報
        for (const [setHasError, fieldInfo] of hasErrorSubscribersRef.current.entries()) {
          const fieldErrors = errors?.[fieldInfo.xmlElementUniqueId]?.[fieldInfo.attributeName ?? '_own'] ?? []
          const hasError = fieldErrors.length > 0
          if (hasError) {
            setHasError({ hasError: true, errorMessages: fieldErrors })
            fieldInfo.previousHasError = true
          }
          if (!hasError && fieldInfo.previousHasError) {
            setHasError({ hasError: false, errorMessages: [] })
            fieldInfo.previousHasError = false
          }
        }

        // 最新の状態を保存
        latestStateRef.current = { errors, validationResultList }

      } finally {
        setIsValidating(false)
      }
    })();
  }, [isTriggered, isValidating, projectDir])

  return (
    <ValidationContextInternal.Provider value={contextValue}>
      {props.children}
    </ValidationContextInternal.Provider>
  )
}

//#endregion プロバイダー


//#region 特定フィールドのエラー有無

/**
 * 特定のフィールドにエラーがあるかどうかを返すReact Hook。
 *
 * @param xmlElementUniqueId XML要素のユニークID
 * @param attributeName 属性名。XML要素自体のエラーを知りたいときは省略する。
 */
export function useFieldValidationError(xmlElementUniqueId: string | null | undefined, attributeName?: string): ErrorStateByField {
  const { errorFlagContext } = React.useContext(ValidationContextInternal)
  const [errorState, setErrorState] = React.useState<ErrorStateByField>(() => ({ hasError: false, errorMessages: [] }))

  React.useEffect(() => {
    if (!xmlElementUniqueId) {
      setErrorState({ hasError: false, errorMessages: [] })
      return;
    }

    errorFlagContext.subscribe(xmlElementUniqueId, attributeName, setErrorState)
    return () => errorFlagContext.unsubscribe(setErrorState)
  }, [errorFlagContext, xmlElementUniqueId, attributeName])

  return errorState
}

//#endregion 特定フィールドのエラー有無


//#region エラーメッセージ表示

/**
 * 検証結果を表形式で表示するときのための1行分のデータ。
 */
export type ValidationResultListItem = {
  xmlElementUniqueId: string
  rootAggregateName: string
  rootAggregateUniqueId: string
  elementName: string
  attributeName: string
  message: string
}

/**
 * 検証結果を表形式で表示するときのためのデータを生成するReact Hook。
 */
export function useValidationErrorMessages(): ValidationResultListItem[] {
  const { resultListContext } = React.useContext(ValidationContextInternal)
  const [validationResultList, setValidationResultList] = React.useState<ValidationResultListItem[]>([])

  React.useEffect(() => {
    resultListContext.subscribe(setValidationResultList)
    return () => resultListContext.unsubscribe(setValidationResultList)
  }, [resultListContext])

  return validationResultList
}

/**
 * サーバーから返ってくる検証結果を、表形式で表示するときのためのデータに変換する。
 * ルート集約・メンバーはそれぞれのリスト（dataStructures等）に既に分類済みなので、
 * 旧実装のようにツリーを遡ってルートを探す必要はない。
 */
function convertToValidationResultListItemList(state: EditingProject, validationResult: ValidationErrorMap): ValidationResultListItem[] {

  const customAttributes = state.customAttributes ?? []
  const customAttributeMap = new Map(customAttributes.map(ca => [ca.uniqueId, ca]))

  // uniqueIdから、それが属するルート集約の情報とその要素自身の名前を引き当てるための辞書
  const elementMap = new Map<string, { elementName: string, rootAggregateName: string, rootAggregateUniqueId: string }>()
  for (const roots of [state.dataStructures, state.commands, state.staticEnums, state.valueObjects, state.constants]) {
    for (const root of roots ?? []) {
      const rootInfo = { rootAggregateName: root.physicalName ?? '', rootAggregateUniqueId: root.uniqueId }
      elementMap.set(root.uniqueId, { elementName: root.physicalName ?? '', ...rootInfo })
      for (const member of root.members) {
        elementMap.set(member.uniqueId, { elementName: member.physicalName ?? '', ...rootInfo })
      }
    }
  }

  const infos: ValidationResultListItem[] = []

  for (const [xmlElementUniqueId, obj] of Object.entries(validationResult ?? {})) {
    // 集約定義のエラー
    const element = elementMap.get(xmlElementUniqueId)
    if (element) {
      for (const [objKey, attrMessages] of Object.entries(obj)) {

        let attributeName: string
        if (objKey === '_own') {
          // この要素自体に対するエラー
          attributeName = ''
        } else {
          // カスタム属性のエラーならばカスタム属性の名前を、
          // そうでなければ属性名をそのまま使う
          const customAttribute = customAttributes.find(ca => ca.uniqueId === objKey)
          attributeName = customAttribute?.displayName ?? customAttribute?.physicalName ?? objKey
        }

        infos.push(...attrMessages.map(x => ({
          xmlElementUniqueId,
          rootAggregateName: element.rootAggregateName,
          rootAggregateUniqueId: element.rootAggregateUniqueId,
          elementName: element.elementName,
          attributeName,
          message: x,
        }) satisfies ValidationResultListItem))
      }
      continue
    }

    // カスタム属性定義のエラー
    const customAttribute = customAttributeMap.get(xmlElementUniqueId)
    if (customAttribute) {
      for (const [objKey, attrMessages] of Object.entries(obj)) {
        const attributeName = objKey === '_own' ? '' : objKey
        infos.push(...attrMessages.map(x => ({
          xmlElementUniqueId,
          rootAggregateName: 'カスタム属性',
          rootAggregateUniqueId: customAttribute.uniqueId ?? '',
          elementName: customAttribute.physicalName ?? '',
          attributeName,
          message: x,
        }) satisfies ValidationResultListItem))
      }
      continue
    }
  }
  return infos
}
//#endregion エラーメッセージ表示

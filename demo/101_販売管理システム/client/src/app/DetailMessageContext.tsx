import React from "react"
import * as ReactHookForm from "react-hook-form"

//#region メッセージのデータ構造

/**
 * サーバー側の PresentationContext がクライアント側に返されるときの detail プロパティの構造。
 * エラー、警告、情報メッセージを階層的に保持する。
 */
export type PresentationContextDetail = {
  /** この項目に対して発生したエラーメッセージの一覧。 */
  error?: string[]
  /** この項目に対して発生した警告メッセージの一覧。 */
  warn?: string[]
  /** この項目に対して発生した情報メッセージの一覧。 */
  info?: string[]
  /**
   * 子要素のメッセージ。
   *
   * 配列（nijo.xmlで言う Type="children"）の場合、
   * オブジェクトのキーは半角数字のみで構成されており、
   * 各キーは配列のインデックスを表す。
   */
  children?: { [key: string]: PresentationContextDetail }
}

//#endregion メッセージのデータ構造

//#region 全体

/**
 * メッセージコンテナのコンテキスト。
 * 各画面側で取り扱いやすいAPIを提供する。
 */
export type DetailMessageContextType = {
  /**
   * メッセージ全体を引数の値で置き換える。現在表示されているメッセージはすべてクリアされる。
   * @param detail complexPost のレスポンスとしてサーバーから返されたオブジェクトの detail プロパティの値。
   */
  replaceMessages: (detail: PresentationContextDetail | null | undefined) => void
  /**
   * メッセージ全体に引数の値を追加する。現在表示されているメッセージは保持される。
   * @param detail complexPost のレスポンスとしてサーバーから返されたオブジェクトの detail プロパティの値。
   */
  appendMessages: (detail: PresentationContextDetail | null | undefined) => void
  /**
   * 現在表示されているメッセージをすべてクリアする。
   */
  clearMessages: () => void
}

/**
 * 内部コンテキストの型
 */
type DetailMessageContextTypeInternal = {
  /**
   * 内部コンテキストの末端コンポーネントがプロバイダーに対してどのフィールドに対するエラーメッセージを表示するかを登録する。
   * コンテキストプロバイダーは、エラーメッセージ表示のタイミングで、ここで登録された情報の中から
   * 各フィールド名にマッチするものを探し出して、対応する setter 関数を呼び出す。
   */
  register: (registration: FieldRegistration) => void
  /**
   * register で登録した内容を解除する。
   * 末端コンポーネントのアンマウント時に呼び出される必要がある。
   */
  unregister: (registration: FieldRegistration) => void
}

/** 内部コンテキストの末端コンポーネントがプロバイダーに対して行う登録 */
type FieldRegistration = {
  /** 末端コンポーネントで宣言される useState の setter 関数 */
  messageSetter: (messages: DetailMessageByField | null) => void
} & (
    // どこにも表示されないメッセージを表示することを表す
    | { type: 'unregistered' }
    // 特定のnameに対するメッセージを表示することを表す
    | { type: 'exact', name: string }
    // 特定のnameおよびその子孫に対するメッセージを表示することを表す
    | { type: 'forwardMatch', name: string }
  )

/**
 * 外部公開コンテキスト
 */
const DetailMessageContext = React.createContext<DetailMessageContextType>({
  replaceMessages: () => { throw new Error('DetailMessageContext を配置してください。') },
  appendMessages: () => { throw new Error('DetailMessageContext を配置してください。') },
  clearMessages: () => { throw new Error('DetailMessageContext を配置してください。') },
})

/**
 * 内部コンテキスト
 */
const DetailMessageContextInternal = React.createContext<DetailMessageContextTypeInternal>({
  register: () => { throw new Error('DetailMessageContextInternal を配置してください。') },
  unregister: () => { throw new Error('DetailMessageContextInternal を配置してください。') },
})

/**
 * complexPost で detail が返されたときに使用
 */
export function useSetter() {
  return React.useContext(DetailMessageContext)
}

/**
 * Query Model や Command Model の処理で発生した各項目に対するエラー等のメッセージ
 * （complexPost のレスポンスとしてサーバー側の PresentationContext がクライアント側に返した detail プロパティ）
 * を表示するためのコンテキスト。各画面側で取り扱いやすいAPIを提供する。
 */
export function Provider(props: { children: React.ReactNode }) {

  // コンテキスト内に未登録メッセージ表示箇所がどこにも無い場合にプロバイダー直下にメッセージを表示するための状態
  const [unregisteredMessages, setUnregisteredMessages] = React.useState<DetailMessageByField>({})

  // 内部コンテキスト用の登録情報。
  // エラーメッセージなどの表示時、この登録情報をもとに各フィールド名に対応する setter 関数を探し出して呼び出す。
  const registeredRef = React.useRef<FieldRegistration[]>([])

  // 内部コンテキストの末端コンポーネントに対して公開する登録関数
  const internalContextValue: DetailMessageContextTypeInternal = React.useMemo(() => ({
    register: request => {
      registeredRef.current.push(request)
    },
    unregister: request => {
      registeredRef.current = registeredRef.current.filter(item => item !== request)
    },
  }), [registeredRef])

  // 外部公開API
  const publicContextValue: DetailMessageContextType = React.useMemo(() => {
    const clearMessages = () => {
      for (const registerInfo of registeredRef.current) {
        registerInfo.messageSetter(null)
      }
      setUnregisteredMessages({})
    }

    const replaceMessages = (detail: PresentationContextDetail | null | undefined) => {
      clearMessages()
      appendMessages(detail)
    }

    const appendMessages = (detail: PresentationContextDetail | null | undefined) => {
      // どこに表示されないメッセージはここに蓄積させておいて最後にまとめて表示
      const unregisteredCache: DetailMessageByField = {}

      // 同じメッセージセッターに複数のメッセージが送られる場合に備えて、
      // 各メッセージセッターへの通知をここでバッファリングする
      const messageCache = new Map<(messages: DetailMessageByField | null) => void, DetailMessageByField>()
      const addToCache = (setter: (messages: DetailMessageByField | null) => void, msg: DetailMessageByField) => {
        const current = messageCache.get(setter) ?? {}
        messageCache.set(setter, {
          error: [...(current.error ?? []), ...(msg.error ?? [])],
          warn: [...(current.warn ?? []), ...(msg.warn ?? [])],
          info: [...(current.info ?? []), ...(msg.info ?? [])],
        })
      }

      // 構造化されたオブジェクトを平坦化
      const flatMap = flattenDetailMessages(detail)

      // 平坦化されたメッセージを順に処理し、
      // それぞれどこに表示するかを判定して setter 関数を呼び出す。
      const exactMatchMap = new Map(registeredRef.current
        .filter(item => item.type === 'exact')
        .map(item => [item.name, item]))
      const forwardMatcheList = registeredRef.current
        .filter(item => item.type === 'forwardMatch')
        .map(item => ({ splittedName: item.name.split('.'), ...item }))

      for (const [nameSplittedByPeriod, messageByField] of flatMap) {

        // nameの完全一致が登録されている場合はそこに優先的に表示
        const name = nameSplittedByPeriod.join('.')
        const exactMatch = exactMatchMap.get(name)
        if (exactMatch) {
          addToCache(exactMatch.messageSetter, messageByField)
          continue
        }

        // 無い場合は前方一致を探す。
        // 前方一致でヒットした登録情報のうち、最も長いものに表示。
        // 最も長いものが複数ある場合は最初に登場した登録情報の箇所に表示。
        // また、ヒットした以降の部分もエラーメッセージに含める。
        // 例: 項目 "a.b.c.d" に対してエラーメッセージが発生しているとき、
        //    "a.b" と "a.b.c" が登録されている場合は "a.b.c" にこれを表示する。
        const forwardMatchCandidates = forwardMatcheList.map(registration => {
          if (registration.splittedName.length > nameSplittedByPeriod.length) {
            return { match: false, hitLength: -1, rest: [], messageSetter: registration.messageSetter }
          }
          for (let i = 0; i < registration.splittedName.length; i++) {
            if (registration.splittedName[i] !== nameSplittedByPeriod[i]) {
              return { match: false, hitLength: -1, rest: [], messageSetter: registration.messageSetter }
            }
          }
          return {
            match: true,
            hitLength: registration.splittedName.length,
            rest: nameSplittedByPeriod.slice(registration.splittedName.length),
            messageSetter: registration.messageSetter,
          }
        }).filter(x => x.match)

        if (forwardMatchCandidates.length > 0) {
          // 上記でヒットした登録情報のうち最もパスが長いものを探す
          let bestCandidate = forwardMatchCandidates[0]
          for (const candidate of forwardMatchCandidates) {
            if (candidate.hitLength > bestCandidate.hitLength) {
              bestCandidate = candidate
            }
          }

          // nameの残り部分をメッセージの先頭に付与してセットする。
          // 半角数値の場合は配列インデックスなので「x行目」という文字に変換する。
          let prefix = bestCandidate.rest
            .map(part => /^\d+$/.test(part) ? `${Number(part) + 1}行目` : part)
            .join(' ')
          if (prefix.length > 0) {
            prefix += ': '
          }
          addToCache(bestCandidate.messageSetter, {
            error: messageByField.error?.map(msg => prefix + msg),
            warn: messageByField.warn?.map(msg => prefix + msg),
            info: messageByField.info?.map(msg => prefix + msg),
          })
          continue
        }

        // どこにも登録されていないnameの場合はオブジェクトに溜めておいて最後にまとめてセットする。
        // 該当のフィールドまでのパスをメッセージに含める。
        let prefix = nameSplittedByPeriod
          .map(part => /^\d+$/.test(part) ? `${Number(part) + 1}行目` : part)
          .join(' ')
        if (prefix.length > 0) {
          prefix += ': '
        }
        if (messageByField.error) {
          unregisteredCache.error = [...(unregisteredCache.error ?? []), ...messageByField.error.map(msg => prefix + msg)]
        }
        if (messageByField.warn) {
          unregisteredCache.warn = [...(unregisteredCache.warn ?? []), ...messageByField.warn.map(msg => prefix + msg)]
        }
        if (messageByField.info) {
          unregisteredCache.info = [...(unregisteredCache.info ?? []), ...messageByField.info.map(msg => prefix + msg)]
        }
      }

      // どこにも登録されていないname用のメッセージをセットする。
      // コンテキスト内部で登録されている場合は最後に登録された箇所に表示し、
      // どこにも無い場合はこのコンポーネントのstateにセットする。
      const unregistered = registeredRef.current.filter(item => item.type === 'unregistered')
      if (unregistered.length > 0) {
        addToCache(unregistered[unregistered.length - 1].messageSetter, unregisteredCache)
      } else {
        setUnregisteredMessages(prev => ({
          error: [...(prev.error ?? []), ...(unregisteredCache.error ?? [])],
          warn: [...(prev.warn ?? []), ...(unregisteredCache.warn ?? [])],
          info: [...(prev.info ?? []), ...(unregisteredCache.info ?? [])],
        }))
      }

      // stateへの反映
      for (const [setter, message] of messageCache) {
        setter(message)
      }
    }

    return { replaceMessages, appendMessages, clearMessages }
  }, [registeredRef])

  return (
    <DetailMessageContextInternal.Provider value={internalContextValue}>
      <DetailMessageContext.Provider value={publicContextValue}>

        {/* コンテキスト内に未登録メッセージ表示箇所がどこにも無い場合はプロバイダー直下にメッセージを表示する */}
        {(unregisteredMessages.error || unregisteredMessages.warn || unregisteredMessages.info) && (
          <ul>
            {unregisteredMessages.error && unregisteredMessages.error.map((msg, idx) => (
              <li key={`error-${idx}`} className="text-sm text-rose-700">{msg}</li>
            ))
            }
            {unregisteredMessages.warn && unregisteredMessages.warn.map((msg, idx) => (
              <li key={`warn-${idx}`} className="text-sm text-amber-700">{msg}</li>
            ))}
            {unregisteredMessages.info && unregisteredMessages.info.map((msg, idx) => (
              <li key={`info-${idx}`} className="text-sm text-sky-700">{msg}</li>
            ))}
          </ul>
        )}

        {props.children}
      </DetailMessageContext.Provider>
    </DetailMessageContextInternal.Provider>
  )
}

//#endregion 全体

//#region 項目単位のメッセージ

/**
 * 特定の項目に対するメッセージ（エラー・警告・情報）
 */
type DetailMessageByField = Omit<PresentationContextDetail, 'children'>

/**
 * Query Model や Command Model の処理で発生した各項目に対するエラー等のメッセージ
 * （complexPost のレスポンスとしてサーバー側の PresentationContext がクライアント側に返した detail プロパティ）
 * を表示するためのコンポーネント。
 *
 * react hook form を利用して、そのフォームに存在する項目の名前を型安全に扱うことができる。
 */
export function Of<
  TFieldValues extends ReactHookForm.FieldValues,
  TFieldPath extends ReactHookForm.FieldPath<TFieldValues>
>(props: {
  /** この項目に対して発生したメッセージをこの箇所に表示する。 */
  name: TFieldPath
  /** nameで指定した項目に加えて、その子孫の項目に発生したメッセージも含めるかどうか */
  includeDescendants?: boolean
  /** useForm の control オブジェクト。 name を型安全に扱うために必要。 */
  control: ReactHookForm.Control<TFieldValues>
  className?: string
}) {

  const internalContext = React.useContext(DetailMessageContextInternal)
  const [messages, setMessages] = React.useState<DetailMessageByField | null>(null)

  React.useEffect(() => {
    // このコンポーネントと対象項目のnameを紐づける
    const registration: FieldRegistration = props.includeDescendants
      ? { type: 'forwardMatch', name: props.name, messageSetter: setMessages }
      : { type: 'exact', name: props.name, messageSetter: setMessages }

    internalContext.register(registration)
    return () => internalContext.unregister(registration)
  }, [props.name])

  if (!messages) {
    return null
  }

  return (
    <ul className={props.className}>
      {messages?.error && messages.error.map((msg, idx) => (
        <li key={`error-${idx}`} className="text-sm text-rose-700">{msg}</li>
      ))}
      {messages?.warn && messages.warn.map((msg, idx) => (
        <li key={`warn-${idx}`} className="text-sm text-amber-700">{msg}</li>
      ))}
      {messages?.info && messages.info.map((msg, idx) => (
        <li key={`info-${idx}`} className="text-sm text-sky-700">{msg}</li>
      ))}
    </ul>
  )
}

//#endregion 項目単位のメッセージ

//#region どこにも登録されていないname用

/**
 * Query Model や Command Model の処理で発生した各項目に対するエラー等のメッセージ
 * （complexPost のレスポンスとしてサーバー側の PresentationContext がクライアント側に返した detail プロパティ）
 * のうち、どこにも登録されていないname用に表示するためのコンポーネント。
 *
 * コンテキスト内のどこにもこのコンポーネントが存在しない場合、未登録のメッセージはプロバイダー直下に表示される。
 */
export function Rest(props: { className?: string }) {
  const internalContext = React.useContext(DetailMessageContextInternal)
  const [messages, setMessages] = React.useState<DetailMessageByField | null>(null)

  React.useEffect(() => {
    internalContext.register({ type: 'unregistered', messageSetter: setMessages })
    return () => internalContext.unregister({ type: 'unregistered', messageSetter: setMessages })
  }, [])

  if (!messages) {
    return null
  }

  return (
    <ul className={props.className}>
      {messages?.error && messages.error.map((msg, idx) => (
        <li key={`error-${idx}`} className="text-sm text-rose-700">{msg}</li>
      ))}
      {messages?.warn && messages.warn.map((msg, idx) => (
        <li key={`warn-${idx}`} className="text-sm text-amber-700">{msg}</li>
      ))}
      {messages?.info && messages.info.map((msg, idx) => (
        <li key={`info-${idx}`} className="text-sm text-sky-700">{msg}</li>
      ))}
    </ul>
  )
}

//#endregion どこにも登録されていないname用

//#region ユーティリティ
/**
 * 詳細メッセージオブジェクトを平坦化する。例:
 *
 * ```
 * // 変換前
 * {
 *   error: ['ルートエラー'],
 *   children: {
 *     '1': {
 *       children: {
 *         aaa: { error: ['エラー1'], info: ['情報1'] }
 *       }
 *     }
 *   }
 * }
 *
 * // 変換後
 * Map {
 *   '': { error: ['ルートエラー'] },
 *   '1.aaa': { error: ['エラー1'], info: ['情報1'] }
 * }
 * ```
 */
function flattenDetailMessages(detail: PresentationContextDetail | null | undefined): [nameSplittedByPeriod: string[], messages: DetailMessageByField][] {
  const result: [nameSplittedByPeriod: string[], messages: DetailMessageByField][] = []
  if (!detail) return result

  const traverse = (current: PresentationContextDetail, prefix: string[]) => {
    const { children, ...messages } = current
    const hasMessage = (messages.error?.length ?? 0) > 0
      || (messages.warn?.length ?? 0) > 0
      || (messages.info?.length ?? 0) > 0

    if (hasMessage) {
      result.push([prefix, messages])
    }

    if (children) {
      for (const key of Object.keys(children)) {
        const nextPrefix = [...prefix, key]
        traverse(children[key], nextPrefix)
      }
    }
  }

  traverse(detail, [])
  return result
}

//#endregion ユーティリティ


/** カスタム属性定義 */
export type NijoXmlCustomAttribute = {
  uniqueId: XmlElementAttributeName
  physicalName: string
  displayName: string
  comment?: string
  isValidation: boolean
  availableModels: string[]
  type: 'Boolean' | 'Decimal' | 'Enum' | 'String'
  enumValues: string[]
}

/** Model定義画面のデータ型定義 */
export type ModelPageForm = {
  /**
   * そのXMLツリーの要素の一覧。
   * 以下は保証されているものとする。
   *
   * - 配列の要素数が1以上であること
   * - 先頭の要素がインデント0、以降の要素がインデント1以上であること
   * - インデントが0の要素のTypeは、モデルを表す種類であること
   * - インデントが1以上の要素のTypeは、モデルを表す種類以外の種類であること
   */
  xmlElements: XmlElementItem[]
}

/** Nijo.csproj の SchemaParseRule の ValueMemberType に同じ。 */
export type ValueMemberType = {
  /** XMLスキーマ定義でこの型を指定するときの型名 */
  schemaTypeName: string
  /** この種類の画面表示上名称 */
  typeDisplayName: string
}

/** プロジェクト設定項目のメタ情報 */
export type ProjectOptionPropertyInfo = {
  propertyName: string
  description: string
  propertyType: 'string' | 'bool' | 'int'
  defaultValue?: string | boolean | number
}

/**
 * プロジェクト設定の値。
 * GeneratedProjectOptions.cs の各プロパティに対応する。
 */
export type ProjectOptions = {
  [key: string]: string | boolean | number
}

// ---------------------------------

/**
 * nijo.preview.json の内容。生成後アプリをデバッグ起動するための設定。
 * PreviewSetting.cs に対応する。
 */
export type PreviewSetting = {
  /** 起動完了時にこのURLをブラウザで開く。空文字なら開かない */
  browser: string
  /** true なら nijo serve の起動と同時にプロセス群を自動起動する */
  startOnNijoServe: boolean
  /** 並列実行するプロセスの定義 */
  concurrently: PreviewProcessSetting[]
}

/** concurrently で並列起動する1プロセスの設定 */
export type PreviewProcessSetting = {
  /** プロセスの識別名。GUI上の表示やログ取得のキーに使う */
  name: string
  process: {
    /** nijo.xml のディレクトリからの相対パス */
    cwd: string
    filename: string
    args: string
  }
  log: {
    /** 標準出力の書き出し先。nijo.xml のディレクトリからの相対パス。空文字ならログファイルを作らない */
    stdout: string
    /** 標準エラー出力の書き出し先。nijo.xml のディレクトリからの相対パス。空文字ならログファイルを作らない */
    stderr: string
    /** true=起動の度に追記 / false=起動の度にクリア */
    appendStdout: boolean
    /** true=起動の度に追記 / false=起動の度にクリア */
    appendStderr: boolean
  }
  /** コード再生成が成功した直後にこのプロセスを再起動するか */
  restartOnGenerateCode: boolean
}

/** 稼働中の1プロセスの状態（/api/preview/state のレスポンス） */
export type PreviewProcessState = {
  name: string
  isRunning: boolean
  processId: number | null
  exitCode: number | null
  stdout: PreviewLogIncrement
  stderr: PreviewLogIncrement
}

/** ログファイルの指定オフセット以降の増分 */
export type PreviewLogIncrement = {
  text: string
  offset: number
}

// ---------------------------------

/** XML要素1個分と対応するデータ型 */
export type XmlElementItem = {
  /** XML要素を一意に識別するID */
  uniqueId: string
  /** インデントレベル。XML要素の親子関係は保存時にインデントをもとに再構築する。 */
  indent: number
  /** XML要素のローカル名 */
  localName?: string
  /** XML要素の値 */
  value?: string
  /** XML要素の属性 */
  attributes: Record<XmlElementAttributeName, string>
  /** コメント */
  comment?: string
}

/** XML要素の属性の識別子 */
export type XmlElementAttributeName = string & { _brand: 'XmlElementAttributeName' }

/** XML要素の属性の種類定義 */
export type XmlElementAttribute = {
  /** この属性の識別子。XML要素の属性名になる。 */
  attributeName: XmlElementAttributeName
  /** この属性の画面表示上の名称。 */
  displayName: string
  /** この属性が使用可能なモデルとノード種別の組み合わせの配列。 */
  availableElements: { model: string, nodeType: NijoXmlNodeType }[]
} & (XmlElementStringAttribute | XmlElementBoolAttribute | XmlElementIntegerAttribute | XmlElementSelectAttribute | XmlElementEnumSelectAttribute)

/** XML要素の属性の種類定義（文字列属性） */
export type XmlElementStringAttribute = {
  type: 'String'
}
/** XML要素の属性の種類定義（ブール属性） */
export type XmlElementBoolAttribute = {
  type: 'Boolean'
}
export type XmlElementIntegerAttribute = {
  type: 'Integer'
}
export type XmlElementEnumSelectAttribute = {
  type: 'EnumSelect'
  typeEnumValues: string[]
}
/** XML要素の属性の種類定義（選択属性） */
export type XmlElementSelectAttribute = {
  type: 'XmlNodeType'
}

// ---------------------------------

export const ATTR_TYPE = 'Type' as XmlElementAttributeName
export const ATTR_UNIQUE_CONSTRAINTS = 'UniqueConstraints' as XmlElementAttributeName
export const ATTR_DISPLAY_NAME = 'DisplayName' as XmlElementAttributeName
export const ATTR_PARAMETER = 'Parameter' as XmlElementAttributeName
export const ATTR_RETURN_VALUE = 'ReturnValue' as XmlElementAttributeName
export const ATTR_CONSTANT_TYPE = 'ConstantType' as XmlElementAttributeName
export const ATTR_CONSTANT_VALUE = 'ConstantValue' as XmlElementAttributeName
export const ATTR_IS_GENERIC_LOOKUP_TABLE = 'IsGenericLookupTable' as XmlElementAttributeName
export const ATTR_IS_HARD_CODED_PRIMARY_KEY = 'IsHardCodedPrimaryKey' as XmlElementAttributeName

// ---------------------------------

/** 汎用参照テーブルの1カテゴリ分のデータ */
export type GenericLookupTableCategoryItem = {
  /** カテゴリ名（XML要素名）例: "Countries" */
  name: string
  /** 表示用名称 例: "国・地域区分" */
  displayName: string
  /** ハードコードされるキーの値: UniqueId → Value のマッピング */
  hardCodedKeyValues: { [uniqueId: string]: string }
}

/** 汎用参照テーブル1個分のカテゴリ定義データ */
export type GenericLookupTableCategoriesData = {
  /** 対象ルート集約のUniqueId */
  for: string
  /** カテゴリ一覧 */
  categories: GenericLookupTableCategoryItem[]
}

export const TYPE_DATA_MODEL = 'data-model'
export const TYPE_QUERY_MODEL = 'query-model'
export const TYPE_COMMAND_MODEL = 'command-model'
export const TYPE_STRUCTURE_MODEL = 'structure-model'
export const TYPE_CONSTANT_MODEL = 'constant-model'
export const TYPE_STATIC_ENUM_MODEL = 'enum'
export const TYPE_VALUE_OBJECT_MODEL = 'value-object'
export const TYPE_CHILD = 'child'
export const TYPE_CHILDREN = 'children'

// ノード種別（C#のE_NodeTypeに対応）
export const NODE_TYPE_ROOT_AGGREGATE = 'RootAggregate'
export const NODE_TYPE_CHILD_AGGREGATE = 'ChildAggregate'
export const NODE_TYPE_CHILDREN_AGGREGATE = 'ChildrenAggregate'
export const NODE_TYPE_VALUE_MEMBER = 'ValueMember'
export const NODE_TYPE_REF = 'Ref'
export const NODE_TYPE_STATIC_ENUM_VALUE = 'StaticEnumValue'
export const NODE_TYPE_UNKNOWN = 'Unknown'

export type NijoXmlNodeType =
  | typeof NODE_TYPE_ROOT_AGGREGATE
  | typeof NODE_TYPE_CHILD_AGGREGATE
  | typeof NODE_TYPE_CHILDREN_AGGREGATE
  | typeof NODE_TYPE_VALUE_MEMBER
  | typeof NODE_TYPE_REF
  | typeof NODE_TYPE_STATIC_ENUM_VALUE
  | typeof NODE_TYPE_UNKNOWN

// 定数の種類
export const CONSTANT_TYPE_CHILD = 'child'
export const CONSTANT_TYPE_STRING = 'string'
export const CONSTANT_TYPE_INT = 'int'
export const CONSTANT_TYPE_DECIMAL = 'decimal'
export const CONSTANT_TYPE_TEMPLATE = 'template'

// ---------------------------------

/**
 * 指定された属性が、指定されたモデル種別で利用可能かを判定する。
 */
export const isAttributeAvailable = (attr: XmlElementAttribute, modelType: string, nodeTypes: NijoXmlNodeType[]): boolean => {
  if (!modelType) return false
  return attr.availableElements.some(ae => ae.model === modelType && nodeTypes.includes(ae.nodeType))
}

// ---------------------------------

export type TreeHelper<TFlatItem extends { indent: number }, TKey> = ReturnType<typeof asTree<TFlatItem, TKey>>

/** 内部の状態はフラットなツリーとして保持されているが、それをツリー構造として扱うためのユーティリティ。 */
export const asTree = <TFlatItem extends { indent: number }, TKey>(flat: TFlatItem[], keySelector: (item: TFlatItem) => TKey) => {
  return {

    /** 指定された要素のルートを取得する。 */
    getRoot: (el: TFlatItem): TFlatItem => {
      // 引数のエレメント以前の位置にある、インデント0の要素のうち直近のものがルート
      const elKey = keySelector(el)
      const previousElementsAndThis = flat.slice(0, flat.findIndex(x => keySelector(x) === elKey) + 1)
      const root = previousElementsAndThis.reverse().find(x => x.indent === 0)
      if (!root) throw new Error('root not found') // 必ずルート集約はあるはず
      return root
    },

    /** 指定された要素の親を取得する。 */
    getParent: (el: TFlatItem): TFlatItem | undefined => {
      // 引数のエレメントより前の位置にあり、
      // インデントが引数のエレメントより小さいもののうち、直近にあるのが親。
      // インデントは必ずしも1小さいとは限らない。
      const elKey = keySelector(el)
      const previousElements = flat.slice(0, flat.findIndex(x => keySelector(x) === elKey))
      const parent = previousElements.reverse().find(y => y.indent < el.indent)
      return parent
    },

    /** 指定された要素の子を取得する。 */
    getChildren: (el: TFlatItem): TFlatItem[] => {
      // 引数のエレメントより後ろの位置にあり、
      // インデントが引数のエレメントより大きいもののうち、
      // そのエレメントと引数のエレメントの間にインデントが挟まるものがないものが子。
      // 例えば以下の場合、b, d, f, gはaの子。cはbの子。eはdの子。
      // - a
      //       - b
      //         - c
      //     - d
      //       - e
      //     - f
      //   - g

      const elKey = keySelector(el)
      const elIndex = flat.findIndex(x => keySelector(x) === elKey)
      // 要素が配列内に見つからないという状況は設計上起こりえないかもしれないが、念のためチェック
      if (elIndex === -1) {
        return []
      }

      const children: TFlatItem[] = []
      const stack: TFlatItem[] = []

      // el の次の要素から走査を開始
      for (let i = elIndex + 1; i < flat.length; i++) {
        const potentialChild = flat[i]

        // 注目している要素のインデントが走査開始時点のインデント以下になった場合、
        // それはもはや現在注目している親の子ではない（兄弟か、より上位の階層の要素）。
        // それ以降の要素も子ではないため、探索を終了する。
        if (potentialChild.indent <= el.indent) {
          break
        }

        if (stack.length === 0) {
          children.push(potentialChild)
          stack.push(potentialChild)
          continue
        }

        const peek = stack[stack.length - 1]

        // elとこの要素の間に挟まるインデントの要素がある場合、この要素はelの直下の子ではない
        if (peek.indent < potentialChild.indent) {
          continue
        }

        // elとこの要素の間に挟まるインデントの要素がなく、
        // この要素のインデントがelと同じ場合、この要素はelの直下の子。
        if (peek.indent >= potentialChild.indent) {
          children.push(potentialChild)
          stack.pop()
          stack.push(potentialChild)
          continue
        }
      }
      return children
    },

    /** 指定された要素の祖先を取得する。よりルート集約に近いほうが先。 */
    getAncestors: (el: TFlatItem): TFlatItem[] => {
      // 引数のエレメントより前の方向に辿っていき、
      // インデントが現在のエレメントより小さいものを集める。
      // よりルート集約に近いほうが先なので、最後に配列を逆転させてreturnする。
      const elKey = keySelector(el)
      const previousElements = flat.slice(0, flat.findIndex(x => keySelector(x) === elKey))
      let currentIndent = el.indent
      const ancestors: TFlatItem[] = []
      for (const y of previousElements) {
        if (y.indent < currentIndent) {
          ancestors.push(y)
          currentIndent = y.indent
        }
        // ルート集約（インデント0）に到達したら探索を打ち切る
        if (y.indent === 0) {
          break
        }
      }
      return ancestors.reverse()
    },

    /** 指定された要素の子孫を取得する。 */
    getDescendants: (el: TFlatItem): TFlatItem[] => {
      // getChildrenのロジックのうち「直下の子」という条件を外したものが子孫。
      const elKey = keySelector(el)
      const belowElements = flat.slice(flat.findIndex(x => keySelector(x) === elKey) + 1)
      const descendants: TFlatItem[] = []
      for (const y of belowElements) {
        // 引数のエレメント以下のインデントの要素が登場したら探索を打ち切る
        if (y.indent <= el.indent) {
          break
        } else {
          descendants.push(y)
        }
      }
      return descendants
    },
  }
}

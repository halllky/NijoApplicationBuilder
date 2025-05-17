/** アプリケーション全体の状態 */
export type ApplicationState = {
  /** アプリケーション名。XMLのルート要素のLocalName。読み取り専用。 */
  applicationName: string
  /** XML要素をルート集約ごとの塊に分類したもの。 */
  xmlElementTrees: ModelPageForm[]
  /** XML要素の属性定義。 */
  attributeDefs: XmlElementAttribute[]
  /** 値メンバーの種類定義。 */
  valueMemberTypes: ValueMemberType[]
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

// ---------------------------------

/** XML要素1個分と対応するデータ型 */
export type XmlElementItem = {
  /** XML要素とは関係ないUIの都合上のID。画面表示時に発番する。 */
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
  /** この属性が使用可能なモデルの種類の配列。 */
  availableModels: string[]
} & (XmlElementStringAttribute | XmlElementBoolAttribute | XmlElementSelectAttribute)

/** XML要素の属性の種類定義（文字列属性） */
export type XmlElementStringAttribute = {
  type: 'string'
}
/** XML要素の属性の種類定義（ブール属性） */
export type XmlElementBoolAttribute = {
  type: 'bool'
}
/** XML要素の属性の種類定義（選択属性）  */
export type XmlElementSelectAttribute = {
  type: 'select'
  /** 選択肢を取得する関数。 */
  getOptions: XmlElementSelectAttributeGetOptionFunction
}

export type XmlElementSelectAttributeGetOptionFunction = (state: ApplicationState) => XmlElementSelectAttributeOption[]
export type XmlElementSelectAttributeOption = { id: string, displayName: string }

// ---------------------------------

export const ATTR_TYPE = 'Type' as XmlElementAttributeName
export const ATTR_GENERATE_DEFAULT_QUERY_MODEL = 'GenerateDefaultQueryModel' as XmlElementAttributeName
export const ATTR_GENERATE_BATCH_UPDATE_COMMAND = 'GenerateBatchUpdateCommand' as XmlElementAttributeName

export const TYPE_DATA_MODEL = 'data-model'
export const TYPE_QUERY_MODEL = 'query-model'
export const TYPE_COMMAND_MODEL = 'command-model'
export const TYPE_STATIC_ENUM_MODEL = 'enum'
export const TYPE_VALUE_OBJECT_MODEL = 'value-object'
export const TYPE_CHILD = 'child'
export const TYPE_CHILDREN = 'children'

// ---------------------------------

/** 内部の状態はフラットなツリーとして保持されているが、それをツリー構造として扱うためのユーティリティ。 */
export const asTree = (flat: XmlElementItem[]) => {
  return {

    /** 指定された要素のルートを取得する。 */
    getRoot: (el: XmlElementItem): XmlElementItem => {
      // 引数のエレメントより前の位置にある、インデント0の要素のうち直近のものがルート
      const previousElements = flat.slice(0, flat.indexOf(el))
      const root = previousElements.reverse().find(x => x.indent === 0)
      if (!root) throw new Error('root not found') // 必ずルート集約はあるはず
      return root
    },

    /** 指定された要素の親を取得する。 */
    getParent: (el: XmlElementItem): XmlElementItem | undefined => {
      // 引数のエレメントより前の位置にあり、
      // インデントが引数のエレメントより小さいもののうち、直近にあるのが親。
      // インデントは必ずしも1小さいとは限らない。
      const previousElements = flat.slice(0, flat.indexOf(el))
      const parent = previousElements.reverse().find(y => y.indent < el.indent)
      return parent
    },

    /** 指定された要素の子を取得する。 */
    getChildren: (el: XmlElementItem): XmlElementItem[] => {
      // 引数のエレメントより後ろの位置にあり、
      // インデントが引数のエレメントより大きいもののうち、
      // そのエレメントと引数のエレメントの間にインデントが挟まるものがないものが子。
      // 例えば以下の場合、b, d, eはaの子。cはbの子。
      // - a
      //   - b
      //     - c
      //   - d
      //   - e
      const belowElements = flat.slice(flat.indexOf(el) + 1)
      const children: XmlElementItem[] = []
      let currentIndent = el.indent + 1 // このインデントと同じまたはより浅いならば子
      for (const y of belowElements) {
        // currentIndentと同じまたはより浅いならば子
        if (y.indent <= currentIndent) {
          children.push(y)
          currentIndent = y.indent
          continue
        }
        // 引数のエレメントより浅いインデントの要素が登場したら探索を打ち切る
        if (y.indent < el.indent) {
          break
        }
      }
      return children
    },

    /** 指定された要素の祖先を取得する。よりルート集約に近いほうが先。 */
    getAncestors: (el: XmlElementItem): XmlElementItem[] => {
      // 引数のエレメントより前の方向に辿っていき、
      // インデントが現在のエレメントより小さいものを集める。
      // よりルート集約に近いほうが先なので、最後に配列を逆転させてreturnする。
      const previousElements = flat.slice(0, flat.indexOf(el))
      let currentIndent = el.indent
      const ancestors: XmlElementItem[] = []
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
    getDescendants: (el: XmlElementItem): XmlElementItem[] => {
      // getChildrenのロジックのうち「直下の子」という条件を外したものが子孫。
      const belowElements = flat.slice(flat.indexOf(el) + 1)
      const descendants: XmlElementItem[] = []
      for (const y of belowElements) {
        // 引数のエレメントより浅いインデントの要素が登場したら探索を打ち切る
        if (y.indent < el.indent) {
          break
        } else {
          descendants.push(y)
        }
      }
      return descendants
    },
  }
}

/**
 * デバッグプロセスの状態。
 * このクラスのデータ構造はC#側と合わせる必要あり
 */
export type DebugProcessState = {
  /** サーバー側で発生した何らかのエラー */
  errorSummary?: string
  /** 現在実行中のNijoApplicationBuilderのNode.jsのデバッグプロセスと推測されるPID */
  estimatedPidOfNodeJs?: number
  /** 現在実行中のNijoApplicationBuilderのASP.NET Coreのデバッグプロセスと推測されるPID */
  estimatedPidOfAspNetCore?: number
  /** Node.jsのプロセス名 */
  nodeJsProcessName?: string
  /** ASP.NET Coreのプロセス名 */
  aspNetCoreProcessName?: string
  /** Node.jsのデバッグURL */
  nodeJsDebugUrl?: string
  /** ASP.NET CoreのデバッグURL（swagger-ui） */
  aspNetCoreDebugUrl?: string
  /** PID推測時のコンソール出力 */
  consoleOut?: string
};

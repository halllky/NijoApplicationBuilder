
/** スキーマ定義編集におけるアプリケーション全体の状態 */
export type SchemaDefinitionGlobalState = {
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

export type XmlElementSelectAttributeGetOptionFunction = (state: SchemaDefinitionGlobalState) => XmlElementSelectAttributeOption[]
export type XmlElementSelectAttributeOption = { id: string, displayName: string }

// ---------------------------------

export const ATTR_TYPE = 'Type' as XmlElementAttributeName
export const ATTR_GENERATE_DEFAULT_QUERY_MODEL = 'GenerateDefaultQueryModel' as XmlElementAttributeName
export const ATTR_GENERATE_BATCH_UPDATE_COMMAND = 'GenerateBatchUpdateCommand' as XmlElementAttributeName
export const ATTR_IS_KEY = 'IsKey' as XmlElementAttributeName

export const TYPE_DATA_MODEL = 'data-model'
export const TYPE_QUERY_MODEL = 'query-model'
export const TYPE_COMMAND_MODEL = 'command-model'
export const TYPE_STATIC_ENUM_MODEL = 'enum'
export const TYPE_VALUE_OBJECT_MODEL = 'value-object'
export const TYPE_CHILD = 'child'
export const TYPE_CHILDREN = 'children'

// ---------------------------------

export type TreeHelper<TFlatItem extends { indent: number }> = ReturnType<typeof asTree<TFlatItem>>

/** 内部の状態はフラットなツリーとして保持されているが、それをツリー構造として扱うためのユーティリティ。 */
export const asTree = <TFlatItem extends { indent: number }>(flat: TFlatItem[]) => {
  return {

    /** 指定された要素のルートを取得する。 */
    getRoot: (el: TFlatItem): TFlatItem => {
      // 引数のエレメント以前の位置にある、インデント0の要素のうち直近のものがルート
      const previousElementsAndThis = flat.slice(0, flat.indexOf(el) + 1)
      const root = previousElementsAndThis.reverse().find(x => x.indent === 0)
      if (!root) throw new Error('root not found') // 必ずルート集約はあるはず
      return root
    },

    /** 指定された要素の親を取得する。 */
    getParent: (el: TFlatItem): TFlatItem | undefined => {
      // 引数のエレメントより前の位置にあり、
      // インデントが引数のエレメントより小さいもののうち、直近にあるのが親。
      // インデントは必ずしも1小さいとは限らない。
      const previousElements = flat.slice(0, flat.indexOf(el))
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

      const elIndex = flat.indexOf(el);
      // 要素が配列内に見つからないという状況は設計上起こりえないかもしれないが、念のためチェック
      if (elIndex === -1) {
        return [];
      }

      const children: TFlatItem[] = []
      const stack: TFlatItem[] = []

      // el の次の要素から走査を開始
      for (let i = elIndex + 1; i < flat.length; i++) {
        const potentialChild = flat[i];

        // 注目している要素のインデントが走査開始時点のインデント以下になった場合、
        // それはもはや現在注目している親の子ではない（兄弟か、より上位の階層の要素）。
        // それ以降の要素も子ではないため、探索を終了する。
        if (potentialChild.indent <= el.indent) {
          break;
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
      const previousElements = flat.slice(0, flat.indexOf(el))
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
      const belowElements = flat.slice(flat.indexOf(el) + 1)
      const descendants: TFlatItem[] = []
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

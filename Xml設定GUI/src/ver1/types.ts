
/** アプリケーション全体で保持されるデータ */
export type ApplicationData = {
  属性種類定義: 属性種類定義.Whole
}

/** 注釈 */
export type Annotation = {
  uniqueId: string | undefined
  text: string | undefined
}

export namespace 属性種類定義 {

  /** 「属性種類定義」全体で保持されるデータの形 */
  export type Whole = {
    全般?: string
    文字系属性: 文字系属性[]
    数値系属性: 数値系属性[]
    時間系属性: 時間系属性[]
    区分系属性: 区分系属性[]
    その他の属性: その他の属性[]
    文字系属性注釈: Annotation[]
    数値系属性注釈: Annotation[]
    時間系属性注釈: Annotation[]
    区分系属性注釈: Annotation[]
    その他の属性注釈: Annotation[]
  }

  /** 仕様がこの文字列の場合のみ特別扱い */
  export const BY_MODEL = 'モデル毎に定義'

  /** 仕様 */
  export type Specification = typeof BY_MODEL | string

  /** 型 */
  export type TypeSpecification = {
    DB?: Specification
    "C#"?: Specification
    TypeScript?: Specification
  }

  /** 検索処理の挙動の仕様 */
  export type SearchingSpecification = {
    検索処理の挙動?: Specification
    全文検索の対象か否か?: Specification
  }

  /** ノーマライズの仕様 */
  export type NormalizeSpecification = {
    UIフォーカスアウト時?: Specification
    登録時?: Specification
  }

  export type 文字系属性 = {
    uniqueId?: string
    型名?: string
    説明?: string
    型: TypeSpecification
    制約: {
      MaxLength?: Specification
      文字種?: Specification
    }
    ノーマライズ: NormalizeSpecification
    検索処理定義: SearchingSpecification
  }

  export type 数値系属性 = {
    uniqueId?: string
    型名?: string
    説明?: string
    型: TypeSpecification
    制約: {
      トータル桁数?: Specification
      小数部桁数?: Specification
    }
    表示形式: {
      "3桁毎カンマ有無"?: Specification
      "prefix/suffix"?: Specification
      負の数の表現?: Specification
    }
    検索処理定義: SearchingSpecification
    ノーマライズ: NormalizeSpecification
  }

  export type 時間系属性 = {
    uniqueId?: string
    型名?: string
    説明?: string
    型: TypeSpecification
    制約: {
      値の範囲?: Specification
    }
    検索処理定義: SearchingSpecification
    ノーマライズ: NormalizeSpecification
  }

  export type 区分系属性 = {
    uniqueId?: string
    型名?: string
    説明?: string
    型: TypeSpecification
    区分の種類?: Specification
    検索処理定義: SearchingSpecification
  }

  export type その他の属性 = {
    uniqueId?: string
    型名?: string
    説明?: string
    型: TypeSpecification
    制約?: Specification
    検索処理定義: SearchingSpecification
  }
}

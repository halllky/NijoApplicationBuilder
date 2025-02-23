import { OutlinerItem } from "./ui-components"

/** アプリケーション全体で保持されるデータ */
export type ApplicationData = {
  システム名?: string | undefined
  属性種類定義: 属性種類定義.All
  DataModel定義: DataModel定義.DataModel[]
  QueryModel定義: QueryModel定義.QueryModel[]
  CommandModel定義: CommandModel定義.CommandModel[]
}

export namespace 属性種類定義 {

  /** 「属性種類定義」全体で保持されるデータの形 */
  export type All = {
    全般?: string
    文字系属性: 文字系属性[]
    数値系属性: 数値系属性[]
    時間系属性: 時間系属性[]
    区分系属性: 区分系属性[]
    その他の属性: その他の属性[]
    文字系属性注釈: OutlinerItem[]
    数値系属性注釈: OutlinerItem[]
    時間系属性注釈: OutlinerItem[]
    区分系属性注釈: OutlinerItem[]
    その他の属性注釈: OutlinerItem[]
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
    uniqueId: string | undefined
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
    uniqueId: string | undefined
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
    uniqueId: string | undefined
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
    uniqueId: string | undefined
    型名?: string
    説明?: string
    型: TypeSpecification
    区分の種類?: Specification
    検索処理定義: SearchingSpecification
  }

  export type その他の属性 = {
    uniqueId: string | undefined
    型名?: string
    説明?: string
    型: TypeSpecification
    制約?: Specification
    検索処理定義: SearchingSpecification
  }
}

export namespace DataModel定義 {
  export type DataModel = {
    uniqueId: string
    表示名?: string
    物理名?: string
    DB名?: string
    ラテン名?: string
    項目定義: 項目定義[]
    キー以外のインデックス: インデックス定義[]
    ライフサイクル定義?: string
    ライフサイクル定義注釈: OutlinerItem[]
  }

  export type 項目定義 = {
    uniqueId: string
    表示名?: string
    属性種類UniqueId?: string
    物理名?: string
    DB名?: string
    /** 主キーの順番。キーでない場合はundefined */
    キー?: number
    必須?: boolean
    属性種類ごとの定義: {
      [制約名: string]: string
    }
  }

  export type インデックス定義 = {
    uniqueId: string
    対象項目UniqueId: string[]
    ユニーク?: boolean
  }

  export type その他制約定義 = {
    uniqueId: string
    制約?: string
    理由?: string
    DataModelを跨ぐ制約か?: boolean
    /** DataModelを跨ぐ場合のみ定義 */
    この制約が崩れた場合の対処?: string
  }
}

export namespace QueryModel定義 {

  export type QueryModel = {
    uniqueId: string
    表示名?: string
    物理名?: string
    ラテン名?: string
    読み取り専用集約?: boolean
    パフォーマンス要件定義?: string
    既定のソート順?: string
    項目定義: 項目定義[]
  }

  export type 項目定義 = {
    uniqueId: string
    表示名?: string
    物理名?: string
    属性種類UniqueId?: string
    独立ライフサイクル?: boolean
    検索条件にのみ存在?: boolean
    RefTo強制レンダリング?: boolean
    データソース定義: {
      dataModelUniqueId?: string
      dataModel項目UniqueId?: string
      補足?: string
    }
    属性項目定義に依らない特記事項: {
      絞り込みの仕様?: string
      ソートの仕様?: string
    }
    属性種類ごとの定義: {
      [制約名: string]: string
    }
  }
}

export namespace CommandModel定義 {

  export type CommandModel = {
    uniqueId: string
    表示名?: string
    物理名?: string
    ラテン名?: string
    処理概要?: string
    トリガー?: string
    パラメータ: 項目定義[]
    戻り値: 項目定義[]
    処理詳細: OutlinerItem[]
  }

  export type 項目定義 = {
    uniqueId: string
    表示名?: string
    物理名?: string
    属性種類UniqueId?: string
    属性種類ごとの定義: {
      [制約名: string]: string
    }
  }

}

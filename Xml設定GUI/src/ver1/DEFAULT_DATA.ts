import { UUID } from "uuidjs"
import { ApplicationData, 属性種類定義 } from "./types"

/**
 * 個々のプロジェクトで特に異議がなければそのシステムはこの仕様で構築される。
 */
export const getDefaultSpecification = (): ApplicationData => ({
  属性種類定義: {
    全般: `- DataModel, CommandModel, QueryModel に登場する項目の属性の種類は、以下で定義した属性の中から選択されなければならない。
- もし「 **システム日付** より未来でなければならない」のように、仕様中に定数でないもの（変数）が登場した場合、それは属性項目定義ではなくCommandModelやDataModelの仕様なので、ここに書かずそちらで表現する`,
    文字系属性: [{
      uniqueId: UUID.generate(),
      型名: '単語',
      説明: '改行を含まない文章。',
      型: {
        DB: 'NVARCHAR(桁)',
        "C#": 'string',
        TypeScript: 'string',
      },
      制約: {
        MaxLength: 属性種類定義.BY_MODEL,
        文字種: 属性種類定義.BY_MODEL,
      },
      検索処理定義: {
        検索処理の挙動: 属性種類定義.BY_MODEL,
        全文検索の対象か否か: 属性種類定義.BY_MODEL,
      },
      ノーマライズ: {
        UIフォーカスアウト時: '前後の空白はTrim。改行は除去。',
        登録時: '前後の空白はTrim。改行は除去。',
      },
    }],
    文字系属性注釈: [{
      uniqueId: UUID.generate(),
      indent: 0,
      bullet: '※1',
      text: `UUIDは基本的に画面上に表示されないため検索条件となることはないが、なる場合は完全一致検索とする。`,
    }, {
      uniqueId: UUID.generate(),
      indent: 0,
      bullet: '※2',
      text: `文字列型属性の文字種定義\n- 指定なし: 下記「指定なし」の定義の通り\n- 半角英数のみ: 0-9, a-z, A-Z の計62文字のみ使用可能`,
    }, {
      uniqueId: UUID.generate(),
      indent: 0,
      bullet: '※3',
      text: `文字種「指定なし」の定義:
基本方針としては、各国語の一般的な文字と一般的な記号を許可し、特殊用途の文字や制御文字を除外する。
- 許可する文字範囲
  - 基本的な文字セット
    - Basic Latin (ASCII): U+0020 ～ U+007E
    - Latin-1 Supplement: U+00A1 ～ U+00FF
    - ひらがな: U+3040 ～ U+309F
    - カタカナ: U+30A0 ～ U+30FF
    - CJK統合漢字（第1水準・第2水準）: U+4E00 ～ U+9FFF
  - 追加の記号類
    - 全角記号・数字: U+FF00 ～ U+FF5E
    - CJK記号と句読点: U+3000 ～ U+303F
- 除外する文字範囲
  - 制御文字
    - C0制御文字: U+0000 ～ U+001F
    - C1制御文字: U+0080 ～ U+009F
    - DEL文字: U+007F
  - 特殊用途の文字
    - Private Use Area: U+E000 ～ U+F8FF
    - 異体字セレクタ: U+FE00 ～ U+FE0F
    - Variation Selectors: U+FE00 ～ U+FE0F
    - 絵文字: U+1F300 ～ U+1F9FF
- 注意事項
  - サロゲートペアは非許可
  - 結合文字（ダイアクリティカルマーク等）は非許可
  - ゼロ幅文字は非許可
- 実装者向け情報
  - 文字列の正規化はNFKCを使用
  - 入力値の検証時は、上記範囲外の文字を含む場合はエラーとする
  - データベースのCollationはutf8mb4_general_ciを推奨`,
    }],
    数値系属性: [],
    数値系属性注釈: [],
    時間系属性: [],
    時間系属性注釈: [],
    区分系属性: [],
    区分系属性注釈: [],
    その他の属性: [],
    その他の属性注釈: [],
  },
})

/**
 * レスポンシブ対応されたフォームレイアウト。
 * `ResponsiveForm` という名前としたいが、ソースコード中に出現する回数が多いためこの名前としている。
 *
 * 基本的な使い方
 * - もっとも外側に `Root` を配置する。
 * - `Root` の中に `BreakPoint` を配置する。
 *   - 画面サイズが変更されたとき、 `BreakPoint` の単位で折り返しが発生する。
 *   - `BreakPoint` の中では `Item` は縦方向に配置される。
 * - `BreakPoint` の直下に `Item` を配置する。
 *   - `Item` のプロパティにその項目のラベルを記載する。（stringでもよいが、凝ったラベルの場合はReactNodeでも可）
 *   - `Item` の子要素にはその項目の入力フォーム（テキストボックスやチェックボックスなど）を記載する。
 */
export namespace VForm3 {

  /**
   * レスポンシブ対応されたフォームレイアウトのコンテナ。
   * 基本的な使い方は `VForm3` のコメントを参照。
   */
  export const Root = (props?: {
    children?: React.ReactNode
    /** このフォーム内に配置される `Item` のラベルの幅。CSSのwidthとして有効な値（ '10rem' など）を指定する。 */
    labelWidth?: string
    className?: string
  }) => {
    return (
      <div>

      </div>
    )
  }

  /**
   * レスポンシブの折り返しが発生する粒度。
   * 基本的な使い方は `VForm3` のコメントを参照。
   */
  export const BreakPoint = (props?: {
    children?: React.ReactNode
    /** trueにするとこのブレークポイントの外側に境界線が表示される。 */
    showBorder?: boolean
    /** このブレークポイントのラベル。 */
    label?: React.ReactNode
    className?: string
  }) => {
    return (
      <div>

      </div>
    )
  }

  /**
   * フォームの1項目。ラベルと値のペア。
   * 基本的な使い方は `VForm3` のコメントを参照。
   */
  export const Item = (props?: {
    /** ラベル */
    label?: React.ReactNode
    /** trueにするとこの項目が必須であることを示すマークが表示される。 */
    required?: boolean
    children?: React.ReactNode
  }) => {
    return (
      <div>

      </div>
    )
  }
}
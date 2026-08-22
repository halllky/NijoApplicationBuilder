import type { LogIncrement } from "../shared/devtool-api.ts"

/**
 * ANSIエスケープシーケンス（色・カーソル移動等の制御文字）にマッチする正規表現。
 * CSI（ESC [ ... 終端文字）と OSC（ESC ] ... BEL または ESC \）の両方を対象とする。
 * チャンク単位で届く出力を都度この正規表現にかけるため、チャンクの境目でシーケンスが
 * 分断された場合は除去しきれないことがある（実害は数バイトの制御文字が残る程度）。
 */
const ANSI_ESCAPE_PATTERN = /\x1B(?:\[[0-9;?]*[a-zA-Z]|\][^\x07\x1B]*(?:\x07|\x1B\\))/g

/**
 * 1ストリーム分のテキストを保持するリングバッファ。ログはファイルに書かず、この中にのみ保持する。
 * デバッグ実行プロセスの標準出力・標準エラー（{@link Preview}）と、DevToolサーバー自身のログ
 * （{@link ServerLog}）の両方がこれを使う。用途によって保持上限が異なるため、上限は
 * コンストラクタで指定する。
 *
 * 返す offset は「これまでに受け取った文字数の累計」であり、
 * バッファの先頭を捨てても増加し続ける（呼び出し側が持つ既読位置と整合させるため）。
 */
export class LogBuffer {
  readonly #maxLength: number
  #text = ""
  #totalLength = 0
  #droppedLength = 0

  constructor(maxLength: number) {
    this.#maxLength = maxLength
  }

  append(rawChunk: string): void {
    // ツールが NO_COLOR を尊重せず色制御文字を出力してくる場合の保険として、蓄積前に取り除く
    const chunk = rawChunk.replace(ANSI_ESCAPE_PATTERN, "")
    this.#text += chunk
    this.#totalLength += chunk.length
    if (this.#text.length > this.#maxLength) {
      const dropped = this.#text.length - this.#maxLength
      this.#text = this.#text.slice(dropped)
      this.#droppedLength += dropped
    }
  }

  /** 起動しなおしでログをクリアする。以降 offset は 0 から数えなおす */
  clear(): void {
    this.#text = ""
    this.#totalLength = 0
    this.#droppedLength = 0
  }

  /**
   * 指定オフセット以降の増分を返す。
   * オフセットが累計より大きい場合（クリアされた場合）は先頭から読み直す。
   */
  read(fromOffset: number): LogIncrement {
    const offset = fromOffset > this.#totalLength ? 0 : fromOffset
    const start = Math.max(offset, this.#droppedLength) - this.#droppedLength
    return { text: this.#text.slice(start), offset: this.#totalLength }
  }
}

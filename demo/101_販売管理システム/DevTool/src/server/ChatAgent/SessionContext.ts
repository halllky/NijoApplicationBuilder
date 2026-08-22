
/**
 * プロンプトに載せる可能性がある背景情報を全部詰めたもの。
 */
export type SessionContext = {
  /** 現在時刻 */
  currentTimeUtc: string
  /** 編集対象のアプリケーションの名前。nijo.xmlから採る。 */
  applicationName: string
}

/**
 * {@link SessionContext} を構築して返す。
 *
 * @param demo101Root 編集対象プロジェクトのルートディレクトリ
 */
export function buildSessionContext(demo101Root: string): SessionContext {
  throw new Error('not implemented.')
}

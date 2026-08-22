import { createAnthropic } from "@ai-sdk/anthropic"
import { convertToModelMessages, stepCountIs, streamText, tool, type UIMessage } from "ai"
import { z } from "zod"
import { ProjectFiles } from "../ProjectFiles.ts"
import { CurrentState } from "./CurrentState.ts"
import { buildSessionContext, type SessionContext } from "./SessionContext.ts"

/**
 * 1回のユーザー発言に対してツール呼び出しを重ねてよい最大ステップ数（無限ループの保険）。
 * 上限に達した最後のステップは #tools を使わせず必ず文章で回答させる（{@link ChatAgent.respond} の prepareStep 参照）ため、
 * 複数ファイルの横断調査のようなステップ数がかさむタスクでも、無回答のまま打ち切られることはない。
 */
const MAX_STEPS = 24

/** ステップ上限に達したときにシステムプロンプトへ追記し、ツールを使わずここまでの情報で回答させるための指示文。 */
const FINAL_STEP_NOTICE = `
このやり取りで使えるツール呼び出し回数の上限に達しました。
これ以上ツールは呼び出せません。ここまでに調べた情報だけを踏まえて、ユーザーへの回答を必ず文章で返してください。
情報が不足していて確定的な回答ができない場合は、これまでに分かったことを伝え、調査続行するかを問うてください。
`.trim()

/** {@link ChatAgent} が使うシステムプロンプトを組み立てる。SessionContext の内容を背景情報として差し込む。 */
function buildSystemPrompt(context: SessionContext): string {
  return `
あなたはこのシステムの構築を補助する者です。
ユーザー入力に応じて以下いずれかのタスクを遂行します。

- このシステムの現在の状態についての情報を答える
- ユーザーの中でも明確化されていない情報（このシステムの目的、スコープ、仕様）を
  明確化するための意思決定の補助をする
- このシステムへの具体的な機能追加や不具合修正を行う
- ユーザーの意図が上記いずれに該当するか明確でない場合に、明確化するための問いかけをする

# 対象システム
名前: ${context.applicationName}
現在時刻(UTC): ${context.currentTimeUtc}

# ツールについて
プロジェクトの中身に関する質問には、憶測で答えず list_files / read_file ツールで実際のファイルを確認してから答えること。
`.trim()
}

/** 要件ヒアリングを行うチャットエージェント */
export class ChatAgent {
  readonly #demo101Root: string
  readonly #projectFiles: ProjectFiles
  readonly #currentState: CurrentState

  /**
   * @param demo101Root 編集対象プロジェクト（デモ101アプリ）のルートディレクトリ。ツールが読めるファイルの範囲はここに限定される。
   * @param currentState 会話履歴の永続化。読み込み・仕切り直しは呼び出し側（HTTPエンドポイント）が直接扱うため、ここでは保存のみに使う。
   */
  constructor(demo101Root: string, currentState: CurrentState) {
    this.#demo101Root = demo101Root
    this.#projectFiles = new ProjectFiles(demo101Root)
    this.#currentState = currentState
  }

  /**
   * 新しいユーザー発言を会話に加えて応答をストリーミングで返す。
   * 会話履歴の読み込み・保存はこのメソッドが担う（呼び出し側は最新のユーザー発言だけを渡せばよい）。
   * apiKey は呼び出しごとに渡された値でプロバイダーを生成する（環境変数は参照しない）。
   */
  async respond(newUserMessage: UIMessage | undefined, options: { apiKey: string, model: string }): Promise<Response> {
    const currentState = await this.#currentState.load()
    if (newUserMessage) currentState.currentSession.push(newUserMessage)

    const sessionContext = await buildSessionContext(this.#demo101Root)
    const anthropic = createAnthropic({ apiKey: options.apiKey })

    const result = streamText({
      model: anthropic(options.model),
      system: buildSystemPrompt(sessionContext),
      messages: await convertToModelMessages(currentState.currentSession),
      tools: this.#tools(),
      stopWhen: stepCountIs(MAX_STEPS),
      // ステップ上限に達する最後のステップではツールを使わせず、必ず文章で回答させる。
      // これが無いと、上限到達時にツール呼び出し直後で応答が打ち切られ、ユーザーには「何も返ってこない」ように見えてしまう。
      prepareStep: ({ stepNumber }) => {
        if (stepNumber !== MAX_STEPS - 1) return undefined
        return {
          toolChoice: "none",
          instructions: `${buildSystemPrompt(sessionContext)}\n\n${FINAL_STEP_NOTICE}`,
        }
      },
    })

    return result.toUIMessageStreamResponse({
      originalMessages: currentState.currentSession,
      onFinish: async ({ messages }) => {
        currentState.currentSession = messages
        await this.#currentState.save(currentState)
      },
    })
  }

  /** このエージェントが使えるツール一覧。現時点ではプロジェクトファイルの読み取りのみ（書き込みは行わない）。 */
  #tools() {
    return {
      list_files: tool({
        description: "プロジェクト内の指定ディレクトリ直下のファイル・サブディレクトリ一覧を返す。ディレクトリはサブディレクトリ末尾に '/' が付く。",
        inputSchema: z.object({
          path: z.string().describe("一覧したいディレクトリのプロジェクトルートからの相対パス。ルート自体を見る場合は '.' を指定する。"),
        }),
        execute: async ({ path }) => {
          const entries = await this.#projectFiles.list(path)
          return entries ?? { error: `ディレクトリが見つかりません: ${path}` }
        },
      }),
      read_file: tool({
        description: "プロジェクト内の指定ファイルの内容をテキストとして返す。",
        inputSchema: z.object({
          path: z.string().describe("読みたいファイルのプロジェクトルートからの相対パス。"),
        }),
        execute: async ({ path }) => {
          const content = await this.#projectFiles.read(path)
          return content ?? { error: `ファイルが見つかりません: ${path}` }
        },
      }),
    }
  }
}

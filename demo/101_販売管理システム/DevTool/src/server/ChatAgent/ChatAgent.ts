import { createOpenRouter } from "@openrouter/ai-sdk-provider"
import { convertToModelMessages, createUIMessageStreamResponse, stepCountIs, streamText, tool, toUIMessageStream, type UIMessage } from "ai"
import { z } from "zod"
import { ProjectFiles } from "../ProjectFiles.ts"
import { CurrentState } from "./CurrentState.ts"
import { readOnlyFileTools } from "./ProjectFileTools.ts"
import { CODE_RESEARCH_DOMAIN, ResearchAgent, SCHEMA_RESEARCH_DOMAIN, SCREEN_RESEARCH_DOMAIN } from "./ResearchAgent.ts"
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
- 上記のタスクを遂行するに十分な情報が集まるまで情報収集を行う

# 対象システム
名前: ${context.applicationName}
現在時刻(UTC): ${context.currentTimeUtc}

# プロジェクトのフォルダ構成
${context.projectStructureOverview}

# ルール
- マークダウン記法を使わず自然な文章で回答すること。
- ユーザーの意図が不明瞭な場合は積極的にユーザーに質問すること。
  このターンで回答を確定させることよりも明確なユーザーの意図に基づくことを優先する。
- プロジェクトの中身に関する調査で広く探す必要がある場合は research_schema / research_code / research_screen に委譲すること。
  読むべきファイルが既に特定できている場合のみ list_files / read_file を直接使ってよい。
- 利用者はプログラミングの素養がなく、画面を見ながら話しかけてくる。発話中の画面名・項目名・ボタン名の実装上の在り処が不明な場合は、
  まず research_screen で実装上の名前に翻訳してから他の調査に進むこと。
- research_* の回答に含まれる「分からなかったこと」は、推測で埋めず、ユーザーへの問いかけに変換すること。
`.trim()
}

/** 要件ヒアリングを行うチャットエージェント */
export class ChatAgent {
  readonly #demo101Root: string
  readonly #projectFiles: ProjectFiles
  readonly #currentState: CurrentState
  /** 知識領域ごとの調査役。ChatAgent 自身は問いを投げるだけで、実際の探索はここに委ねる。 */
  readonly #researchAgents: readonly ResearchAgent[]

  /**
   * @param demo101Root 編集対象プロジェクト（デモ101アプリ）のルートディレクトリ。ツールが読めるファイルの範囲はここに限定される。
   * @param currentState 会話履歴の永続化。読み込み・仕切り直しは呼び出し側（HTTPエンドポイント）が直接扱うため、ここでは保存のみに使う。
   */
  constructor(demo101Root: string, currentState: CurrentState) {
    this.#demo101Root = demo101Root
    this.#projectFiles = new ProjectFiles(demo101Root)
    this.#currentState = currentState
    this.#researchAgents = [
      new ResearchAgent(this.#projectFiles, SCHEMA_RESEARCH_DOMAIN),
      new ResearchAgent(this.#projectFiles, CODE_RESEARCH_DOMAIN),
      new ResearchAgent(this.#projectFiles, SCREEN_RESEARCH_DOMAIN),
    ]
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
    // OpenRouter API を直接叩くので strict モードを指定する（互換モードでは streamOptions 等が送られない）。
    const openrouter = createOpenRouter({ apiKey: options.apiKey, compatibility: "strict" })

    const result = streamText({
      model: openrouter.chat(options.model),
      system: buildSystemPrompt(sessionContext),
      messages: await convertToModelMessages(currentState.currentSession),
      tools: this.#tools(sessionContext, options),
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

    return createUIMessageStreamResponse({
      stream: toUIMessageStream({
        stream: result.stream,
        originalMessages: currentState.currentSession,
        onFinish: async ({ messages }) => {
          currentState.currentSession = messages
          await this.#currentState.save(currentState)
        },
      }),
    })
  }

  /**
   * このエージェントが使えるツール一覧。
   * list_files / read_file は読むファイルが特定できている場合の直接アクセス用。
   * 広く探す調査は research_schema / research_code / research_screen（各 {@link ResearchAgent}）に委譲する。
   * 書き込みはこのエージェントの責務ではない。
   */
  #tools(sessionContext: SessionContext, options: { apiKey: string, model: string }) {
    const researchTools = Object.fromEntries(this.#researchAgents.map(agent => [
      agent.toolName,
      tool({
        description: agent.description,
        inputSchema: z.object({
          question: z.string().describe("調査してほしい内容。具体的な問いの形で渡すこと。"),
        }),
        execute: async ({ question }) => await agent.research(question, sessionContext, options),
      }),
    ]))

    return {
      ...readOnlyFileTools(this.#projectFiles),
      ...researchTools,
    }
  }
}

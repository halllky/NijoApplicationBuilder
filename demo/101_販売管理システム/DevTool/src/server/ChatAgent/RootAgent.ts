import { createOpenRouter } from "@openrouter/ai-sdk-provider"
import { convertToModelMessages, createUIMessageStreamResponse, stepCountIs, streamText, tool, toUIMessageStream, type UIMessage } from "ai"
import { z } from "zod"
import { ProjectFiles } from "../ProjectFiles.ts"
import type { AgentCallOptions } from "./ChatTurn.ts"
import { ChatSession } from "./ChatSession.ts"
import { CODE_RESEARCH_DOMAIN, ResearchAgent, SCHEMA_RESEARCH_DOMAIN, SCREEN_RESEARCH_DOMAIN } from "./ResearchAgent.ts"
import { buildSessionContext, type SessionContext } from "./SessionContext.ts"

/**
 * UIMessage からテキストパートだけを取り出して結合する（ツール呼び出し等の他パートは対象外）。
 * 会話ログに全文を残す際、ユーザー発言（index.ts）・AIの最終回答（{@link RootAgent.respond}）の
 * 両方から使う。
 */
export function extractMessageText(message: UIMessage | undefined): string {
  if (!message) return ""
  return message.parts
    .filter((part): part is Extract<UIMessage["parts"][number], { type: "text" }> => part.type === "text")
    .map(part => part.text)
    .join("\n")
}

/**
 * 1回のユーザー発言に対してツール呼び出しを重ねてよい最大ステップ数（無限ループの保険）。
 * 上限に達した最後のステップは #tools を使わせず必ず文章で回答させる（{@link RootAgent.respond} の prepareStep 参照）ため、
 * 複数ファイルの横断調査のようなステップ数がかさむタスクでも、無回答のまま打ち切られることはない。
 */
const MAX_STEPS = 24

/** ステップ上限に達したときにシステムプロンプトへ追記し、ツールを使わずここまでの情報で回答させるための指示文。 */
const FINAL_STEP_NOTICE = `
このやり取りで使えるツール呼び出し回数の上限に達しました。
これ以上ツールは呼び出せません。ここまでに調べた情報だけを踏まえて、ユーザーへの回答を必ず文章で返してください。
情報が不足していて確定的な回答ができない場合は、これまでに分かったことを伝え、調査続行するかを問うてください。
`.trim()

/**
 * 回答の書き方。役割の指示（{@link buildSystemPrompt}）とは別の system メッセージとして最後に渡す。
 * 利用者に見える画面（ChatPane）はマークダウンをレンダリングせず素のテキストとして表示するため、記号を書かせてはいけない。
 * 禁止だけを書くと守られないので、代わりに何を書くかと良い例／悪い例まで具体的に示す。
 * 最後の1文は、read_file や research_* が返すマークダウン混じりのテキストをモデルが手本として模倣してしまうのを打ち消すためのもの。
 */
const OUTPUT_STYLE = `
# 回答の書き方
回答は装飾を解釈しないプレーンテキストとして、そのまま利用者の画面に表示される。
記号を書くと記号のまま見えてしまうため、次の記号は使わないこと。
  # （見出し）、* や - （強調・箇条書き）、\` （コード）、| （表）、[]() （リンク）
列挙したくなったら「1つめは〜。2つめは〜。」のように文の中で言い分けること。
ファイル名や項目名はバッククォートや引用符で囲わず、そのまま文中に書くこと。
悪い例: 「- **受注登録画面** の \`OrderId\` を確認しました」
良い例: 「受注登録画面の OrderId という項目を確認しました。」
調査ツールの回答や読んだファイルにマークダウンが含まれていても、それを真似せず上記の書き方で答えること。
`.trim()

/** {@link RootAgent} が使うシステムプロンプトを組み立てる。SessionContext の内容を背景情報として差し込む。 */
function buildSystemPrompt(context: SessionContext): string {
  return `
あなたはこのシステムの構築を補助する者です。
ユーザー入力に応じて以下のようなタスクを行い、
その結果をマークダウンを使わない自然な文章で回答します。

- このシステムの現在の状態についての情報を答える
- ユーザーの中でも明確化されていない情報（このシステムの目的、スコープ、仕様）を
  明確化するための意思決定の補助をする
- このシステムへの具体的な機能追加や不具合修正を行う
- 上記のタスクを遂行するに十分な情報が集まるまで情報収集を行う

## 対象システム
名前: ${context.applicationName}
現在時刻(UTC): ${context.currentTimeUtc}

## ルール
- ユーザーの意図が不明瞭な場合は積極的にユーザーに質問すること。
  このターンで回答を確定させることよりも明確なユーザーの意図に基づくことを優先する。
- プロジェクトの中身に関する調査で広く探す必要がある場合は research_schema / research_code / research_screen に委譲すること。
  読むべきファイルが既に特定できている場合のみ list_files / read_file を直接使ってよい。
- 利用者はプログラミングの素養がなく、画面を見ながら話しかけてくる。発話中の画面名・項目名・ボタン名の実装上の在り処が不明な場合は、
  まず research_screen で実装上の名前に翻訳してから他の調査に進むこと。
- research_* の回答に含まれる「分からなかったこと」は、推測で埋めず、ユーザーへの問いかけに変換すること。
`.trim()
}

/** ユーザーと直に対話する主エージェント */
export class RootAgent {
  readonly #demo101Root: string
  readonly #projectFiles: ProjectFiles
  readonly #chatSession: ChatSession
  /** 知識領域ごとの調査役。主エージェント自身は問いを投げるだけで、実際の探索はここに委ねる。 */
  readonly #researchAgents: readonly ResearchAgent[]

  /**
   * @param demo101Root 編集対象プロジェクト（デモ101アプリ）のルートディレクトリ。ツールが読めるファイルの範囲はここに限定される。
   * @param chatSession 会話の永続化。応答対象のセッションの読み込みと保存に使う。
   */
  constructor(demo101Root: string, chatSession: ChatSession) {
    this.#demo101Root = demo101Root
    this.#projectFiles = new ProjectFiles(demo101Root)
    this.#chatSession = chatSession
    this.#researchAgents = [
      new ResearchAgent(this.#projectFiles, SCHEMA_RESEARCH_DOMAIN),
      new ResearchAgent(this.#projectFiles, CODE_RESEARCH_DOMAIN),
      new ResearchAgent(this.#projectFiles, SCREEN_RESEARCH_DOMAIN),
    ]
  }

  /**
   * 指定セッションに新しいユーザー発言を加えて応答をストリーミングで返す。
   * 会話の読み込み・保存はこのメソッドが担う（呼び出し側は最新のユーザー発言だけを渡せばよい）。
   * セッションが存在しない場合は 404 の Response を返す。
   * apiKey は呼び出しごとに渡された値でプロバイダーを生成する（環境変数は参照しない）。
   * options.turn は呼び出し元（index.ts）が生成し、このターン中に呼ぶサブエージェントにもそのまま渡される
   * （主エージェント・サブエージェントのLLM呼び出し・ツール呼び出しを1つの計器にまとめるため）。
   */
  async respond(sessionId: string, newUserMessage: UIMessage | undefined, options: AgentCallOptions): Promise<Response> {
    const session = await this.#chatSession.read(sessionId)
    if (!session) return Response.json({ error: "指定されたチャットセッションが見つかりません。" }, { status: 404 })
    if (newUserMessage) session.messages.push(newUserMessage)

    const sessionContext = await buildSessionContext(this.#demo101Root)
    // OpenRouter API を直接叩くので strict モードを指定する（互換モードでは streamOptions 等が送られない）。
    const openrouter = createOpenRouter({ apiKey: options.apiKey, compatibility: "strict" })

    const result = streamText({
      model: openrouter.chat(options.model),
      // 役割の指示と回答の書き方は別の system メッセージに分けて渡す。
      // 書き方の指示を役割の指示の箇条書きに混ぜると埋もれて効かないため、独立したブロックとして最後に置く。
      instructions: [
        { role: "system", content: buildSystemPrompt(sessionContext) },
        { role: "system", content: OUTPUT_STYLE },
      ],
      messages: await convertToModelMessages(session.messages),
      tools: this.#tools(sessionContext, options),
      stopWhen: stepCountIs(MAX_STEPS),
      // ステップ上限に達する最後のステップではツールを使わせず、必ず文章で回答させる。
      // これが無いと、上限到達時にツール呼び出し直後で応答が打ち切られ、ユーザーには「何も返ってこない」ように見えてしまう。
      prepareStep: ({ stepNumber }) => {
        if (stepNumber !== MAX_STEPS - 1) return undefined
        // instructions を上書きすると外側の指定は丸ごと差し替わるため、書き方の指示もここで渡し直す。
        return {
          toolChoice: "none",
          instructions: [
            { role: "system", content: buildSystemPrompt(sessionContext) },
            { role: "system", content: FINAL_STEP_NOTICE },
            { role: "system", content: OUTPUT_STYLE },
          ],
        }
      },
      // 消えたブラウザタブのために残りのステップを焼き続けないよう、接続断でループを打ち切る。
      abortSignal: options.abortSignal,
      telemetry: { functionId: "chat.root", integrations: [options.turn] },
    })

    return createUIMessageStreamResponse({
      stream: toUIMessageStream({
        stream: result.stream,
        originalMessages: session.messages,
        onEnd: async ({ messages, responseMessage, isAborted, finishReason }) => {
          // 変更計画を保持したままのセッションごと保存する
          session.messages = messages

          const summary = options.turn.summary()
          options.log.conversation("chat.assistant.message", { text: extractMessageText(responseMessage) })
          options.log.info("chat.turn.finish", { ...summary, isAborted, finishReason, steppedToCap: summary.steps === MAX_STEPS })

          try {
            await this.#chatSession.save(session)
          } catch (error) {
            options.log.error("session.save.failed", { error })
          }
        },
        onError: error => {
          options.log.error("chat.turn.error", { error: error instanceof Error ? error : String(error) })
          return "サーバー内部でエラーが発生しました。"
        },
      }),
    })
  }

  /**
   * このエージェントが使えるツール一覧
   */
  #tools(sessionContext: SessionContext, options: AgentCallOptions) {
    return {
      // ファイル読み書きツール
      ...this.#projectFiles.buildAiTools(),

      // サブエージェント
      ...Object.fromEntries(this.#researchAgents.map(agent => [
        agent.toolName,
        tool({
          description: agent.description,
          inputSchema: z.object({
            question: z.string().describe("調査してほしい内容。具体的な問いの形で渡すこと。"),
          }),
          execute: async ({ question }) => await agent.research(question, sessionContext, options),
        }),
      ])),
    }
  }
}

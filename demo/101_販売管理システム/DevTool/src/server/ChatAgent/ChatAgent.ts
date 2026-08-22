import { createAnthropic } from "@ai-sdk/anthropic"
import { convertToModelMessages, streamText, type UIMessage } from "ai"

const SYSTEM_PROMPT = `
あなたは業務システムの要件定義を手伝うヒアリング担当者です。
ユーザーとの対話を通じて、システムに実現してほしいことを丁寧に引き出してください。

- 曖昧な発言は、具体的な業務場面（誰が・いつ・何を）に落とし込んで確認する
- ユーザーが言っていないことを勝手に補って進めない。不明な点は質問する
- 一度に多くを質問しすぎず、対話のテンポを保つ
`.trim()

/** 要件ヒアリングを行うチャットエージェント */
export class ChatAgent {
  /**
   * チャット履歴に対する応答をストリーミングで返す。
   * apiKey は呼び出しごとに渡された値でプロバイダーを生成する（環境変数は参照しない）。
   */
  async respond(messages: UIMessage[], options: { apiKey: string, model: string }): Promise<Response> {
    const anthropic = createAnthropic({ apiKey: options.apiKey })
    const result = streamText({
      model: anthropic(options.model),
      system: SYSTEM_PROMPT,
      messages: await convertToModelMessages(messages),
    })
    return result.toUIMessageStreamResponse()
  }
}

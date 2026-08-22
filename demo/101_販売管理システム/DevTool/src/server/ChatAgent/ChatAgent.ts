import { createAnthropic } from "@ai-sdk/anthropic"
import { convertToModelMessages, streamText } from "ai"
import { CurrentStateDto } from "../../shared/devtool-api"

const SYSTEM_PROMPT = `
あなたはこのシステムの構築を補助する者です。
ユーザー入力に応じて以下いずれかのタスクを遂行します。

- このシステムの現在の状態についての情報を答える
- ユーザーの中でも明確化されていない情報（このシステムの目的、スコープ、仕様）を
  明確化するための意思決定の補助をする
- このシステムへの具体的な機能追加や不具合修正を行う
- ユーザーの意図が上記いずれに該当するか明確でない場合に、明確化するための問いかけをする
`.trim()

/** 要件ヒアリングを行うチャットエージェント */
export class ChatAgent {
  /**
   * チャット履歴に対する応答をストリーミングで返す。
   * apiKey は呼び出しごとに渡された値でプロバイダーを生成する（環境変数は参照しない）。
   */
  async respond(currentState: CurrentStateDto, options: { apiKey: string, model: string }): Promise<Response> {
    const anthropic = createAnthropic({ apiKey: options.apiKey })
    const result = streamText({
      model: anthropic(options.model),
      system: SYSTEM_PROMPT,
      messages: await convertToModelMessages(currentState.currentSession),
    })
    return result.toUIMessageStreamResponse()
  }
}

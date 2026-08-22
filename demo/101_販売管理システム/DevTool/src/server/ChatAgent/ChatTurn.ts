import type { GenerateTextStartEvent, Telemetry } from "ai"
import type { ServerLog } from "../ServerLog.ts"

/**
 * RootAgent.respond から ResearchAgent.research まで、LLM呼び出しの1回1回に共通して必要になる値。
 * turn は同じユーザーターンの中で作られた呼び出し（主エージェント・サブエージェントの双方）を
 * 1つの計器にまとめるために使う（{@link ChatTurn} 参照）。abortSignal はブラウザが接続を切った際に
 * 進行中のLLM呼び出し・ツール呼び出しを打ち切るために使う。
 */
export type AgentCallOptions = {
  apiKey: string
  model: string
  turn: ChatTurn
  log: ServerLog
  abortSignal: AbortSignal
}

/** LLM1ステップの応答時間がこれを超えたら warn に引き上げる（slow: true を付けて同じイベント名のまま）。 */
const SLOW_LLM_MS = 30_000

/** ツール1回の実行時間がこれを超えたら warn に引き上げる。 */
const SLOW_TOOL_MS = 5_000

/** Telemetry の各コールバックに渡ってくる、対応する名前のイベントの実際の型を取り出す。 */
type TelemetryEvent<Name extends keyof Telemetry> = Telemetry[Name] extends ((event: infer Event) => unknown) | undefined ? Event : never

/**
 * ユーザーの1ターン（{@link RootAgent.respond} の1回の呼び出し）を横断して観測する AI SDK の
 * Telemetry 実装。RootAgent がターンごとに1つ生成し、そのターン中に呼ぶサブエージェント
 * （{@link ResearchAgent.research}）にも同じインスタンスを渡すことで、
 * サブエージェントのLLM呼び出し・ツール呼び出しも同じ計器に乗せる。
 * `telemetry: { functionId, integrations: [turn] }` として streamText / generateText の両方に渡す。
 *
 * onEnd はあえて実装しない: streamText の telemetry.onEnd はストリームが最後まで読み切られたときしか
 * 発火せず、ブラウザがタブを閉じた場合には飛ばない。ターン終了の1行は
 * toUIMessageStream の onEnd（cancel でも発火する）側で {@link ChatTurn.summary} を読んで出す
 * （RootAgent.ts 参照）。これによりターン終了ログが二重に出ることも構造的に無くなる。
 */
export class ChatTurn implements Telemetry {
  readonly #log: ServerLog
  #steps = 0
  #tokensIn = 0
  #tokensOut = 0
  #llmMs = 0
  #toolCalls = 0
  #toolFailures = 0
  #toolMs = 0

  constructor(log: ServerLog) {
    this.#log = log
  }

  /** ターン開始時にプロンプト一式のスナップショットをトレースへ残す。 */
  onStart = (event: TelemetryEvent<"onStart">): void => {
    // onStart は generateText/generateObject/embed/rerank 共通の型だが、
    // ChatTurn は generateText・streamText でしか使わないため、実際に来る型として扱う。
    const generateTextEvent = event as unknown as GenerateTextStartEvent & { functionId?: string }
    this.#log.trace("llm.start", {
      functionId: generateTextEvent.functionId,
      provider: generateTextEvent.provider,
      modelId: generateTextEvent.modelId,
      toolNames: generateTextEvent.tools ? Object.keys(generateTextEvent.tools) : [],
      instructions: generateTextEvent.instructions,
      messages: generateTextEvent.messages,
    })
  }

  /** ツール実行開始をトレースへ残す。research_* は数十秒かかることがあり、「実行中」が見える唯一の手段。 */
  onToolExecutionStart = (event: TelemetryEvent<"onToolExecutionStart">): void => {
    this.#log.trace("tool.start", {
      functionId: event.functionId,
      tool: event.toolCall.toolName,
      toolCallId: event.toolCall.toolCallId,
      input: event.toolCall.input,
    })
  }

  /** 1ステップ（1回のLLM呼び出し）完了。スループット計測の主役。 */
  onStepEnd = (event: TelemetryEvent<"onStepEnd">): void => {
    this.#steps++
    this.#tokensIn += event.usage.inputTokens ?? 0
    this.#tokensOut += event.usage.outputTokens ?? 0
    this.#llmMs += event.performance.responseTimeMs

    const toolMs = Object.values(event.performance.toolExecutionMs).reduce((sum, ms) => sum + ms, 0)
    const slow = event.performance.responseTimeMs > SLOW_LLM_MS
    const fields = {
      functionId: event.functionId,
      step: event.stepNumber,
      model: event.model.modelId,
      finishReason: event.finishReason,
      tokensIn: event.usage.inputTokens,
      tokensOut: event.usage.outputTokens,
      // stepTimeMs から responseTimeMs・toolExecutionMs を差し引いた残りが、SDK側の前後処理に費やした時間の目安になる
      stepMs: Math.round(event.performance.stepTimeMs),
      responseMs: Math.round(event.performance.responseTimeMs),
      toolMs: Math.round(toolMs),
      slow: slow || undefined,
    }
    if (slow) this.#log.warn("llm.step", fields)
    else this.#log.info("llm.step", fields)

    this.#log.trace("llm.step.detail", {
      functionId: event.functionId,
      step: event.stepNumber,
      text: event.text,
      toolCalls: event.toolCalls,
      warnings: event.warnings,
    })
  }

  /** 1回のツール実行完了。ツールの妥当性評価の主役。 */
  onToolExecutionEnd = (event: TelemetryEvent<"onToolExecutionEnd">): void => {
    this.#toolCalls++
    this.#toolMs += event.toolExecutionMs

    const output = event.toolOutput
    // ProjectFiles のツールは「見つかりません」等の失敗を { error: "..." } という
    // “成功”の戻り値として返す。toolOutput.type だけを見ると失敗ゼロに見えてしまうため、
    // 戻り値の形も併せて見て妥当性を判定する。
    const ok = output.type === "tool-result" && !isPseudoFailure(output.output)
    if (!ok) this.#toolFailures++

    const slow = event.toolExecutionMs > SLOW_TOOL_MS
    const fields = {
      functionId: event.functionId,
      tool: event.toolCall.toolName,
      ok,
      ms: Math.round(event.toolExecutionMs),
      chars: outputChars(output),
      slow: slow || undefined,
    }
    if (ok && !slow) this.#log.info("tool.call", fields)
    else this.#log.warn("tool.call", fields)

    let outputContent: string | undefined
    if (output.type === "tool-result") {
      outputContent = event.toolCall.toolName === "read_file"
        ? "割愛"
        : output.output
    } else {
      outputContent = undefined
    }

    this.#log.trace("tool.detail", {
      functionId: event.functionId,
      tool: event.toolCall.toolName,
      input: event.toolCall.input,
      output: outputContent,
      error: output.type === "tool-error" ? String(output.error) : undefined,
    })
  }

  /** ブラウザが閉じられた等でストリームが中断された。 */
  onAbort = (event: TelemetryEvent<"onAbort">): void => {
    this.#log.warn("chat.turn.abort", {
      functionId: event.functionId,
      steps: event.steps.length,
      reason: event.reason === undefined ? undefined : String(event.reason),
    })
  }

  /** LLM呼び出し・ツール呼び出しのいずれかで回復不能なエラーが起きた。 */
  onError = (event: unknown): void => {
    this.#log.error("chat.turn.error", { error: event instanceof Error ? event : String(event) })
  }

  /** ターン終了時に呼ぶ側（RootAgent）へ、このターン中に集計した値を渡す。 */
  summary() {
    return {
      steps: this.#steps,
      tokensIn: this.#tokensIn,
      tokensOut: this.#tokensOut,
      llmMs: Math.round(this.#llmMs),
      toolCalls: this.#toolCalls,
      toolFailures: this.#toolFailures,
      toolMs: Math.round(this.#toolMs),
    } as const
  }
}

/** ツールの戻り値が、成功の形をしているが実質は失敗（`{ error: "..." }`）かどうか */
function isPseudoFailure(output: unknown): boolean {
  return typeof output === "object" && output !== null && "error" in output
}

/** ログに載せる目安として、ツールの出力（または失敗時のエラー）の文字数を数える */
function outputChars(toolOutput: { type: "tool-result" | "tool-error", output?: unknown, error?: unknown }): number {
  const value = toolOutput.type === "tool-result" ? toolOutput.output : toolOutput.error
  if (typeof value === "string") return value.length
  try {
    return JSON.stringify(value)?.length ?? 0
  } catch {
    return 0
  }
}

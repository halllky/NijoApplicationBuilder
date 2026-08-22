import { tool } from "ai"
import { z } from "zod"
import type { ProjectFiles } from "../ProjectFiles.ts"

/**
 * {@link ProjectFiles} の一覧・閲覧操作を Vercel AI SDK のツール定義として公開する。
 * ChatAgent 本体と ResearchAgent の両方が同じ実装を使う（ツール定義の重複を避けるための共通化）。
 */
export function readOnlyFileTools(projectFiles: ProjectFiles) {
  return {
    list_files: tool({
      description: "プロジェクト内の指定ディレクトリ直下のファイル・サブディレクトリ一覧を返す。ディレクトリはサブディレクトリ末尾に '/' が付く。",
      inputSchema: z.object({
        path: z.string().describe("一覧したいディレクトリのプロジェクトルートからの相対パス。ルート自体を見る場合は '.' を指定する。"),
      }),
      execute: async ({ path }) => {
        const entries = await projectFiles.list(path)
        return entries ?? { error: `ディレクトリが見つかりません: ${path}` }
      },
    }),
    read_file: tool({
      description: "プロジェクト内の指定ファイルの内容をテキストとして返す。",
      inputSchema: z.object({
        path: z.string().describe("読みたいファイルのプロジェクトルートからの相対パス。"),
      }),
      execute: async ({ path }) => {
        const content = await projectFiles.read(path)
        return content ?? { error: `ファイルが見つかりません: ${path}` }
      },
    }),
  }
}

/**
 * {@link ProjectFiles.grep} を Vercel AI SDK のツール定義として公開する。
 * 全文検索は調査範囲が広い ResearchAgent 専用とし、主エージェントには持たせない。
 */
export function grepFileTool(projectFiles: ProjectFiles) {
  return {
    grep: tool({
      description: "プロジェクト内を再帰的に検索し、指定した文字列を含む行の一覧を返す（大文字小文字を区別しない部分一致）。結果が多すぎる場合は打ち切られる（truncated）ため、必要なら path や extensions で絞り込むこと。",
      inputSchema: z.object({
        query: z.string().describe("検索したい文字列。"),
        path: z.string().describe("検索を開始するディレクトリのプロジェクトルートからの相対パス。プロジェクト全体を検索する場合は '.' を指定する。"),
        extensions: z.array(z.string()).optional().describe("拡張子で絞り込みたい場合に指定する（例: ['.cs', '.xml']）。指定しない場合は全ファイルが対象。"),
      }),
      execute: async ({ query, path, extensions }) => {
        const result = await projectFiles.grep(query, path, extensions)
        return result ?? { error: `ディレクトリが見つかりません: ${path}` }
      },
    }),
  }
}

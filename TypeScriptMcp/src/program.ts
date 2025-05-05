/**
 * TypeScript版MCPのメインプログラム
 */

import * as path from 'path';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { z } from 'zod';
import { trySetup } from './util';
import { TypeScriptFinder } from './typescript-finder';

/**
 * McpServerを作成して設定する
 */
async function setupServer() {
  // MCPサーバーの作成
  const server = new McpServer({
    name: 'typescript-mcp',
    version: '1.0.0'
  });

  // find_definition: シンボルの定義情報を検索するツール
  server.tool(
    'find_definition',
    {
      sourceFilePath: z.string().min(1, 'ファイルパスは必須です'),
      line: z.string().regex(/^\d+$/, '行番号は数値を指定してください'),
      character: z.string().regex(/^\d+$/, '文字位置は数値を指定してください')
    },
    async ({ sourceFilePath, line, character }) => {
      // 引数のチェック
      if (!path.isAbsolute(sourceFilePath)) {
        return {
          content: [{ type: 'text', text: 'sourceFilePathは絶対パスで指定してください。' }]
        };
      }

      const lineNumber = parseInt(line, 10);
      const columnNumber = parseInt(character, 10);
      const { context, error } = trySetup();

      if (error) {
        return { content: [{ type: 'text', text: error }] };
      }

      if (!context) {
        return { content: [{ type: 'text', text: 'コンテキストの初期化に失敗しました。' }] };
      }

      try {
        // TypeScriptプロジェクトを解析
        const finder = new TypeScriptFinder(context.projectPath);
        const result = finder.findDefinition(sourceFilePath, lineNumber, columnNumber);

        if (!result || result.locations.length === 0) {
          return {
            content: [{
              type: 'text',
              text: `シンボル ${result?.name || 'unknown'} の定義情報は見つかりませんでした。`
            }]
          };
        }

        const locationsText = result.locations.map(loc =>
          `* ${loc.filePath}: ${loc.line}行目 ${loc.column}文字目 付近`
        ).join('\n');

        return {
          content: [{
            type: 'text',
            text: `シンボル ${result.name} は以下のソースコードで定義されています。\n${locationsText}`
          }]
        };
      } catch (ex: any) {
        context.writeLog(ex.toString());
        return { content: [{ type: 'text', text: ex.toString() }] };
      }
    }
  );

  // find_references: シンボルの参照を検索するツール
  server.tool(
    'find_references',
    {
      sourceFilePath: z.string().min(1, 'ファイルパスは必須です'),
      line: z.string().regex(/^\d+$/, '行番号は数値を指定してください'),
      character: z.string().regex(/^\d+$/, '文字位置は数値を指定してください')
    },
    async ({ sourceFilePath, line, character }) => {
      // 引数のチェック
      if (!path.isAbsolute(sourceFilePath)) {
        return {
          content: [{ type: 'text', text: 'sourceFilePathは絶対パスで指定してください。' }]
        };
      }

      const lineNumber = parseInt(line, 10);
      const columnNumber = parseInt(character, 10);
      const { context, error } = trySetup();

      if (error) {
        return { content: [{ type: 'text', text: error }] };
      }

      if (!context) {
        return { content: [{ type: 'text', text: 'コンテキストの初期化に失敗しました。' }] };
      }

      try {
        // TypeScriptプロジェクトを解析
        const finder = new TypeScriptFinder(context.projectPath);
        const result = finder.findReferences(sourceFilePath, lineNumber, columnNumber);

        if (!result || result.locations.length === 0) {
          return {
            content: [{
              type: 'text',
              text: `シンボル ${result?.name || 'unknown'} はどこからも参照されていません。`
            }]
          };
        }

        const locationsText = result.locations.map(loc =>
          `* ${loc.filePath}: ${loc.line}行目 ${loc.column}文字目 付近`
        ).join('\n');

        return {
          content: [{
            type: 'text',
            text: `シンボル ${result.name} は以下のソースコードで参照されています。\n${locationsText}`
          }]
        };
      } catch (ex: any) {
        context.writeLog(ex.toString());
        return { content: [{ type: 'text', text: ex.toString() }] };
      }
    }
  );

  // suggest_abstract_members: クラスが実装すべきメンバーを提案するツール
  server.tool(
    'suggest_abstract_members',
    {
      sourceFilePath: z.string().min(1, 'ファイルパスは必須です'),
      line: z.string().regex(/^\d+$/, '行番号は数値を指定してください'),
      character: z.string().regex(/^\d+$/, '文字位置は数値を指定してください')
    },
    async ({ sourceFilePath, line, character }) => {
      // 引数のチェック
      if (!path.isAbsolute(sourceFilePath)) {
        return {
          content: [{ type: 'text', text: 'sourceFilePathは絶対パスで指定してください。' }]
        };
      }

      const lineNumber = parseInt(line, 10);
      const columnNumber = parseInt(character, 10);
      const { context, error } = trySetup();

      if (error) {
        return { content: [{ type: 'text', text: error }] };
      }

      if (!context) {
        return { content: [{ type: 'text', text: 'コンテキストの初期化に失敗しました。' }] };
      }

      try {
        // TypeScriptプロジェクトを解析
        const finder = new TypeScriptFinder(context.projectPath);
        const requiredMembers = finder.findRequiredMembers(sourceFilePath, lineNumber, columnNumber);

        if (requiredMembers.length === 0) {
          return {
            content: [{ type: 'text', text: 'このクラスは実装する必要のあるメンバーがありません。' }]
          };
        }

        const membersText = requiredMembers.map(m => `* ${m}`).join('\n');

        return {
          content: [{
            type: 'text',
            text: `このクラスは以下のメンバーを実装する必要があります。\n${membersText}`
          }]
        };
      } catch (ex: any) {
        context.writeLog(ex.toString());
        return { content: [{ type: 'text', text: ex.toString() }] };
      }
    }
  );

  // StdioTransportを使用してサーバー接続
  const transport = new StdioServerTransport();
  await server.connect(transport);

  console.log('TypeScript MCP サーバーが起動しました');
}

// サーバーセットアップを実行
setupServer().catch(error => {
  console.error('サーバー起動エラー:', error);
  process.exit(1);
});

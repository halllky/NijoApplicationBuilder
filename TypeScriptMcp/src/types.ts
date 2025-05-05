/**
 * TypeScript版MCPの型定義
 */

import * as fs from 'fs';
import * as path from 'path';

/**
 * リクエスト1回分のコンテキスト情報
 */
export class TypeScriptMcpSessionContext {
  /**
   * 処理対象のプロジェクトの絶対パス
   */
  projectPath: string = '';

  /**
   * 不具合発生時の調査用のログなどの一時ファイルが出力されるフォルダ
   */
  workDirectory: string = '';

  /**
   * ログファイルに情報を追記します。ログファイルはセッションを越えて常に1つ。
   * アーカイブ化などは特に考えていません。
   */
  writeLog(text: string): void {
    const logFile = path.join(this.workDirectory, 'log.txt');
    fs.appendFileSync(logFile, text);
  }
}

/**
 * MCPツールの説明用デコレータ
 */
export function Description(description: string) {
  return function (target: any, propertyKey: string, descriptor: PropertyDescriptor) {
    if (!target.descriptions) {
      target.descriptions = {};
    }
    target.descriptions[propertyKey] = description;
    return descriptor;
  };
}

/**
 * MCPサーバーツールの型
 */
export function McpServerTool(config: { name: string }) {
  return function (target: any, propertyKey: string, descriptor: PropertyDescriptor) {
    if (!target.mcpTools) {
      target.mcpTools = {};
    }
    target.mcpTools[config.name] = propertyKey;
    return descriptor;
  };
}

/**
 * MCPサーバーツールクラスの型
 */
export function McpServerToolType(constructor: Function) {
  // クラスデコレータの実装
  return constructor;
}

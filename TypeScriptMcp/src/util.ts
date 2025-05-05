/**
 * TypeScript版MCPのユーティリティ関数
 */

import * as fs from 'fs';
import * as path from 'path';
import { TypeScriptMcpSessionContext } from './types';

/**
 * TypeScriptMcpのリクエスト1回分のセットアップ
 */
export function trySetup(): { context: TypeScriptMcpSessionContext | null, error: string | null } {
  try {
    const thisExeDir = __dirname;

    // 実行時設定ファイルの読み込み
    const appSettingsPath = path.join(thisExeDir, '..', 'appsettings.json');
    const appSettingsJson = fs.readFileSync(appSettingsPath, 'utf8');
    const appSettings = JSON.parse(appSettingsJson);

    // TypeScriptMcpセクションが存在するか確認
    const TYPESCRIPT_MCP_SECTION = 'TypeScriptMcp';
    if (!appSettings[TYPESCRIPT_MCP_SECTION]) {
      throw new Error(`appsettings.jsonに${TYPESCRIPT_MCP_SECTION}セクションが見つかりません。`);
    }

    // ワークディレクトリのセットアップ
    const workDirectory = path.resolve(
      thisExeDir,
      appSettings[TYPESCRIPT_MCP_SECTION].WorkDirectory || 'work'
    );

    if (!fs.existsSync(workDirectory)) {
      fs.mkdirSync(workDirectory, { recursive: true });
      fs.writeFileSync(path.join(workDirectory, '.gitignore'), '*'); // git管理対象外
    }

    // 結果返却
    const context = new TypeScriptMcpSessionContext();
    context.projectPath = appSettings[TYPESCRIPT_MCP_SECTION].ProjectPath || '';
    if (!context.projectPath) {
      throw new Error(`appsettings.jsonの${TYPESCRIPT_MCP_SECTION}セクションにProjectPathが設定されていません。`);
    }

    context.workDirectory = workDirectory;
    return { context, error: null };

  } catch (ex: any) {
    return {
      context: null,
      error: `MCPツールのセットアップでエラーが発生しました: ${ex.message}`
    };
  }
}

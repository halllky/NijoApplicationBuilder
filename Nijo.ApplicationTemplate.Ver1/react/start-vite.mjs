#!/usr/bin/env node

/**
 * このスクリプトはVite開発サーバーを起動し、起動状態を標準出力に出力します。
 * ログのリダイレクト時のバッファリング問題を解決するために、改行を含めて出力します。
 * MCPサーバーからの起動を想定しています。
 */

import { createServer } from 'vite';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const configFile = path.resolve(__dirname, 'vite.config.ts');
const logFilePath = path.resolve(__dirname, 'start-vite.log');

// ログ出力関数
function log(level, message) {
    const timestamp = new Date().toISOString();
    const logMessage = `${timestamp} [${level}] ${message}\n`;

    // コンソールに出力
    if (level === 'ERROR') {
        console.error(logMessage);
    } else {
        console.log(logMessage);
    }

    // ファイルに追記
    try {
        fs.appendFileSync(logFilePath, logMessage);
    } catch (err) {
        console.error(`ログファイルへの書き込みに失敗しました: ${err}\n`);
        // ログファイル書き込み失敗は致命的ではないので、処理は続行
    }
}

// スクリプト開始ログ
log('INFO', 'start-vite.mjs スクリプト開始');

// ログファイルの初期化（既存ファイルがあれば削除）
try {
    if (fs.existsSync(logFilePath)) {
        fs.unlinkSync(logFilePath);
        log('INFO', `既存のログファイル ${logFilePath} を削除しました。`);
    }
} catch (err) {
    log('ERROR', `既存のログファイル ${logFilePath} の削除に失敗しました: ${err}`);
    process.exit(1); // ログファイルの初期化失敗は問題がある可能性が高いので終了
}


// vite.config.tsが存在するか確認
log('INFO', `設定ファイルを確認中: ${configFile}`);
if (!fs.existsSync(configFile)) {
    log('ERROR', `設定ファイル '${configFile}' が見つかりません。`);
    process.exit(1);
}
log('INFO', '設定ファイルが見つかりました。');

async function startServer() {
    try {
        log('INFO', 'Viteサーバーを起動中...');

        // Viteサーバーの作成と設定
        log('INFO', 'Viteサーバーを作成します。');
        const server = await createServer({
            // 設定ファイルのパスを指定
            configFile,
            // 強制的に指定されたポートを使用（他のプロセスが使用中の場合はエラー）
            server: {
                port: 5173,
                strictPort: true,
            },
            // クリアスクリーン無効化
            clearScreen: false,
            // ログレベル（'info'はデフォルト）
            logLevel: 'debug', // Vite自身のログレベル
        });
        log('INFO', 'Viteサーバーの作成が完了しました。');

        // サーバーを起動してリクエストのリッスンを開始
        log('INFO', 'Viteサーバーのリスンを開始します。');
        await server.listen();
        log('INFO', 'Viteサーバーのリスンが開始されました。');

        // サーバーのURLとステータスをログに出力
        const serverInfo = server.resolvedUrls?.local[0]; // localは配列の場合がある
        if (serverInfo) {
            log('INFO', `Viteサーバーが起動しました: ${serverInfo}`);
        } else {
            log('WARN', 'ViteサーバーのURLが取得できませんでした。起動はしている可能性があります。');
        }

        // プロセス終了時にサーバーを停止
        const shutdown = async (signal) => {
            log('INFO', `${signal} シグナル受信。Viteサーバーを停止中...`);
            try {
                await server.close();
                log('INFO', 'Viteサーバーが正常に停止しました。');
                process.exit(0);
            } catch (err) {
                log('ERROR', `Viteサーバーの停止中にエラーが発生しました: ${err}`);
                process.exit(1);
            }
        };

        // SIGINT（Ctrl+C）とSIGTERM（終了シグナル）を処理
        log('INFO', 'シャットダウンハンドラを設定します。');
        process.on('SIGINT', () => shutdown('SIGINT'));
        process.on('SIGTERM', () => shutdown('SIGTERM'));
        log('INFO', 'シャットダウンハンドラの設定が完了しました。');

        log('INFO', 'start-vite.mjs の初期化処理が正常に完了しました。');
        return server;

    } catch (error) {
        // エラーメッセージとスタックトレースをログに出力
        log('ERROR', `Viteサーバーの起動中にエラーが発生しました: ${error.message}`);
        if (error.stack) {
            log('ERROR', `スタックトレース: ${error.stack}`);
        }
        process.exit(1);
    }
}

// サーバー起動関数の呼び出し
startServer().catch(error => {
    // startServer内でキャッチされなかった予期せぬエラー
    log('ERROR', `サーバー起動関数の実行中に予期せぬエラーが発生しました: ${error.message}`);
    if (error.stack) {
        log('ERROR', `スタックトレース: ${error.stack}`);
    }
    process.exit(1);
}); 
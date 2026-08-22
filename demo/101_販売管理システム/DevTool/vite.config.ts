import { defineConfig } from "vite"
import react from "@vitejs/plugin-react-swc"
import tailwindcss from "@tailwindcss/vite"

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  // クライアントに露出してよい環境変数は VITE_ 接頭辞のもののみ（既定値だが明示する）。
  // APIキーやモデル設定はサーバー側（src/server）のみが .env.local を読み書きするため、
  // 接頭辞を広げるとそれらがブラウザバンドルに漏れる。
  envPrefix: 'VITE_',
  server: {
    port: 5177,
    strictPort: true,
    proxy: {
      // src/server（デバッグ実行プロセスの起動・停止・ログ取得、チャット、設定のAPI）
      '/devtool-api': { target: 'http://localhost:5184' },
    },
  }
})

import { defineConfig } from "vite"
import react from "@vitejs/plugin-react-swc"
import tailwindcss from "@tailwindcss/vite"

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    port: 5177,
    strictPort: true,
    proxy: {
      // DevTool.Server（デバッグ実行プロセスの起動・停止・ログ取得API）
      '/devtool-api': { target: 'http://localhost:5184' },
    },
  }
})

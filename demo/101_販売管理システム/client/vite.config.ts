import { defineConfig } from "vite"
import { viteSingleFile } from "vite-plugin-singlefile"
import react from "@vitejs/plugin-react-swc"
import tailwindcss from "@tailwindcss/vite"

// 共有デモサイトでは WebService (nijo serve --demo-mode) がリバースプロキシ経由で
// このViteサーバーを /demo/ 配下に公開する。
// 注意: Viteの開発サーバーはserveコマンド時、`base`をHTMLのアセット参照(/@vite/client,
// /src/main.tsx 等)には反映しない(常にルート相対で出力される)ため、
// ここでは base を変更せず、代わりにWebService側のリバースプロキシで
// /demo/ 配下に加えて /@vite/**, /src/**, /node_modules/** 等の
// Vite開発用アセットパスも同じViteサーバーへ転送する構成にしている
// (DemoReverseProxyConfig.cs 参照)。ルーティング(basename)だけは
// VITE_DEMO_BASE を使ってアプリ側で明示的に切り替える(routes.tsx参照)。
const isDemoMode = !!process.env.VITE_DEMO_BASE

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    viteSingleFile(),
  ],
  build: {
    minify: false,
  },
  server: {
    port: 5173,
    strictPort: true,
    host: true,
    // プロキシ経由のHostヘッダを拒否しないようにする
    allowedHosts: isDemoMode ? true : undefined,
    hmr: isDemoMode ? {
      protocol: "wss",
      clientPort: 443,
      path: "/__hmr",
    } : undefined,
  }
})

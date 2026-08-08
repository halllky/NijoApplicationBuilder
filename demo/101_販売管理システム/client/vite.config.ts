import { defineConfig } from "vite"
import { viteSingleFile } from "vite-plugin-singlefile"
import react from "@vitejs/plugin-react-swc"
import tailwindcss from "@tailwindcss/vite"

// 共有デモサイトでは WebService (nijo serve --demo-mode) がビルド済みのこのSPAを
// リバースプロキシ経由で /demo/ 配下に公開する(WebApiが単一プロセスで静的配信する。
// Demo101ProcessManager.cs / DemoReverseProxyConfig.cs 参照)。
// public/ 配下の画像等、JSにバンドルされないアセットの参照パスを /demo/ プレフィックス込みで
// 出力する必要があるため、ビルド時は base を VITE_DEMO_BASE に合わせる
// (JS/CSSはviteSingleFileでHTMLにインライン化されるため影響を受けない)。
// ルーティング(basename)も同じ VITE_DEMO_BASE を使う(routes.tsx参照)。
const isDemoMode = !!process.env.VITE_DEMO_BASE

export default defineConfig({
  base: isDemoMode ? process.env.VITE_DEMO_BASE : "/",
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

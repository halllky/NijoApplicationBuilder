import { defineConfig } from 'vite'

export default defineConfig({
  server: {
    port: 5173,
    strictPort: true,
    host: true,
    proxy: {
      // ここのポートは ASP.NET Core デバッグ設定と合わせる必要がある
      '/api': { target: 'http://localhost:5290' },
      '/swagger': { target: 'http://localhost:5290' },
    },
  },
})

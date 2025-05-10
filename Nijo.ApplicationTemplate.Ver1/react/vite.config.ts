import { defineConfig, PluginOption } from 'vite'
import { viteSingleFile } from 'vite-plugin-singlefile'
import react from '@vitejs/plugin-react-swc'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default ({ mode }: { mode: string }) => {
  const plugins: PluginOption[] = [
    react(),
    tailwindcss(),
  ]

  // Windows Forms 埋め込み用ビルドの場合は
  // html, css, js をすべて1つのファイルにバンドルする。
  // modeは package.json でのビルドコマンド起動時に指定される。
  if (mode === 'nijo-ui') {
    plugins.push(viteSingleFile())
  }

  return defineConfig({
    logLevel: 'info',
    clearScreen: false,
    plugins,
    server: {
      port: 5173,
      strictPort: true,
    }
  })
}
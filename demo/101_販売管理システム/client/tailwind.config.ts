import type { Config } from 'tailwindcss'

export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
    // 依存先パッケージ（ui-components）のファイルも監視対象に含める
    "../../../Nijo.GuiClient/package_ui-components/src/**/*.{js,ts,jsx,tsx}",
    // 依存先パッケージ（react-editable-grid）のビルド済みファイルも監視対象に含める
    "../../../node_modules/@halllky/react-editable-grid/dist/**/*.js",
  ],
} satisfies Config

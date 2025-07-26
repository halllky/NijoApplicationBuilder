import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "Nijo Application Builder",
  description: "スキーマ駆動型アプリケーション生成フレームワーク",

  // GitHub Pages用の設定
  base: '/NijoApplicationBuilder/',
  outDir: '../../docs',

  // 静的ファイルの設定
  cleanUrls: true,

  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: 'ホーム', link: '/' },
      { text: 'チュートリアル', link: '/tutorials/' },
      { text: 'ハウツーガイド', link: '/how-to-guides/' },
      { text: 'リファレンス', link: '/reference/' },
      { text: '設計思想', link: '/explanation/' }
    ],

    sidebar: [
      {
        text: '📚 Tutorials',
        collapsed: false,
        items: [
          { text: 'チュートリアル概要', link: '/tutorials/' },
          { text: '5分で作る住所録アプリ', link: '/tutorials/getting-started' }
        ]
      },
      {
        text: '🛠️ How-to Guides',
        collapsed: false,
        items: [
          { text: 'ハウツーガイド概要', link: '/how-to-guides/' }
        ]
      },
      {
        text: '📖 Reference',
        collapsed: false,
        items: [
          { text: 'リファレンス概要', link: '/reference/' }
        ]
      },
      {
        text: '💡 Explanation',
        collapsed: false,
        items: [
          { text: '設計思想概要', link: '/explanation/' }
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/example/nijo' }
    ],

    footer: {
      copyright: 'Copyright © 2025 Nijo Application Builder'
    },

    search: {
      provider: 'local'
    }
  }
})

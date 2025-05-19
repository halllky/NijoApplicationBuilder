import React from 'react'
import * as ReactRouter from 'react-router-dom'
import MainLayout from "./layout/MainLayout"
import { getRouter } from './routes'
import * as Util from './util'
import { getNijoUiRoutesForEmbedded } from './スキーマ定義編集UI'

// Windows Form 埋め込み用のビルドの場合はスキーマ定義編集画面を表示
export const IS_EMBEDDED = () => import.meta.env.MODE === 'nijo-ui'

function App() {
  const router = React.useMemo(() => {
    if (!IS_EMBEDDED()) {
      // 通常の自動生成されたアプリケーションの動作確認
      return ReactRouter.createBrowserRouter([{
        path: "/",
        element: (
          <ContextProviders>
            <MainLayout />
          </ContextProviders>
        ),
        children: getRouter(),
      }])

    } else {
      // Windows Form 埋め込み用のビルド
      return ReactRouter.createBrowserRouter(getNijoUiRoutesForEmbedded())
    }
  }, [])

  return (
    <ReactRouter.RouterProvider router={router} />
  )
}

/** 各種コンテキストプロバイダー */
export const ContextProviders = ({ children }: { children: React.ReactNode }) => {
  return (
    <Util.IMEProvider>
      {children}
    </Util.IMEProvider>
  )
}

export default App

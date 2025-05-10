import React from 'react'
import * as ReactRouter from 'react-router-dom'
import MainLayout from "./layout/MainLayout"
import { getRouter } from './routes'
import * as Util from './util'
import { NijoUi } from './debug-rooms/スキーマ定義編集UIの試作/NijoUi'

// Windows Form 埋め込み用のビルドの場合はスキーマ定義編集画面を表示
const IS_EMBEDDED = import.meta.env.MODE === 'nijo-ui'

function App() {
  const router = React.useMemo(() => {
    return ReactRouter.createBrowserRouter([{
      path: "/",
      element: (
        <ContextProviders>
          {IS_EMBEDDED && <NijoUi />}
          {!IS_EMBEDDED && <MainLayout />}
        </ContextProviders>
      ),
      children: getRouter(),
    }])
  }, [])

  return (
    <ReactRouter.RouterProvider router={router} />
  )
}

/** 各種コンテキストプロバイダー */
const ContextProviders = ({ children }: { children: React.ReactNode }) => {
  return (
    <Util.IMEProvider>
      {children}
    </Util.IMEProvider>
  )
}

export default App

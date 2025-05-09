import React from 'react'
import * as ReactRouter from 'react-router-dom'
import MainLayout from "./layout/MainLayout"
import { getRouter } from './routes'
import * as Util from './util'

function App() {
  const router = React.useMemo(() => {
    return ReactRouter.createBrowserRouter([{
      path: "/",
      element: (
        <ContextProviders>
          <MainLayout />
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

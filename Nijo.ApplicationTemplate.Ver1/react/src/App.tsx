import React from 'react'
import * as ReactRouter from 'react-router-dom'
import MainLayout from "./layout/MainLayout"
import { getRouter } from './routes'

function App() {
  const router = React.useMemo(() => {
    return ReactRouter.createBrowserRouter([{
      path: "/",
      element: <MainLayout />,
      children: getRouter(),
    }])
  }, [])

  return (
    <ReactRouter.RouterProvider router={router} />
  )
}

export default App

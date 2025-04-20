import React from 'react'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import MainLayout from './layout/MainLayout'
import { 顧客一覧検索 } from './pages/顧客/顧客一覧検索'

const router = createBrowserRouter([
  {
    path: '/',
    element: <MainLayout />,
    children: [
      {
        index: true,
        element: <顧客一覧検索 />
      },
      {
        path: '顧客',
        element: <顧客一覧検索 />
      }
    ]
  }
])

function App() {
  return (
    <RouterProvider router={router} />
  )
}

export default App

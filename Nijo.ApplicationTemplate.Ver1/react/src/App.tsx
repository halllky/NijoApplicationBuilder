import React from 'react'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import MainLayout from './layout/MainLayout'
import { 顧客一覧検索 } from './pages/顧客/顧客一覧検索'
import { 顧客詳細編集 } from './pages/顧客/顧客詳細編集'
import { Home } from './pages'

const router = createBrowserRouter([
  {
    path: '/',
    element: <MainLayout />,
    children: [
      {
        index: true,
        element: <Home />
      },
      {
        path: '顧客',
        children: [
          {
            index: true,
            element: <顧客一覧検索 />
          },
          {
            path: 'new',
            element: <顧客詳細編集 />
          },
          {
            path: ':顧客ID',
            element: <顧客詳細編集 />
          }
        ]
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

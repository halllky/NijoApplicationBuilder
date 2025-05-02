import React from "react"
import { createBrowserRouter } from "react-router-dom"
import { Home } from "./pages/Home"
import { 顧客一覧検索 } from "./pages/顧客/顧客一覧検索"
import { 顧客詳細編集 } from "./pages/顧客/顧客詳細編集"
import { 従業員一覧検索 } from "./pages/従業員/従業員一覧検索"
import { 従業員詳細編集 } from "./pages/従業員/従業員詳細編集"
import MainLayout from "./layout/MainLayout"

export const router = createBrowserRouter([
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
      },
      {
        path: '従業員',
        children: [
          {
            index: true,
            element: <従業員一覧検索 />
          },
          {
            path: 'new',
            element: <従業員詳細編集 />
          },
          {
            path: ':従業員ID',
            element: <従業員詳細編集 />
          }
        ]
      }
    ]
  }
])

import React from "react"
import { Route, Routes } from "react-router-dom"
import { HomePage } from "./pages/HomePage"
import { 顧客一覧検索 } from "./pages/顧客/顧客一覧検索"
import { 顧客詳細編集 } from "./pages/顧客/顧客詳細編集"

export const AppRoutes = () => {
    return (
        <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/顧客">
                <Route index element={<顧客一覧検索 />} />
                <Route path="new" element={<顧客詳細編集 />} />
                <Route path=":顧客ID" element={<顧客詳細編集 />} />
            </Route>
        </Routes>
    )
}

import * as ReactRouter from "react-router-dom"
import * as Input from "../input"
import * as Layout from "../layout"
import { 基本的入力フォームの実装例 } from "./基本的入力フォームの実装例"
import { IconButtonの実装例 } from "./IconButtonの実装例"
import { EditableGridの実装例 } from "./EditableGridの実装例"
import { RouteObjectWithSideMenuSetting } from "../routes"

/**
 * 標準のUIコンポーネントの実装例や、動作確認を行なうための画面。
 * 頻繁に仕様変更が入る可能性が高いため、デバッグルームといえど変更容易性を重視して作成する。
 */
export const DebugRooms = () => {

  // 開発環境でない場合は表示しない
  if (!import.meta.env.DEV) {
    return null
  }

  return (
    <Layout.PageFrame headerContent={(
      <Layout.PageFrameTitle>デバッグルーム</Layout.PageFrameTitle>
    )}>
      <div className="grid grid-cols-[12rem_1fr] gap-4 p-4">
        <p className="col-span-2">
          標準のUIコンポーネントの実装例や、動作確認を行なうための画面。
        </p>

        <hr className="col-span-2 border-t border-gray-300" />
        <Input.HyperLink to="/debug-rooms/basic-input-form">
          基本的な入力フォーム
        </Input.HyperLink>
        <p>
          基本的なUIコンポーネント（テキストボックス、チェックボックス、ラジオボタン、日付入力、コンボボックス）の実装例。
        </p>

        <hr className="col-span-2 border-t border-gray-300" />
        <Input.HyperLink to="/debug-rooms/icon-button">
          ボタン
        </Input.HyperLink>
        <p>
          ボタンコンポーネント（IconButton）の実装例。
          このアプリケーション内で使用されるボタンは基本的にすべてこれを用いる。
          アイコン有無、アウトライン有無、背景塗りつぶし有無など様々なレイアウトに対応している。
        </p>

        <hr className="col-span-2 border-t border-gray-300" />
        <Input.HyperLink to="/debug-rooms/editable-grid">
          グリッドレイアウト
        </Input.HyperLink>
        <p>
          グリッドコンポーネント（EditableGrid）の実装例。
          縦横の2次元から成る情報が、グリッド状に配置される。
        </p>

        <hr className="col-span-2 border-t border-gray-300" />
        <Input.HyperLink to="/debug-rooms/v-form-3">
          フォームレイアウト
        </Input.HyperLink>
        <p>
          フォームのレイアウト用コンポーネント（VForm3）の実装例を確認する。
          このコンポーネントは、一覧検索画面の検索条件欄や、詳細編集画面の各項目のような、
          ラベルと入力フォームのペアから成るレイアウトを、レスポンシブに配置するためのものである。
        </p>
      </div>
    </Layout.PageFrame>
  )
}

/** 各種デバッグルームのルーティング定義 */
export const DEBUG_ROOMS_ROUTES: RouteObjectWithSideMenuSetting[] = [
  { path: "/debug-rooms", element: <DebugRooms />, sideMenuLabel: "【開発用】デバッグルーム" },
  { path: "/debug-rooms/basic-input-form", element: <基本的入力フォームの実装例 /> },
  { path: "/debug-rooms/icon-button", element: <IconButtonの実装例 /> },
  { path: "/debug-rooms/editable-grid", element: <EditableGridの実装例 /> },
]

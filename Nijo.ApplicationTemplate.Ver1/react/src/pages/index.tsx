import { RouteObjectWithSideMenuSetting } from "../routes";

// *** 実装例 ここから ***
// import { 従業員一覧検索 } from "./【実装例サンプル】従業員/従業員一覧検索";
// import { 従業員詳細編集 } from "./【実装例サンプル】従業員/従業員詳細編集";
// *** 実装例 ここまで ***

/** Home以外の画面のルーティング設定 */
export default function (): RouteObjectWithSideMenuSetting[] {
  return [
    // *** 実装例 ここから ***
    // { path: '従業員', element: <従業員一覧検索 />, sideMenuLabel: "従業員一覧" },
    // { path: '従業員/new', element: <従業員詳細編集 /> },
    // { path: '従業員/:従業員ID', element: <従業員詳細編集 /> },
    // *** 実装例 ここまで ***
  ]
}
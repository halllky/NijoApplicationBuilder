import { RouteObjectWithSideMenuSetting } from "../routes";
import { 従業員一覧検索 } from "./従業員/従業員一覧検索";
import { 従業員詳細編集 } from "./従業員/従業員詳細編集";
import { 顧客一覧検索 } from "./顧客/顧客一覧検索";
import { 顧客詳細編集 } from "./顧客/顧客詳細編集";

/** Home以外の画面のルーティング設定 */
export default function (): RouteObjectWithSideMenuSetting[] {
  return [
    { path: '顧客', element: <顧客一覧検索 />, sideMenuLabel: "顧客一覧" },
    { path: '顧客/new', element: <顧客詳細編集 /> },
    { path: '顧客/:顧客ID', element: <顧客詳細編集 /> },
    { path: '従業員', element: <従業員一覧検索 />, sideMenuLabel: "従業員一覧" },
    { path: '従業員/new', element: <従業員詳細編集 /> },
    { path: '従業員/:従業員ID', element: <従業員詳細編集 /> },
  ]
}
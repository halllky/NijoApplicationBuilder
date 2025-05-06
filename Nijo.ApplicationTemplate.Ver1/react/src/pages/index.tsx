import { RouteObjectWithSideMenuSetting } from "../routes";
import { カテゴリマスタ一覧検索画面 } from "./カテゴリマスタ/カテゴリマスタ一覧検索画面";
import { カテゴリマスタ詳細編集画面 } from "./カテゴリマスタ/カテゴリマスタ詳細編集画面";
// import { 予約一覧検索画面 } from "./予約/予約一覧検索画面";
import { 予約詳細編集画面 } from "./予約/予約詳細編集画面";
import { 店舗マスタ一覧検索画面 } from "./店舗マスタ/店舗マスタ一覧検索画面";
import { 店舗マスタ詳細編集画面 } from "./店舗マスタ/店舗マスタ詳細編集画面";
// import { 倉庫マスタ一覧検索画面 } from "./倉庫マスタ/倉庫マスタ一覧検索画面";
import { 倉庫マスタ詳細編集画面 } from "./倉庫マスタ/倉庫マスタ詳細編集画面";
import { 商品マスタ一覧検索画面 } from "./商品マスタ/商品マスタ一覧検索画面";
import { 商品マスタ詳細編集画面 } from "./商品マスタ/商品マスタ詳細編集画面";
import { 仕入先マスタ一覧検索画面 } from "./仕入先マスタ/仕入先マスタ一覧検索画面";
import { 仕入先マスタ詳細編集画面 } from "./仕入先マスタ/仕入先マスタ詳細編集画面";
import { 従業員マスタ一覧検索画面 } from "./従業員マスタ/従業員マスタ一覧検索画面";
import { 従業員マスタ詳細編集画面 } from "./従業員マスタ/従業員マスタ詳細編集画面";
import { 売上分析一覧検索画面 } from "./売上分析/売上分析一覧検索画面";
import { 売上分析詳細編集画面 } from "./売上分析/売上分析詳細編集画面";
import { 部署一覧検索画面 } from "./部署/部署一覧検索画面";
import { 部署詳細編集画面 } from "./部署/部署詳細編集画面";

/** Home以外の画面のルーティング設定 */
export default function (): RouteObjectWithSideMenuSetting[] {
  return [
    // *** 実装例 ここから ***
    // { path: '従業員', element: <従業員一覧検索 />, sideMenuLabel: "従業員一覧" },
    // { path: '従業員/new', element: <従業員詳細編集 /> },
    // { path: '従業員/:従業員ID', element: <従業員詳細編集 /> },
    // *** 実装例 ここまで ***

    { path: 'カテゴリマスタ', element: <カテゴリマスタ一覧検索画面 />, sideMenuLabel: "カテゴリマスタ" },
    { path: 'カテゴリマスタ/new', element: <カテゴリマスタ詳細編集画面 /> },
    { path: 'カテゴリマスタ/:カテゴリID', element: <カテゴリマスタ詳細編集画面 /> },

    // { path: '予約', element: <予約一覧検索画面 />, sideMenuLabel: "予約" },
    { path: '予約/new', element: <予約詳細編集画面 /> },
    { path: '予約/:予約ID', element: <予約詳細編集画面 /> },

    { path: '店舗マスタ', element: <店舗マスタ一覧検索画面 />, sideMenuLabel: "店舗マスタ" },
    { path: '店舗マスタ/new', element: <店舗マスタ詳細編集画面 /> },
    { path: '店舗マスタ/:店舗ID', element: <店舗マスタ詳細編集画面 /> },

    // { path: '倉庫マスタ', element: <倉庫マスタ一覧検索画面 />, sideMenuLabel: "倉庫マスタ" },
    { path: '倉庫マスタ/new', element: <倉庫マスタ詳細編集画面 /> },
    { path: '倉庫マスタ/:倉庫ID', element: <倉庫マスタ詳細編集画面 /> },

    { path: '商品マスタ', element: <商品マスタ一覧検索画面 />, sideMenuLabel: "商品マスタ" },
    { path: '商品マスタ/new', element: <商品マスタ詳細編集画面 /> },
    { path: '商品マスタ/:商品ID', element: <商品マスタ詳細編集画面 /> },

    { path: '仕入先マスタ', element: <仕入先マスタ一覧検索画面 />, sideMenuLabel: "仕入先マスタ" },
    { path: '仕入先マスタ/new', element: <仕入先マスタ詳細編集画面 /> },
    { path: '仕入先マスタ/:仕入先ID', element: <仕入先マスタ詳細編集画面 /> },

    { path: '従業員マスタ', element: <従業員マスタ一覧検索画面 />, sideMenuLabel: "従業員マスタ" },
    { path: '従業員マスタ/new', element: <従業員マスタ詳細編集画面 /> },
    { path: '従業員マスタ/:従業員ID', element: <従業員マスタ詳細編集画面 /> },

    { path: '売上分析', element: <売上分析一覧検索画面 />, sideMenuLabel: "売上分析" },
    { path: '売上分析/new', element: <売上分析詳細編集画面 /> },
    { path: '売上分析/:売上分析ID', element: <売上分析詳細編集画面 /> },

    { path: '部署', element: <部署一覧検索画面 />, sideMenuLabel: "部署" },
    { path: '部署/new', element: <部署詳細編集画面 /> },
    { path: '部署/:部署ID', element: <部署詳細編集画面 /> },
  ]
}
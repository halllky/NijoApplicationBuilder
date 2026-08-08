import { createBrowserRouter } from "react-router-dom"
import * as Icon from "@heroicons/react/24/solid"
import { RootLayout } from "./app/RootLayout"
import * as DetailMessageContext from "./app/DetailMessageContext"
import P000, * as P000Module from "./pages/P000_トップページ"
import P002, * as P002Module from "./pages/P002_ログアウト"
import P100, * as P100Module from "./pages/P100_売上"
import P200, * as P200Module from "./pages/P200_入荷"
import P300, * as P300Module from "./pages/P300_商品"
import P400, * as P400Module from "./pages/P400_従業員"
import P101 from "./pages/P101_売上詳細"
import P201 from "./pages/P201_入荷詳細"
import P301 from "./pages/P301_商品詳細"
import UIComponentCatalog from "./debug-rooms/UIコンポーネントカタログ"
import ER図 from "./debug-rooms/ER図"
import { P001_ログイン } from "./pages/P001_ログイン"
import { LoginUserProvider } from "./app/useLoginLogout"
import { ErrorPage } from "./app/ErrorPage"

// ルートナビゲーションに表示する業務画面の一覧。
// どの画面をナビゲーションに載せるかは、このアプリ固有の構成なのでここで決める。
const navigationItems = [
  { to: P100Module.URL, label: "売上", icon: Icon.CurrencyYenIcon },
  { to: P200Module.URL, label: "入荷", icon: Icon.TruckIcon },
  { to: P300Module.URL, label: "商品", icon: Icon.CubeIcon },
  { to: P400Module.URL, label: "従業員", icon: Icon.UserGroupIcon },
]

export const router = createBrowserRouter([
  {
    element: (
      <DetailMessageContext.Provider>
        <LoginUserProvider>
          <P001_ログイン>
            <RootLayout
              appTitle="販売管理システム"
              appTitleTo={P000Module.URL}
              navigationItems={navigationItems}
              logoutItem={{ to: P002Module.URL, label: "ログアウト", icon: Icon.ArrowRightEndOnRectangleIcon }}
            />
          </P001_ログイン>
        </LoginUserProvider>
      </DetailMessageContext.Provider>
    ),
    children: [

      // 業務画面
      P000,
      P002,
      P100,
      P200,
      P300,
      P400,
      ...P101,
      ...P201,
      P301,

      // デバッグ用画面（開発環境でのみ表示）
      ...(!import.meta.env.DEV ? [] : [
        UIComponentCatalog,
        ER図,
      ]),
    ],
    // loader などでエラーが発生した場合に表示するエラーページ
    errorElement: <ErrorPage />,
  },
])

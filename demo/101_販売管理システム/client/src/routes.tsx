import { createBrowserRouter } from "react-router-dom"
import { RootLayout } from "./layout/RootLayout"
import * as DetailMessageContext from "./util/DetailMessageContext"
import P000 from "./pages/P000_トップページ"
import P002 from "./pages/P002_ログアウト"
import P100 from "./pages/P100_売上"
import P200 from "./pages/P200_入荷"
import P300 from "./pages/P300_商品"
import P400 from "./pages/P400_従業員"
import P101 from "./pages/P101_売上詳細"
import P201 from "./pages/P201_入荷詳細"
import P301 from "./pages/P301_商品詳細"
import UIComponentCatalog from "./debug-rooms/UIコンポーネントカタログ"
import ER図 from "./debug-rooms/ER図"
import { P001_ログイン } from "./pages/P001_ログイン"
import { LoginUserProvider } from "./util/useLoginLogout"
import { ErrorPage } from "./layout/ErrorPage"

export const router = createBrowserRouter([
  {
    element: (
      <DetailMessageContext.Provider>
        <LoginUserProvider>
          <P001_ログイン>
            <RootLayout />
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
], {
  // 共有デモサイトでは /demo/ 配下に公開されるため、ルーティングのbasenameを合わせる。
  // (Viteのアセット配信自体はルート相対のままなので、base設定とは連動させていない。
  //  詳細は vite.config.ts のコメント参照)
  // 通常のローカル開発ではVITE_DEMO_BASEは未設定なので挙動は変わらない。
  basename: import.meta.env.VITE_DEMO_BASE ?? "/",
})

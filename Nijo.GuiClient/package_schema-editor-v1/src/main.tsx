import React from "react"
import * as ReactRouter from "react-router-dom"
import ReactDOM from "react-dom/client"
import * as Util from "@nijo/ui-components/util"
import { getRouterForNijoUi } from "./routing"
import { PersonalSettingsProvider } from "./PersonalSettings"

import "./main.css"
import "allotment/dist/style.css"
import { CtrlSProvider } from "./UI/useCtrlS"
import { DemoModeProvider } from "./DemoMode/DemoModeProvider"
import { DemoBanner } from "./DemoMode/DemoBanner"
import { DemoChatPanel } from "./DemoMode/DemoChatPanel"

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)

function App() {
  const router = React.useMemo(() => {
    return ReactRouter.createBrowserRouter(getRouterForNijoUi())
  }, [])

  return (
    <PersonalSettingsProvider>
      <DemoModeProvider>
        <CtrlSProvider>
          <DemoBanner />
          <ReactRouter.RouterProvider router={router} />
          <DemoChatPanel />
        </CtrlSProvider>
      </DemoModeProvider>
    </PersonalSettingsProvider>
  )
}

/**
 * WindowsForms埋め込みアプリまたはそのデバッグ用のデバッグ用サーバーのURL。
 * Nijo/Properties/launchSettings.json のうち
 * Task/NijoServeデバッグ.bat で指定されているプロファイルのポート番号とあわせること。
 */
export const SERVER_DOMAIN = import.meta.env.DEV
  ? 'http://localhost:5001'
  : '';

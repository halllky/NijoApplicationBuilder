import React from "react"
import * as ReactRouter from "react-router-dom"
import { useNavigate } from "react-router-dom"
import { useLoginLogout } from "../app/useLoginLogout"
import { NowLoading } from "../ui/NowLoading"

export const URL = "/logout"

export default {
  path: URL,
  element: <P002_ログアウト />,
} satisfies ReactRouter.RouteObject

/**
 * ログアウト画面。
 * 独立した画面にしているのは、編集画面などからログアウトしたときに
 * useBlocker による確認ダイアログが出るようにするため。
 */
function P002_ログアウト() {
  const { logoutAsync } = useLoginLogout()
  const navigate = useNavigate()

  React.useEffect(() => {
    const execute = async () => {
      await logoutAsync()
      navigate("/")
    }
    execute()
  }, [logoutAsync, navigate])

  return <NowLoading />
}

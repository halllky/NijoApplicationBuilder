import React, { useEffect } from "react"
import { useBlocker } from "react-router-dom"

/**
 * 編集中の内容がある場合に、画面遷移やブラウザのリロード/閉じる操作をブロックし、
 * ブラウザ標準の確認ダイアログを表示するフック。
 *
 * @param isDirty 編集中かどうか (true の場合ブロックする)
 */
export function useUnsavedChangesBlocker(isDirty: boolean) {
  // 画面遷移ブロック (React Router)
  const blocker = useBlocker(isDirty)
  useEffect(() => {
    if (blocker.state === "blocked") {
      const result = window.confirm(
        "編集中の内容があります。移動してもよろしいですか？"
      );
      if (result) {
        blocker.proceed?.()
      } else {
        blocker.reset?.()
      }
    }
  }, [blocker])

  // ブラウザのリロードや閉じる操作のブロック
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (isDirty) {
        e.preventDefault()
        e.returnValue = ""
      }
    }
    window.addEventListener("beforeunload", handleBeforeUnload)
    return () => window.removeEventListener("beforeunload", handleBeforeUnload)
  }, [isDirty])
}

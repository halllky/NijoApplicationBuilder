import React from "react"
import { PREVIEW_TARGET_ORIGIN } from "../../shared/devtool-api"

/**
 * デバッグ対象アプリを埋め込むiframeの再読み込み指示を扱う。
 * クロスオリジンのiframeは表示中のパスを外部から読み取れず、
 * 遷移させることもできないため、表示URLは常にオリジンのルートに固定する。
 */
export function usePreviewFrame() {
  const [reloadKey, setReloadKey] = React.useState(1)

  const reload = React.useCallback(() => {
    setReloadKey(count => count * -1)
  }, [])

  return {
    url: PREVIEW_TARGET_ORIGIN,
    /** iframe の強制再読み込みに使用 */
    reloadKey,
    reload,
  }
}

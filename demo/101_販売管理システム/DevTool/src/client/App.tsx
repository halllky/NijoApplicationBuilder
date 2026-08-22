import { DebugToolbar } from "./DebugToolbar"
import { PreviewFrame, usePreviewFrame, useDevToolState } from "./Preview"

/**
 * 画面全体。デバッグ対象アプリ（生成後アプリ）をiframeで全画面表示し、
 * その手前にデバッグ実行プロセスを操作するフローティングツールバーを重ねる。
 */
export default function App() {
  // iframeに表示するURLと再読み込み指示
  const frame = usePreviewFrame()
  // デバッグ実行プロセスの状態・ログ・DevToolサーバー自身のログとその操作。対象アプリが応答し始めたらiframeを読み込み直す
  const preview = useDevToolState({ onTargetReachable: frame.reload })

  return (
    <div className="fixed inset-0">
      {/* デバッグ対象アプリの埋め込み */}
      <PreviewFrame url={frame.url} reloadKey={frame.reloadKey} />

      {/* デバッグ実行プロセスを操作するフローティングツールバー */}
      <DebugToolbar preview={preview} />
    </div>
  )
}

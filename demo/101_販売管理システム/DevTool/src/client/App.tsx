import { DebugToolbar } from "./DebugToolbar"

/**
 * デバッグ対象アプリ（生成後アプリの client）の vite dev server の URL。
 * DevTool.Server 側の起動設定（Program.cs）と同じポートに合わせる必要がある。
 * vite の開発用モジュールグラフは常にサーバールート直下に解決されるため、
 * DevTool と同一オリジンのサブパスとして埋め込むことはできない。
 */
const DEBUG_TARGET_URL = "http://localhost:5173/"

/**
 * 画面全体。デバッグ対象アプリ（生成後アプリ）をiframeで全画面表示し、
 * その手前にデバッグ実行プロセスを操作するフローティングツールバーを重ねる。
 */
export default function App() {
  return (
    <div className="fixed inset-0">
      {/* デバッグ対象アプリの埋め込み */}
      <iframe src={DEBUG_TARGET_URL} className="w-full h-full border-0" title="デバッグ対象アプリ" />

      {/* デバッグ実行プロセスを操作するフローティングツールバー */}
      <DebugToolbar />
    </div>
  )
}

import React from "react"

/**
 * ログ1本分の表示欄。追記のたびに末尾へ自動スクロールする。
 * デバッグ実行プロセスの標準出力・標準エラー出力（ProcessList）と、
 * DevToolサーバー自身のログ（ServerLogWindow）の両方から使う。
 * 高さは表示先ごとに異なるため呼び出し側が className で決める。
 */
export const LogPane: React.FC<{ text: string, isError?: boolean, className?: string }> = ({ text, isError, className = 'h-[120px]' }) => {
  const ref = React.useRef<HTMLPreElement>(null)

  // DOM要素の末尾へのスクロール位置は描画結果に依存するため、DOM操作としてeffectで行う
  React.useEffect(() => {
    const el = ref.current
    if (el) el.scrollTop = el.scrollHeight
  }, [text])

  return (
    <pre
      ref={ref}
      className={`${className} overflow-auto bg-gray-900 text-xs p-2 whitespace-pre-wrap rounded ${isError ? 'text-rose-400' : 'text-gray-100'}`}
    >
      {text}
    </pre>
  )
}

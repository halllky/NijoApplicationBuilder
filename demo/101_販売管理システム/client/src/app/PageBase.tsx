import { ReactNode, useEffect } from "react"
import * as DetailMessage from "./DetailMessageContext"
import { useUnsavedChangesBlocker } from "./useUnsavedChangesBlocker"

export type PageBaseProps = {
  /** ページタイトル。ブラウザのタイトルバーに表示されます。 */
  browserTitle?: string
  /** trueの場合、ページを離脱する際に確認ダイアログを表示します。 */
  isDirty?: boolean
  /** ヘッダー */
  header?: ReactNode
  /** コンテンツ */
  contents?: ReactNode
  /** フッター */
  footer?: ReactNode
  /** コンテンツエリアの追加クラス */
  className?: string
}

/**
 * ページのベースコンポーネント。
 * ヘッダー、コンテンツ、フッターのレイアウトと、ページタイトルの設定を行います。
 * どこにも表示されないエラーメッセージの表示も行います。
 */
export function PageBase(props: PageBaseProps) {
  useEffect(() => {
    if (props.browserTitle) {
      document.title = props.browserTitle
    }
  }, [props.browserTitle])

  // ページ離脱確認
  useUnsavedChangesBlocker(props.isDirty ?? false)

  return (
    <div className="flex flex-col h-full">

      {/* どこにも表示されないエラーメッセージ */}
      <DetailMessage.Rest className="px-8" />

      {/* ヘッダ */}
      {props.header && (
        <header className="flex flex-wrap items-center px-8 py-1 gap-4">
          {props.header}
        </header>
      )}

      {/* コンテンツ */}
      <div className={`flex-grow flex flex-col px-8 ${props.className ?? ''}`}>
        {props.contents}
      </div>

      {/* フッター */}
      {props.footer && (
        <footer className="px-8 py-1 border-t border-gray-300 flex items-center justify-between bg-gray-50">
          {props.footer}
        </footer>
      )}
    </div>
  )
}

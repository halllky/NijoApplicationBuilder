import * as React from "react"
import * as ReactRouter from "react-router-dom"
import * as Icon from "@heroicons/react/24/solid"
import { useOutletContext } from "react-router-dom"
import { NijoUiOutletContextType } from "./types"
import * as Input from "../input"
import useEvent from "react-use-event-hook"

/** 画面の枠 */
export const PageFrame = ({ shouldBlock, title, headerComponent, children }: {
  /** 何かを保存せずに画面を離脱しようとしているかどうかの条件。離脱時の確認ダイアログを表示するかどうかに影響する。 */
  shouldBlock: boolean
  /** 画面のヘッダ部に表示されるタイトル */
  title?: string
  /** 画面のヘッダ部に表示されるコンポーネント */
  headerComponent?: React.ReactNode
  /** 画面のメインコンテンツ。基本的に height: 100% を指定すること。 */
  children?: React.ReactNode
}) => {

  // サイドメニュー展開ボタン
  const { sideMenuPanel } = useOutletContext<NijoUiOutletContextType>()

  // 離脱時の確認ダイアログ
  // ページの再読み込み前に確認ダイアログを表示する
  ReactRouter.useBeforeUnload(e => {
    if (shouldBlock) {
      e.preventDefault();
    }
  });

  // 別のページへの遷移をブロックする
  const blocker = ReactRouter.useBlocker(
    ({ currentLocation, nextLocation }) =>
      shouldBlock && currentLocation.pathname !== nextLocation.pathname
  );
  React.useEffect(() => {
    if (blocker && blocker.state === "blocked") {
      if (window.confirm("編集中の内容がありますが、ページを離れてもよろしいですか？")) {
        blocker.proceed();
      } else {
        blocker.reset();
      }
    }
  }, [blocker]);

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center flex-wrap gap-1 p-1 min-h-10">

        {/* 画面名 */}
        <Input.IconButton icon={Icon.Bars3Icon} mini hideText onClick={sideMenuPanel.toggleCollapsed}>
          サイドメニュー折り畳み
        </Input.IconButton>
        <h1 className="select-none font-bold">{title}</h1>
        <span className={`text-xs text-gray-700 bg-gray-700 text-white px-1 py-px rounded-sm select-none ${shouldBlock ? '' : 'invisible'}`}>
          未保存
        </span>

        {/* 各画面で任意に指定するヘッダ項目 */}
        {headerComponent}
      </div>
      <div className="flex-1 overflow-auto">
        {children}
      </div>
    </div>
  )
}

import * as React from "react"
import * as ReactRouter from "react-router-dom"
import * as Icon from "@heroicons/react/24/solid"
import { Link, useOutletContext } from "react-router-dom"
import { getNavigationUrl } from "../routes"
import { NijoUiOutletContextType } from "./types"

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

  // アプリケーション名
  const { typedDoc: { appSettings } } = useOutletContext<NijoUiOutletContextType>()
  const [applicationName, setApplicationName] = React.useState<string>()
  React.useEffect(() => {
    setApplicationName(appSettings.applicationName)
  }, [appSettings])

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
        {title ? (
          <>
            <Link to={getNavigationUrl({ page: 'top-page' })} className="font-bold underline">
              {applicationName ?? 'ホーム'}
            </Link>
            <Icon.ChevronRightIcon className="w-4 h-4" />
            <h1 className="select-none font-bold">{title}</h1>
          </>
        ) : (
          <span className="font-bold select-none">
            {applicationName ?? 'ホーム'}
          </span>
        )}
        {headerComponent}
      </div>
      <div className="flex-1 overflow-auto">
        {children}
      </div>
    </div>
  )
}
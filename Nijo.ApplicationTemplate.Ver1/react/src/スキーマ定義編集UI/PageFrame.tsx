import * as React from "react"
import * as Icon from "@heroicons/react/24/solid"
import { Link, useOutletContext } from "react-router-dom"
import { getNavigationUrl } from "../routes"
import { NijoUiOutletContextType } from "./types"

/** 画面の枠 */
export const PageFrame = ({ title, headerComponent, children }: {
  title?: string
  headerComponent?: React.ReactNode
  children?: React.ReactNode
}) => {

  // アプリケーション名
  const { typedDoc: { loadAppSettings } } = useOutletContext<NijoUiOutletContextType>()
  const [applicationName, setApplicationName] = React.useState<string>()
  React.useEffect(() => {
    loadAppSettings().then(settings => setApplicationName(settings.applicationName))
  }, [loadAppSettings])

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
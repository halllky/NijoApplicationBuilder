import { Link } from "react-router-dom"
import * as Icon from "@heroicons/react/24/solid"
import { getNavigationUrl } from "../routes"

/** トップページに戻るボタン */
export const ToTopPageButton = () => {
  return (
    <Link to={getNavigationUrl({ page: 'top-page' })}>
      <Icon.HomeIcon className="w-6 h-6" />
    </Link>
  )
}
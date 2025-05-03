import React from 'react'
import * as ReactRouter from 'react-router-dom'
import { getRouter, RouteObjectWithSideMenuSetting } from '../routes'
/**
 * サイドメニュー。
 * React router でのルーティング定義のうち `sideMenuLabel` が定義されているものを表示する。
 */
const Sidebar: React.FC = () => {

  // ルーティング定義を取得する
  const routes = React.useMemo(() => {
    return getRouter()
  }, [])

  // ルーティング定義のうち `sideMenuLabel` が定義されているものを表示する。
  // children が `sideMenuLabel` である場合もあるため、children も再帰的にフィルタリングする。
  const sideMenuItems = React.useMemo(() => {
    const result: RouteObjectWithSideMenuSetting[] = []
    const filterRoutesRecursively = (routes: RouteObjectWithSideMenuSetting[]): void => {
      for (const route of routes) {
        if (route.sideMenuLabel) {
          result.push(route)
        }
        if (route.children) {
          filterRoutesRecursively(route.children)
        }
      }
    }
    filterRoutesRecursively(routes)
    return result
  }, [routes])

  return (
    <div className="w-64 bg-gray-800 text-white h-full flex flex-col">
      <div className="p-4 border-b border-gray-700">
        <h2 className="text-xl font-semibold">アプリケーション</h2>
      </div>
      <nav className="flex-1 p-4">
        <ul className="space-y-2">

          {sideMenuItems.map((route) => (
            <li key={route.path}>
              <ReactRouter.NavLink
                to={route.path ?? ''}
                className={({ isActive }) =>
                  `block p-2 rounded-md hover:bg-gray-700 transition-colors ${isActive ? 'bg-gray-700' : ''}`
                }
              >
                {route.sideMenuLabel}
              </ReactRouter.NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  )
}

export default Sidebar

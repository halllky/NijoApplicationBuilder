import React from 'react'
import { NavLink } from 'react-router-dom'

const Sidebar: React.FC = () => {
  return (
    <div className="w-64 bg-gray-800 text-white h-full flex flex-col">
      <div className="p-4 border-b border-gray-700">
        <h2 className="text-xl font-semibold">アプリケーション</h2>
      </div>
      <nav className="flex-1 p-4">
        <ul className="space-y-2">
          <li>
            <NavLink
              to="/"
              className={({ isActive }) =>
                `block p-2 rounded-md hover:bg-gray-700 transition-colors ${isActive ? 'bg-gray-700' : ''}`
              }
              end
            >
              ホーム
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/顧客"
              className={({ isActive }) =>
                `block p-2 rounded-md hover:bg-gray-700 transition-colors ${isActive ? 'bg-gray-700' : ''}`
              }
            >
              顧客一覧検索
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/従業員"
              className={({ isActive }) =>
                `block p-2 rounded-md hover:bg-gray-700 transition-colors ${isActive ? 'bg-gray-700' : ''}`
              }
            >
              従業員一覧検索
            </NavLink>
          </li>
        </ul>
      </nav>
    </div>
  )
}

export default Sidebar

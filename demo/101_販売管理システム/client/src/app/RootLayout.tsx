import React from "react"
import { Outlet, Link, useNavigation } from "react-router-dom"
import { NowLoading } from "../ui/NowLoading"

export type RootNavigationItem = {
  to: string
  label: string
  icon?: React.ElementType
}

export type RootLayoutProps = {
  /** アプリケーション名。ルートナビゲーション左端に表示され、トップページへのリンクを兼ねる */
  appTitle: string
  /** アプリケーション名クリック時の遷移先（トップページのURL） */
  appTitleTo: string
  /** ルートナビゲーションに並べる業務画面へのリンク */
  navigationItems: RootNavigationItem[]
  /** ルートナビゲーション右端に表示するログアウトリンク */
  logoutItem: RootNavigationItem
}

/**
 * ログイン後のアプリケーション全体の枠。
 * どの業務画面をナビゲーションに表示するかは呼び出し元（routes.tsx）が決める。
 */
export function RootLayout({ appTitle, appTitleTo, navigationItems, logoutItem }: RootLayoutProps) {
  const navigation = useNavigation()

  return (
    <div className="flex flex-col h-full">

      {/* ルートナビゲーション */}
      <nav className="bg-gray-800 text-white px-8 py-2">
        <ul className="flex flex-wrap gap-x-8 items-center">
          <li className="shrink-0">
            <RootNavigationLink to={appTitleTo} className="text-lg font-bold mr-4">{appTitle}</RootNavigationLink>
          </li>
          {navigationItems.map(item => (
            <li key={item.to} className="shrink-0">
              <RootNavigationLink to={item.to} icon={item.icon}>{item.label}</RootNavigationLink>
            </li>
          ))}

          <li className="flex-1"></li>

          <li className="shrink-0">
            <RootNavigationLink to={logoutItem.to} icon={logoutItem.icon}>{logoutItem.label}</RootNavigationLink>
          </li>
        </ul>
      </nav>

      {/* 各画面のloader実行中でもルートナビゲーションが使えるようにするため relative の位置はここ */}
      <div className="flex-1 overflow-auto relative">

        {/* 各画面の loader 実行中に表示するローディングオーバーレイ */}
        {navigation.state === "loading" && (
          <NowLoading />
        )}

        {/* routes.tsx で指定した各画面はここに表示される */}
        <Outlet />
      </div>
    </div>
  )
}

/** ルートナビゲーションリンク */
function RootNavigationLink({ to, children, className, icon }: {
  to: string
  children: React.ReactNode
  className?: string
  icon?: React.ElementType
}) {
  const IconComponent = icon
  return (
    <Link to={to} className={`hover:text-gray-300 select-none flex items-center gap-1 ${className ?? ''}`}>
      {IconComponent && <IconComponent className="w-5 h-5" />}
      {children}
    </Link>
  )
}

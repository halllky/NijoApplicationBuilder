import React from "react"
import { Link } from "react-router-dom"
import { ArrowsUpDownIcon, CircleStackIcon, Cog6ToothIcon, UserCircleIcon } from "@heroicons/react/24/outline"
import { CopyableText } from "./CopyableText"
import { menuItems } from ".."
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import { NavLink, useLocation } from "react-router-dom"
import { LOCAL_STORAGE_KEYS } from "../hooks/localStorageKeys"

export const SideMenu = () => {
  return (
    <PanelGroup direction="vertical" className="bg-neutral-200" autoSaveId={LOCAL_STORAGE_KEYS.SIDEBAR_SIZE_Y}>

      <Panel className="flex flex-col">
        <Link to='/' className="p-1 ellipsis-ex font-semibold select-none">
          サンプルアプリケーション
        </Link>
        <nav className="flex-1 overflow-y-auto leading-none">
          {menuItems.map(item =>
            <SideMenuLink key={item.url} url={item.url}>{item.text}</SideMenuLink>
          )}
        </nav>
      </Panel>

      <PanelResizeHandle className="h-1 border-b border-neutral-400" />

      <Panel className="flex flex-col">
        <nav className="flex-1 overflow-y-auto leading-none">
          <SideMenuLink url="/changes" icon={ArrowsUpDownIcon}>変更</SideMenuLink>
          <SideMenuLink url="/settings" icon={Cog6ToothIcon}>設定</SideMenuLink>
          <SideMenuLink url="/account" icon={UserCircleIcon}>テスト太郎</SideMenuLink>
        </nav>
        <span className="p-1 text-sm">
          ver. <CopyableText>0.9.0.0</CopyableText>
        </span>
      </Panel>
    </PanelGroup>
  )
}

const SideMenuLink = ({ url, icon, children }: {
  url: string
  icon?: React.ElementType
  children?: React.ReactNode
}) => {
  const location = useLocation()
  const className = location.pathname.startsWith(url)
    ? 'inline-block w-full p-1 ellipsis-ex font-bold bg-white'
    : 'inline-block w-full p-1 ellipsis-ex'
  return (
    <NavLink to={url} className={className}>
      {React.createElement(icon ?? CircleStackIcon, { className: 'inline w-4 mr-1 opacity-70 align-middle' })}
      <span className="text-sm align-middle select-none">{children}</span>
    </NavLink>
  )
}

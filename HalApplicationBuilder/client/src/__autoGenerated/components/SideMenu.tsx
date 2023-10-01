import React, { useRef } from "react"
import { Link } from "react-router-dom"
import { ArrowsUpDownIcon, CircleStackIcon, PlayCircleIcon, UserCircleIcon } from "@heroicons/react/24/outline"
import { CopyableText } from "./CopyableText"
import { menuItems, THIS_APPLICATION_NAME } from ".."
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import { NavLink, useLocation } from "react-router-dom"
import { LOCAL_STORAGE_KEYS } from "../hooks/localStorageKeys"
import * as GlobalFocus from "../hooks/GlobalFocus"

export const SideMenu = () => {
  return (
    <GlobalFocus.TabKeyJumpGroup>
      <PanelGroup direction="vertical" className="bg-neutral-300" autoSaveId={LOCAL_STORAGE_KEYS.SIDEBAR_SIZE_Y}>

        <Panel className="flex flex-col">
          <Link to='/' className="p-1 ellipsis-ex font-semibold select-none">
            {THIS_APPLICATION_NAME}
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
            <SideMenuLink url="/bagkground-tasks" icon={PlayCircleIcon}>バッチ処理</SideMenuLink>
            <SideMenuLink url="/settings" icon={CircleStackIcon}>サーバー</SideMenuLink>
            <SideMenuLink url="/account" icon={UserCircleIcon}>テスト太郎</SideMenuLink>
          </nav>
          <span className="p-1 text-sm whitespace-nowrap overflow-hidden">
            ver. <CopyableText>0.9.0.0</CopyableText>
          </span>
        </Panel>
      </PanelGroup>
    </GlobalFocus.TabKeyJumpGroup>
  )
}

const SideMenuLink = ({ url, icon, children }: {
  url: string
  icon?: React.ElementType
  children?: React.ReactNode
}) => {
  const ref = useRef<HTMLAnchorElement>(null)
  const location = useLocation()
  const className = location.pathname.startsWith(url)
    ? 'outline-none inline-block w-full p-1 ellipsis-ex font-bold bg-white'
    : 'outline-none inline-block w-full p-1 ellipsis-ex'
  return (
    <NavLink ref={ref} to={url} className={className} {...GlobalFocus.useFocusTarget(ref)}>
      {React.createElement(icon ?? CircleStackIcon, { className: 'inline w-4 mr-1 opacity-70 align-middle' })}
      <span className="text-sm align-middle select-none">{children}</span>
    </NavLink>
  )
}

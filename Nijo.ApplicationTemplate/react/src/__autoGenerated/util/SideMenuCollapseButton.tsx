import React from "react"
import { Bars3Icon } from "@heroicons/react/24/solid"
import { IconButton } from "../input/IconButton"

export type SideMenuContextType = {
  toggle: () => void
  setCollapsed: (collapsed: boolean) => void
}
export const SideMenuContext = React.createContext<SideMenuContextType>({
  toggle: () => { },
  setCollapsed: () => { },
})

/** サイドメニュー開閉ボタン */
export const SideMenuCollapseButton = ({ className }: {
  className?: string
}) => {
  const { toggle } = React.useContext(SideMenuContext)

  return (
    <IconButton
      onClick={toggle}
      hideText
      icon={Bars3Icon}
      className={`p-1 ${className}`}
    >
      サイドメニューを開閉する
    </IconButton>
  )
}

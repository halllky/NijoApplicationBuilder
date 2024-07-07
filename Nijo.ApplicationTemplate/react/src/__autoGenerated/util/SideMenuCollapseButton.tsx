import { useCallback } from "react"
import { ChevronLeftIcon, ChevronRightIcon } from "@heroicons/react/24/solid"
import { IconButton } from "../input/IconButton"
import { defineContext } from "."

const [SideMenuContextProvider, useSideMenuContext] = defineContext(() => ({
  collapsed: false,
}), state => ({
  toggle: () => ({ collapsed: !state.collapsed }),
}))

/** サイドメニュー開閉ボタン */
const SideMenuCollapseButton = ({ className }: {
  className?: string
}) => {
  const [{ collapsed }, dispatch] = useSideMenuContext()
  const toggle = useCallback(() => {
    dispatch(state => state.toggle())
  }, [dispatch])

  return (
    <IconButton
      onClick={toggle}
      hideText
      icon={collapsed ? ChevronRightIcon : ChevronLeftIcon}
      className={`p-1 ${className}`}
    >
      サイドメニューを開閉する
    </IconButton>
  )
}

export {
  SideMenuContextProvider,
  SideMenuCollapseButton,
  useSideMenuContext,
}

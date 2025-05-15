import React from "react"
import useEvent from "react-use-event-hook"

/** 折り畳みができるUIの折りたたみ状態など、切り替え（トグル）を扱います。 */
export const useToggle = (initialState?: boolean) => {
  const [opened, setOpened] = React.useState(initialState ?? false)
  const toggle = useEvent(() => {
    setOpened(!opened)
  })
  return { opened, setOpened, toggle }
}
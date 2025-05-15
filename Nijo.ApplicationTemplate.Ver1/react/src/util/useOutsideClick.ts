import React from "react"

/** コンポーネントの外側のクリックの検知 */
export const useOutsideClick = (ref: React.RefObject<HTMLElement | null>, onOutsideClick: () => void, deps: React.DependencyList) => {
  const handleOutsideClick = React.useCallback(onOutsideClick, deps)

  React.useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (!ref.current) return
      if (ref.current.contains(e.target as HTMLElement)) return
      handleOutsideClick()
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [ref, handleOutsideClick])
}

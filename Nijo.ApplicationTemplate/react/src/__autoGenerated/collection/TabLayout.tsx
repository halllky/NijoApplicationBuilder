import React, { useCallback, useContext, useLayoutEffect, useMemo, useState } from 'react'
import { IconButton } from '../input/IconButton'
import { XMarkIcon } from '@heroicons/react/24/solid'

/** この中にBarやPanelを配置してください。 */
const Container = ({ onActiveTabChanged, className, children }: {
  onActiveTabChanged?: (tabKey: TabKey | undefined) => void
  className?: string
  children?: React.ReactNode
}) => {

  // アクティブタブ
  const [activeTabKey, setActiveTabKey] = useState<TabKey | undefined>()
  const handleTabKeyChanged = useCallback((value: TabKey | undefined) => {
    setActiveTabKey(value)
    onActiveTabChanged?.(value)
  }, [setActiveTabKey, onActiveTabChanged])

  // コンテキスト
  const contextValue = useMemo((): TabContextState => ({
    activeTabKey,
    setActiveTabKey: handleTabKeyChanged,
  }), [activeTabKey, handleTabKeyChanged])

  // 初期表示時に自動選択する
  useLayoutEffect(() => {
    if (activeTabKey === undefined) {
      // 標準的な実装の場合、Containerの直下にはBarが1個、Panelが複数個あるはず。
      // PanelのpropsにはtabKeyが指定されているはずなので、最初に見つかったtabKeyを選択する。
      if (Array.isArray(children)) {
        for (const child of children.flat(1)) {
          const panel = child as { props?: { tabKey?: TabKey } }
          if (panel.props?.tabKey !== undefined) {
            handleTabKeyChanged(panel.props.tabKey)
            break
          }
        }
      }
    }
  }, [])

  return (
    <TabContext.Provider value={contextValue}>
      <div className={`flex flex-col ${className ?? ''}`}>
        {children}
      </div>
    </TabContext.Provider>
  )
}

/** 複数のTabコンポーネントの親として設定してください。 */
const Bar = ({ atStart, atEnd, children }: {
  /** タブの前に挿入されるコンポーネント */
  atStart?: React.ReactNode
  /** タブの後ろに挿入されるコンポーネント */
  atEnd?: React.ReactNode
  children?: React.ReactNode
}) => {

  return (
    <div className="flex flex-wrap gap-x-1 gap-y-px pt-1 bg-color-gutter">
      {atStart}
      {children}
      {atEnd && <>
        <div className="flex-1"></div>
        {atEnd}
      </>}
    </div>
  )
}

/** Barの子要素として設定してください。 */
const Tab = ({ tabKey, onClose, label }: {
  /** どのPanelを開閉するかを表す一意なキー */
  tabKey: TabKey
  onClose?: () => void
  label?: string
}) => {
  const { activeTabKey, setActiveTabKey } = useContext(TabContext)

  const activate = useCallback(() => {
    setActiveTabKey(tabKey)
  }, [tabKey, setActiveTabKey])

  const className = tabKey === activeTabKey
    ? `basis-32 min-w-0 flex items-center px-1 bg-color-0 border-t-2 select-none border-color-8`
    : `basis-32 min-w-0 flex items-center px-1 bg-color-0 border-t-2 select-none border-color-0 opacity-40`
  const buttonClassName = tabKey === activeTabKey
    ? `flex-1 text-start text-color-9 whitespace-nowrap overflow-hidden text-ellipsis font-bold`
    : `flex-1 text-start text-color-9 whitespace-nowrap overflow-hidden text-ellipsis`

  return (
    <div title={label} className={className}>
      <button type="button" onClick={activate} className={buttonClassName}>
        {label}
      </button>
      {onClose !== undefined && tabKey === activeTabKey && (
        <IconButton icon={XMarkIcon} onClick={onClose} tabIndex={-1} />
      )}
    </div>
  )
}

/** Barの外側に配置してください。 */
const Panel = ({ tabKey, className, children }: {
  /** どのTabで開閉されるかを表す一意なキー */
  tabKey: TabKey
  className?: string
  children?: React.ReactNode
}) => {
  const { activeTabKey } = useContext(TabContext)

  return (
    <div className={`${tabKey !== activeTabKey ? 'hidden' : ''} ${className ?? ''}`}>
      {children}
    </div>
  )
}

// -----------------------------------
// 内部使用コンテキスト等

export type TabKey = string | number
type TabContextState = {
  activeTabKey: TabKey | undefined
  setActiveTabKey: (value: TabKey | undefined) => void
}
const TabContext = React.createContext<TabContextState>({
  activeTabKey: undefined,
  setActiveTabKey: () => { },
})

// -----------------------------------

export const TabLayout = {
  Container,
  Bar,
  Tab,
  Panel,
}

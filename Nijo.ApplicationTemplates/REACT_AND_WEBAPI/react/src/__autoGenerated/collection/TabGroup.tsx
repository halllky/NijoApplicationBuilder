import React, { useState, useMemo, useEffect, useRef, useCallback } from "react"
import { forwardRefEx } from "../util"

type TabGroupArgs<T> = {
  items: T[]
  keySelector: (item: T) => string
  nameSelector?: (item: T) => string
  onCreate?: () => void
  onRemove?: () => void
  children?: (args: { item: T, index: number }) => React.ReactNode
}

export const TabGroup = <T,>({
  items,
  keySelector,
  nameSelector,
  onCreate,
  onRemove,
  children,
}: TabGroupArgs<T>) => {
  const [selectedItemKey, setSelectedItemKey] = useState<string | undefined>(undefined)
  const selectedItem = useMemo(() => {
    return items.find(item => keySelector(item) === selectedItemKey)
  }, [items, selectedItemKey, keySelector])

  // タブの自動選択
  useEffect(() => {
    // 何も選択されていないとき
    if (!selectedItemKey && items.length > 0) {
      setSelectedItemKey(keySelector(items[0]))
    }
    // 選択中のタブが消えたとき
    if (selectedItemKey && !items.some(item => keySelector(item) === selectedItemKey)) {
      if (items.length === 0) {
        setSelectedItemKey(undefined)
      } else {
        setSelectedItemKey(keySelector(items[0]))
      }
    }
  }, [items, keySelector, selectedItemKey])

  // ref
  const refs = useRef<React.RefObject<HTMLLIElement>[]>([])
  for (let i = 0; i < items.length; i++) {
    refs.current[i] = React.createRef()
  }
  refs.current[items.length] = React.createRef()

  return (
    <div className="flex flex-col cursor-pointer">

      {/* タブパネルのボタン */}
      <ul className="w-full flex flex-wrap gap-1">
        {items.map((item, index) => (
          <TabButton
            key={index}
            ref={refs.current[index]}
            active={keySelector(item) === selectedItemKey}
            onActivate={() => setSelectedItemKey(keySelector(item))}
            onMoveNext={() => index < items.length && refs.current[index + 1].current?.focus()}
            onMovePrev={() => index > 0 && refs.current[index - 1].current?.focus()}
          >
            {nameSelector?.(item) ?? (index + 1)}
          </TabButton>
        ))}
        <TabButton addButton
          ref={refs.current[items.length]}
          onActivate={() => onCreate?.()}
          onMovePrev={() => items.length > 0 && refs.current[items.length - 1].current?.focus()}
        >
          +追加
        </TabButton>
      </ul>

      {/* タブのコンテンツ（選択中以外はhidden） */}
      {
        items.map((item, index) => (
          <div key={index} className={`relative flex-1 p-1 border ${BORDER_COLOR} bg-color-base ${item !== selectedItem && 'hidden'}`}>
            {children?.({ item, index })}
          </div>
        ))
      }
    </div >
  )
}

const TabButton = forwardRefEx<HTMLLIElement, {
  children?: React.ReactNode
  active?: boolean
  addButton?: boolean
  onActivate?: () => void
  onMoveNext?: () => void
  onMovePrev?: () => void
}>(({ addButton, children, onActivate, onMoveNext, onMovePrev, active }, ref) => {
  const onKeyDown: React.KeyboardEventHandler<HTMLLIElement> = useCallback(e => {
    if (e.key === ' ' || e.key === 'Enter') {
      onActivate?.()
      e.preventDefault()
    } else if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
      onMovePrev?.()
      e.preventDefault()
    } else if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
      onMoveNext?.()
      e.preventDefault()
    }
  }, [onActivate, onMoveNext, onMovePrev])

  const className = addButton
    ? `select-none inline-flex justify-center items-center px-1`
    : `select-none inline-flex justify-center items-center px-1 min-w-[48px]
      mb-[-1px] border-t border-l border-r
      ${active && `bg-color-base ${BORDER_COLOR} z-[1]`}
      ${!active && 'bg-color-3 border-color-3 text-color-5'}`

  return (
    <li
      ref={ref}
      className={className}
      onClick={onActivate}
      onKeyDown={onKeyDown}
      tabIndex={0}
    >
      {children}
    </li>
  )
})

const BORDER_COLOR = 'border-color-7'

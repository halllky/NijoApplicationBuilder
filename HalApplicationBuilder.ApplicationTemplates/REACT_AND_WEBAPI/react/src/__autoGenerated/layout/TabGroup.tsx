import React, { useState, useMemo, useEffect, useRef, useCallback } from "react"
import { forwardRefEx } from "../util"

export const TabGroup = <T,>(props: {
  items: T[]
  keySelector: (item: T) => string
  nameSelector?: (item: T) => string
  onCreate?: () => void
  onRemove?: () => void
  children?: (args: { item: T, index: number }) => React.ReactNode
}) => {
  const [selectedItemKey, setSelectedItemKey] = useState<string | undefined>(undefined)
  const selectedItem = useMemo(() => {
    return props.items.find(item => props.keySelector(item) === selectedItemKey)
  }, [props.items, selectedItemKey])

  // タブの自動選択
  useEffect(() => {
    // 何も選択されていないとき
    if (!selectedItemKey && props.items.length > 0) {
      setSelectedItemKey(props.keySelector(props.items[0]))
    }
    // 選択中のタブが消えたとき
    if (selectedItemKey && !props.items.some(item => props.keySelector(item) === selectedItemKey)) {
      if (props.items.length === 0) {
        setSelectedItemKey(undefined)
      } else {
        setSelectedItemKey(props.keySelector(props.items[0]))
      }
    }
  }, [props.items])

  // ref
  const refs = useRef<React.RefObject<HTMLLIElement>[]>([])
  for (let i = 0; i < props.items.length; i++) {
    refs.current[i] = React.createRef()
  }
  refs.current[props.items.length] = React.createRef()

  return (
    <div className="flex flex-col cursor-pointer">

      {/* タブパネルのボタン */}
      <ul className="w-full flex flex-wrap gap-1">
        {props.items.map((item, index) => (
          <TabButton
            key={index}
            ref={refs.current[index]}
            active={props.keySelector(item) === selectedItemKey}
            onActivate={() => setSelectedItemKey(props.keySelector(item))}
            onMoveNext={() => index < props.items.length && refs.current[index + 1].current?.focus()}
            onMovePrev={() => index > 0 && refs.current[index - 1].current?.focus()}
          >
            {props.nameSelector?.(item) ?? (index + 1)}
          </TabButton>
        ))}
        <TabButton addButton
          ref={refs.current[props.items.length]}
          onActivate={() => props.onCreate?.()}
          onMovePrev={() => props.items.length > 0 && refs.current[props.items.length - 1].current?.focus()}
        >
          +追加
        </TabButton>
      </ul>

      {/* タブのコンテンツ（選択中以外はhidden） */}
      {
        props.items.map((item, index) => (
          <div key={index} className={`flex-1 p-1 border ${BORDER_COLOR} bg-color-base ${item !== selectedItem && 'hidden'}`}>
            {props.children?.({ item, index })}
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
  }, [onActivate])

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

import useEvent from "react-use-event-hook"
import { DraggableWindowProps } from "./types"
import React from "react"

/**
 * ドラッグで位置を変更できるウィンドウ
 */
export default function DraggableWindow({
  layout,
  children,
  onMove,
  onResize,
  className
}: DraggableWindowProps) {

  // ResizeObserverを使用してリサイズを検知
  const resizeObserverRef = React.useRef<ResizeObserver | null>(null)
  const [isInitialized, setIsInitialized] = React.useState(false) // 初期化により react hook form の isDirty が true になるのを防ぐ
  const observerCallback = useEvent((entries: ResizeObserverEntry[]) => {
    if (!onResize) return
    for (const entry of entries) {
      const borderBoxSize = entry.borderBoxSize[0]
      if (!borderBoxSize) continue

      if (isInitialized) {
        onResize(borderBoxSize.inlineSize, borderBoxSize.blockSize)
      } else {
        setIsInitialized(true)
      }
    }
  })
  const divRefCallback = React.useCallback((div: HTMLDivElement | null) => {
    // 初回のみResizeObserverを作成
    if (!resizeObserverRef.current) {
      resizeObserverRef.current = new ResizeObserver(observerCallback)
    }

    // divとオブザーバーを接続する
    resizeObserverRef.current.disconnect()
    if (div) resizeObserverRef.current.observe(div)
  }, [resizeObserverRef, observerCallback])

  const handleMouseDown: React.MouseEventHandler<HTMLDivElement> = useEvent(e => {
    // スクロールエリアのパン操作が発生しないようにする
    e.stopPropagation()
  })

  const handleMouseDownInContents = useEvent((e: React.MouseEvent<SVGSVGElement>) => {
    e.preventDefault()
    e.stopPropagation()
    window.addEventListener("mousemove", onMove)
    window.addEventListener("mouseup", () => {
      window.removeEventListener("mousemove", onMove)
    })
  })

  return (
    <div
      ref={divRefCallback}
      className={`absolute z-0 resize overflow-auto cursor-auto ${className ?? ""}`}
      style={{
        left: layout.x,
        top: layout.y,
        width: layout.width,
        height: layout.height,
      }}
      onMouseDown={handleMouseDown}
    >
      {children({
        handleMouseDown: handleMouseDownInContents,
      })}
    </div>
  )
}

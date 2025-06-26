import React from "react"
import { DiagramViewProps, DiagramItem, DiagramItemLayout } from "./types"
import { usePanAndZoom } from "./usePanAndZoom"
import ZoomControls from "./ZoomControls"
import DraggableWindow from "./DraggableWindow"
import * as Icon from "@heroicons/react/24/outline"
import * as Input from "../../input"
import useEvent from "react-use-event-hook"

/**
 * ノードを自由に配置でき、パン操作やズームを行うことができるダイアグラムビュー
 */
export default function DiagramView<T extends DiagramItem>({
  items,
  onUpdateItem,
  onRemoveItem,
  renderItem,
  className,
  children,
}: DiagramViewProps<T>) {
  const {
    zoom,
    onZoomIn,
    onZoomOut,
    onResetZoom,
    panOffset,
    onResetPan,
    isDragging,
    scrollRef,
    canvasRef,
    handleMouseDown,
  } = usePanAndZoom()

  // ドラッグ開始時のオフセットを保存するためのRef
  const dragOffsetRef = React.useRef<{ x: number; y: number } | null>(null)

  // ドラッグ開始時の処理
  const handleItemDragStart = useEvent((itemIndex: number, e: React.MouseEvent) => {
    const item = items[itemIndex]
    const containerRect = scrollRef.current?.getBoundingClientRect()
    if (!containerRect) return

    // コンテナ内の相対座標を計算
    const relativeX = e.clientX - containerRect.left
    const relativeY = e.clientY - containerRect.top

    // 要素の現在位置（ズームとパンを考慮）
    const elementX = (item.layout.x + panOffset.x) * zoom
    const elementY = (item.layout.y + panOffset.y) * zoom

    // ドラッグ開始時の要素内でのオフセットを記録
    dragOffsetRef.current = {
      x: relativeX - elementX,
      y: relativeY - elementY,
    }
  })

  // アイテムの移動処理
  const handleItemMove = useEvent((itemIndex: number, e: MouseEvent) => {
    const item = items[itemIndex]
    const dragOffset = dragOffsetRef.current

    // DiagramViewコンテナの位置を取得
    const containerRect = scrollRef.current?.getBoundingClientRect()
    if (!containerRect || !dragOffset) return

    // コンテナ内の相対座標を計算
    const relativeX = e.clientX - containerRect.left
    const relativeY = e.clientY - containerRect.top

    // ドラッグオフセットを考慮した要素の位置を計算
    const newLayout: DiagramItemLayout = {
      ...item.layout,
      x: (relativeX - dragOffset.x) / zoom - panOffset.x,
      y: (relativeY - dragOffset.y) / zoom - panOffset.y,
    }
    onUpdateItem(itemIndex, { ...item, layout: newLayout } as T)
  })

  // アイテムのリサイズ処理
  const handleItemResize = useEvent((itemIndex: number, width: number, height: number) => {
    const item = items[itemIndex]
    const newLayout: DiagramItemLayout = {
      ...item.layout,
      width,
      height,
    }
    onUpdateItem(itemIndex, { ...item, layout: newLayout } as T)
  })

  return (
    <div
      className={`relative flex flex-col overflow-hidden outline-none ${className ?? ""}`}
      tabIndex={0} // キーボード操作を可能にする
    >
      {/* スクロールエリア */}
      <div
        ref={scrollRef}
        className="flex-1 relative overflow-hidden bg-white select-none"
        style={{
          zoom,
          cursor: isDragging ? 'grabbing' : 'grab',
        }}
        onMouseDown={handleMouseDown}
      >
        {/* キャンバス領域 */}
        <div
          ref={canvasRef}
          className="relative w-[150vw] h-[150vh] min-w-[150vw] min-h-[150vh]"
          style={{
            transform: `translate(${panOffset.x}px, ${panOffset.y}px)`,
          }}
        >
          {/* アイテムをレンダリング */}
          {items.map((item, index) => (
            <DraggableWindow
              key={item.id}
              layout={item.layout}
              onMove={(e: MouseEvent) => handleItemMove(index, e)}
              onResize={(width: number, height: number) => handleItemResize(index, width, height)}
            >
              {({ handleMouseDown }) =>
                renderItem(item, index, {
                  onUpdateLayout: (layout: DiagramItemLayout) => {
                    onUpdateItem(index, { ...item, layout } as T)
                  },
                  onRemove: () => onRemoveItem(index),
                  zoom,
                  handleMouseDown: (e: React.MouseEvent) => {
                    handleItemDragStart(index, e)
                    handleMouseDown(e)
                  },
                })
              }
            </DraggableWindow>
          ))}
        </div>
      </div>

      {/* カスタムコンテンツ（追加ボタンなど） */}
      {children}

      {/* ズームコントロール */}
      <div className="flex flex-col absolute bottom-4 right-4">
        <ZoomControls
          zoom={zoom}
          onZoomIn={onZoomIn}
          onZoomOut={onZoomOut}
          onResetZoom={onResetZoom}
        />
        <div className="basis-1"></div>
        <Input.IconButton icon={Icon.ArrowPathIcon} onClick={onResetPan} fill>
          位置リセット
        </Input.IconButton>
      </div>
    </div>
  )
}

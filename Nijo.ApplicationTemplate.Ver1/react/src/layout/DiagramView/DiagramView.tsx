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

    // アイテムの移動処理
    const createMoveHandler = useEvent((itemIndex: number) => {
        return (e: MouseEvent) => {
            const item = items[itemIndex]
            const newLayout: DiagramItemLayout = {
                ...item.layout,
                x: e.clientX / zoom - panOffset.x,
                y: e.clientY / zoom - panOffset.y,
            }
            onUpdateItem(itemIndex, { ...item, layout: newLayout } as T)
        }
    })

    // アイテムのリサイズ処理
    const createResizeHandler = useEvent((itemIndex: number) => {
        return (width: number, height: number) => {
            const item = items[itemIndex]
            const newLayout: DiagramItemLayout = {
                ...item.layout,
                width,
                height,
            }
            onUpdateItem(itemIndex, { ...item, layout: newLayout } as T)
        }
    })

    return (
        <div
            className={`relative flex flex-col overflow-hidden resize outline-none ${className ?? ""}`}
            tabIndex={0} // キーボード操作を可能にする
        >
            {/* スクロールエリア */}
            <div
                ref={scrollRef}
                className="flex-1 relative overflow-hidden bg-white border border-gray-500 select-none"
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
                            onMove={createMoveHandler(index)}
                            onResize={createResizeHandler(index)}
                        >
                            {({ DragHandle, handleMouseDown }) =>
                                renderItem(item, index, {
                                    onUpdateLayout: (layout: DiagramItemLayout) => {
                                        onUpdateItem(index, { ...item, layout } as T)
                                    },
                                    onRemove: () => onRemoveItem(index),
                                    zoom,
                                    DragHandle,
                                    handleMouseDown,
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

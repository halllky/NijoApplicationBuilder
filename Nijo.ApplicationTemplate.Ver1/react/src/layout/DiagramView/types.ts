import React from "react"

/** ダイアグラム内のアイテムのレイアウト情報 */
export interface DiagramItemLayout {
  x: number
  y: number
  width: number
  height: number
}

/** ダイアグラム内のアイテムの基本情報 */
export interface DiagramItem {
  id: string
  layout: DiagramItemLayout
}

/** ズーム操作のプロパティ */
export interface ZoomProps {
  zoom: number
  onZoomIn: () => void
  onZoomOut: () => void
  onResetZoom: () => void
}

/** パン操作のプロパティ */
export interface PanProps {
  panOffset: { x: number; y: number }
  onResetPan: () => void
}

/** ドラッグ可能ウィンドウのプロパティ */
export interface DraggableWindowProps {
  layout: DiagramItemLayout
  onMove: (e: MouseEvent) => void
  onResize?: (width: number, height: number) => void
  children: (props: {
    handleMouseDown: React.MouseEventHandler<Element>
  }) => React.ReactNode
  className?: string
}

/** ダイアグラムビューのプロパティ */
export interface DiagramViewProps<T extends DiagramItem> {
  items: T[]
  onUpdateItem: (index: number, item: T) => void
  onRemoveItem: (index: number) => void
  renderItem: (item: T, index: number, props: {
    onUpdateLayout: (layout: DiagramItemLayout) => void
    onRemove: () => void
    zoom: number
    handleMouseDown: React.MouseEventHandler<Element>
  }) => React.ReactNode
  className?: string
  children?: React.ReactNode
}

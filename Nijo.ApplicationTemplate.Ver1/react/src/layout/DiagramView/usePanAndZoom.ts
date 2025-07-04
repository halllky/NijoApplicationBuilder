import React from "react"
import useEvent from "react-use-event-hook"

export interface PanAndZoomState {
  zoom: number
  panOffset: { x: number; y: number }
}

/**
 * パンとズーム機能を提供するカスタムフック
 */
export function usePanAndZoom(
  initialState: PanAndZoomState | undefined,
  onStateChangePropValue: ((state: PanAndZoomState) => void) | undefined
) {
  // ズーム（0.1 ～ 1.0）
  const [zoom, setZoom] = React.useState(initialState?.zoom ?? 1)
  const handleZoomOut = useEvent(() => {
    setZoom(prev => Math.max(0.1, prev - 0.1))
  })
  const handleZoomIn = useEvent(() => {
    setZoom(prev => Math.min(1, prev + 0.1))
  })
  const handleResetZoom = useEvent(() => {
    setZoom(1)
  })

  // パン操作（背景ドラッグでの移動）
  const [panOffset, setPanOffset] = React.useState(initialState?.panOffset ?? { x: 0, y: 0 })
  const [isDragging, setIsDragging] = React.useState(false)
  const [dragStart, setDragStart] = React.useState({ x: 0, y: 0 })
  const [dragStartOffset, setDragStartOffset] = React.useState({ x: 0, y: 0 })

  const scrollRef = React.useRef<HTMLDivElement>(null)
  const canvasRef = React.useRef<HTMLDivElement>(null)

  const handleMouseDown = useEvent((e: React.MouseEvent<HTMLDivElement>) => {
    const target = e.target as HTMLElement
    if (target === canvasRef.current || target === scrollRef.current) {
      setIsDragging(true)
      setDragStart({ x: e.clientX, y: e.clientY })
      setDragStartOffset({ ...panOffset })
      e.preventDefault()
    }
  })

  // パン操作開始
  React.useEffect(() => {
    if (!isDragging) return

    const handleGlobalMouseMove = (e: MouseEvent) => {
      const deltaX = Math.round((e.clientX - dragStart.x) / zoom)
      const deltaY = Math.round((e.clientY - dragStart.y) / zoom)
      setPanOffset({
        x: Math.round(dragStartOffset.x + deltaX),
        y: Math.round(dragStartOffset.y + deltaY),
      })
    }

    // パン操作終了
    const handleGlobalMouseUp = () => {
      setIsDragging(false)
    }

    document.addEventListener('mousemove', handleGlobalMouseMove)
    document.addEventListener('mouseup', handleGlobalMouseUp)

    return () => {
      document.removeEventListener('mousemove', handleGlobalMouseMove)
      document.removeEventListener('mouseup', handleGlobalMouseUp)
    }
  }, [isDragging, dragStart.x, dragStart.y, dragStartOffset.x, dragStartOffset.y, zoom])

  const handleResetPan = useEvent(() => {
    setPanOffset({ x: 0, y: 0 })
  })

  // パンとズーム状態が変更されたときにコールバックを呼び出す
  const [currentStateInitialized, setCurrentStateInitialized] = React.useState(false)
  React.useEffect(() => {
    // 最初の1回は初期化による発火なので無視
    if (!currentStateInitialized) {
      setCurrentStateInitialized(true)
      return
    }
    onStateChangePropValue?.({
      zoom,
      panOffset: { ...panOffset },
    })
  }, [zoom, panOffset.x, panOffset.y]) // onStateChangePropValue, currentStateInitialized は意図的に依存配列に入れていない

  return {
    // ズーム関連
    zoom,
    onZoomIn: handleZoomIn,
    onZoomOut: handleZoomOut,
    onResetZoom: handleResetZoom,

    // パン関連
    panOffset,
    onResetPan: handleResetPan,
    isDragging,

    // DOM参照とイベントハンドラ
    scrollRef,
    canvasRef,
    handleMouseDown,
  }
}

import React from "react"

/**
 * つまみ要素の pointerdown を起点に、要素を画面上の任意の位置へドラッグ移動できるようにする。
 * 返り値の `handlePointerDown` をつまみ要素の onPointerDown に渡して使う。
 *
 * ドラッグ対象は常に画面内に収まるよう、位置は `elementRef` の実測サイズをもとに
 * ビューポート内へクランプされる（画面外に出て操作不能になるのを防ぐ）。
 */
export function useDraggablePosition(initialPosition: { x: number, y: number }, elementRef: React.RefObject<HTMLElement | null>) {
  const [position, setPosition] = React.useState(initialPosition)

  // ドラッグ開始時点の座標。レンダリングに影響しないため ref で保持する
  const dragOriginRef = React.useRef<{ pointerX: number, pointerY: number, positionX: number, positionY: number } | null>(null)

  const clampToViewport = React.useCallback((pos: { x: number, y: number }) => {
    const rect = elementRef.current?.getBoundingClientRect()
    const maxX = Math.max(window.innerWidth - (rect?.width ?? 0), 0)
    const maxY = Math.max(window.innerHeight - (rect?.height ?? 0), 0)
    return {
      x: Math.min(Math.max(pos.x, 0), maxX),
      y: Math.min(Math.max(pos.y, 0), maxY),
    }
  }, [elementRef])

  const handlePointerDown = (e: React.PointerEvent) => {
    // ポインタキャプチャにより、ドラッグ中にポインタが iframe 等の別要素上に移動しても
    // このハンドル要素（ひいては window の pointermove）へイベントが届き続ける
    e.currentTarget.setPointerCapture(e.pointerId)
    dragOriginRef.current = { pointerX: e.clientX, pointerY: e.clientY, positionX: position.x, positionY: position.y }
  }

  // ドラッグ中のポインタ移動は window 全体で追従する必要があるため、window のイベントと同期する
  React.useEffect(() => {
    const handlePointerMove = (e: PointerEvent) => {
      const origin = dragOriginRef.current
      if (!origin) return
      setPosition(clampToViewport({
        x: origin.positionX + (e.clientX - origin.pointerX),
        y: origin.positionY + (e.clientY - origin.pointerY),
      }))
    }
    const handlePointerUp = () => {
      dragOriginRef.current = null
    }
    window.addEventListener('pointermove', handlePointerMove)
    window.addEventListener('pointerup', handlePointerUp)
    return () => {
      window.removeEventListener('pointermove', handlePointerMove)
      window.removeEventListener('pointerup', handlePointerUp)
    }
  }, [clampToViewport])

  // ウィンドウのリサイズで要素が画面外にはみ出た場合も追従して戻す
  React.useEffect(() => {
    const handleResize = () => setPosition(p => clampToViewport(p))
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [clampToViewport])

  return { position, handlePointerDown }
}

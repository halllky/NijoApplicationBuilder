import React from "react"

/**
 * つまみ要素の pointerdown を起点に、要素を画面上の任意の位置へドラッグ移動できるようにする。
 * 返り値の `handlePointerDown` をつまみ要素の onPointerDown に渡して使う。
 */
export function useDraggablePosition(initialPosition: { x: number, y: number }) {
  const [position, setPosition] = React.useState(initialPosition)

  // ドラッグ開始時点の座標。レンダリングに影響しないため ref で保持する
  const dragOriginRef = React.useRef<{ pointerX: number, pointerY: number, positionX: number, positionY: number } | null>(null)

  const handlePointerDown = (e: React.PointerEvent) => {
    dragOriginRef.current = { pointerX: e.clientX, pointerY: e.clientY, positionX: position.x, positionY: position.y }
  }

  // ドラッグ中のポインタ移動は window 全体で追従する必要があるため、window のイベントと同期する
  React.useEffect(() => {
    const handlePointerMove = (e: PointerEvent) => {
      const origin = dragOriginRef.current
      if (!origin) return
      setPosition({
        x: origin.positionX + (e.clientX - origin.pointerX),
        y: origin.positionY + (e.clientY - origin.pointerY),
      })
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
  }, [])

  return { position, handlePointerDown }
}

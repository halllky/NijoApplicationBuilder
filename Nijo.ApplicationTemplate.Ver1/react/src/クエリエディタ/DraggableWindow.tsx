import * as Icon from "@heroicons/react/24/outline"
import useEvent from "react-use-event-hook"
import { EditorItemLayout } from "./types"

/**
 * ドラッグで位置を変更できるウィンドウ
 */
export default function DraggableWindow({ layout, children, onMove, className }: {
  layout: EditorItemLayout
  onMove: (e: MouseEvent) => void
  children: (props: {
    DragHandle: React.ReactNode
    handleMouseDown: React.MouseEventHandler<Element>
  }) => React.ReactNode
  className?: string
}) {
  const handleMouseDown = useEvent((e: React.MouseEvent<SVGSVGElement>) => {
    e.preventDefault()
    e.stopPropagation()
    window.addEventListener("mousemove", onMove)
    window.addEventListener("mouseup", () => {
      window.removeEventListener("mousemove", onMove)
    })
  })

  return (
    <div
      className={`absolute z-0 flex flex-col resize overflow-hidden cursor-auto ${className ?? ""}`}
      style={{
        left: layout.x,
        top: layout.y,
        width: layout.width,
        height: layout.height,
      }}
    >
      {children({
        DragHandle: (
          <Icon.Bars3Icon
            className="mx-1 basis-4 min-w-4 h-8 text-sky-600 cursor-grab"
            onMouseDown={handleMouseDown}
          />
        ),
        handleMouseDown,
      })}
    </div>
  )
}

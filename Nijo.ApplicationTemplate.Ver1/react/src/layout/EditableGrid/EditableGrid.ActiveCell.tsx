import React from "react";
import { GetPixelFunction } from "./EditableGrid.CellEditor";
import { CellSelectionRange } from "./types";

export const ActiveCell = ({ selectedRange, getPixel }: {
  selectedRange: CellSelectionRange | null
  getPixel: GetPixelFunction
}) => {
  const containerRef = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    if (selectedRange && containerRef.current) {
      const left = getPixel({ position: 'left', colIndex: selectedRange.startCol })
      const right = getPixel({ position: 'right', colIndex: selectedRange.endCol })
      const top = getPixel({ position: 'top', rowIndex: selectedRange.startRow })
      const bottom = getPixel({ position: 'bottom', rowIndex: selectedRange.endRow })
      containerRef.current.style.left = `${left}px`
      containerRef.current.style.top = `${top}px`
      containerRef.current.style.minWidth = `${right - left}px`
      containerRef.current.style.minHeight = `${bottom - top}px`
    }
  }, [selectedRange, getPixel])

  return (
    <div ref={containerRef} className="absolute border-1 border-sky-500 bg-sky-200/25 pointer-events-none">
    </div>
  )
}
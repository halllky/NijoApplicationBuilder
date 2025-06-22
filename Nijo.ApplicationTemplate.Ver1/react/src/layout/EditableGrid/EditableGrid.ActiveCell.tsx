import React from "react";
import { GetPixelFunction } from "./EditableGrid.CellEditor";
import { CellPosition, CellSelectionRange } from "./types";

export const ActiveCell = ({ anchorCellRef, selectedRange, getPixel, isFocused }: {
  anchorCellRef: React.RefObject<CellPosition | null>
  selectedRange: CellSelectionRange | null
  getPixel: GetPixelFunction
  isFocused: boolean
}) => {
  const containerRef = React.useRef<HTMLDivElement>(null)
  const leftRef = React.useRef<HTMLDivElement>(null)
  const rightRef = React.useRef<HTMLDivElement>(null)
  const aboveRef = React.useRef<HTMLDivElement>(null)
  const belowRef = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    if (selectedRange && containerRef.current && isFocused) {
      const left = getPixel({ position: 'left', colIndex: selectedRange.startCol })
      const right = getPixel({ position: 'right', colIndex: selectedRange.endCol })
      const top = getPixel({ position: 'top', rowIndex: selectedRange.startRow })
      const bottom = getPixel({ position: 'bottom', rowIndex: selectedRange.endRow })
      containerRef.current.style.left = `${left}px`
      containerRef.current.style.top = `${top}px`
      containerRef.current.style.minWidth = `${right - left}px`
      containerRef.current.style.minHeight = `${bottom - top}px`

      if (leftRef.current && rightRef.current && aboveRef.current && belowRef.current) {
        if (anchorCellRef.current) {
          const activeTop = getPixel({ position: 'top', rowIndex: anchorCellRef.current.rowIndex })
          const activeBottom = getPixel({ position: 'bottom', rowIndex: anchorCellRef.current.rowIndex })
          const activeLeft = getPixel({ position: 'left', colIndex: anchorCellRef.current.colIndex })
          const activeRight = getPixel({ position: 'right', colIndex: anchorCellRef.current.colIndex })

          leftRef.current.style.left = `${left}px`
          leftRef.current.style.top = `${activeTop}px`
          leftRef.current.style.width = `${activeLeft - left}px`
          leftRef.current.style.height = `${activeBottom - activeTop}px`

          rightRef.current.style.left = `${activeRight}px`
          rightRef.current.style.top = `${activeTop}px`
          rightRef.current.style.width = `${right - activeRight}px`
          rightRef.current.style.height = `${activeBottom - activeTop}px`

          aboveRef.current.style.left = `${left}px`
          aboveRef.current.style.top = `${top}px`
          aboveRef.current.style.width = `${right - left}px`
          aboveRef.current.style.height = `${activeTop - top}px`

          belowRef.current.style.left = `${left}px`
          belowRef.current.style.top = `${activeBottom}px`
          belowRef.current.style.width = `${right - left}px`
          belowRef.current.style.height = `${bottom - activeBottom}px`
        } else {
          leftRef.current.style.width = '0px'
          leftRef.current.style.height = '0px'
          rightRef.current.style.width = '0px'
          rightRef.current.style.height = '0px'
          aboveRef.current.style.width = '0px'
          aboveRef.current.style.height = '0px'
          belowRef.current.style.width = '0px'
          belowRef.current.style.height = '0px'
        }
      }
    }
  }, [selectedRange, getPixel, isFocused])

  // フォーカスが当たっていない場合は何も表示しない
  if (!isFocused) {
    return null;
  }

  return (
    <>
      {/* 選択範囲全体の外枠 */}
      <div ref={containerRef} className="absolute border-1 border-sky-500 pointer-events-none"></div>

      {/* アクティブセルの位置だけ背景色なし、それ以外の選択セルは背景色あり、とするため、4つのdivでアクティブセル以外の部分を覆う */}
      <div ref={leftRef} className="absolute bg-sky-200/25 mix-blend-multiply pointer-events-none" />
      <div ref={rightRef} className="absolute bg-sky-200/25 mix-blend-multiply pointer-events-none" />
      <div ref={aboveRef} className="absolute bg-sky-200/25 mix-blend-multiply pointer-events-none" />
      <div ref={belowRef} className="absolute bg-sky-200/25 mix-blend-multiply pointer-events-none" />
    </>
  )
}

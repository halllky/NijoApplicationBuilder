import { useCallback, useId, useRef, useState } from "react"
import { TabKeyJumpGroup, useAppContext, useFocusTarget, useGlobalFocusContext } from "../hooks"
import { AgGridReact } from "ag-grid-react"
import { CellFocusedEvent, ColDef } from "ag-grid-community"

export const AgGridWrapper = <T,>({ rowData, columnDefs, gridRef: argsGridRef, className }: {
  rowData?: T[]
  columnDefs?: ColDef<T>[]
  gridRef?: React.MutableRefObject<AgGridReact<T> | null>
  className?: string
}) => {
  const [{ darkMode }] = useAppContext()

  const gridRef = useRef<AgGridReact<T>>(null)
  if (argsGridRef) argsGridRef.current = gridRef.current

  // フォーカス制御
  const gridWrapperTabId = useId()
  const gridWrapperRef = useRef<HTMLDivElement>(null)
  const gridWrapperFocusMethods = useFocusTarget(gridWrapperRef, { tabId: gridWrapperTabId, borderHidden: true })
  const [, dispatchGlobalFocus] = useGlobalFocusContext()

  // 最後にフォーカスが当たっていたセルを記憶する
  const [lastFocused, setLastFocused] = useState<{ row: number, col: string } | null>(null)
  const onCellFocused = useCallback((e: CellFocusedEvent<T>) => {
    setLastFocused(e.rowIndex === null || e.column === null ? null : {
      row: e.rowIndex,
      col: typeof e.column === 'string' ? e.column : e.column.getId(),
    })
  }, [])

  // Tabキーでフォーカスされたとき、まずdivにフォーカスがあたるので、ag-gridにフォーカスを移す
  const onGridWrapperFocused = useCallback((e: React.FocusEvent<HTMLDivElement>) => {
    if (rowData && rowData.length > 0) {
      // ag-grid自体にフォーカスが当たっていないとsetFocusedCellが効かないので
      const cell = e.target.querySelector('.ag-cell') as HTMLElement | null
      if (!cell) return
      cell.focus()

      // 最後にフォーカスしていたセルをアクティブにする
      if (lastFocused) {
        gridRef.current?.api.setFocusedCell(lastFocused.row, lastFocused.col)
      } else if (columnDefs && columnDefs.length > 0 && columnDefs[0].colId) {
        gridRef.current?.api.setFocusedCell(0, columnDefs[0].colId)
      }
    } else {
      // セルがないときは次のエリアへ
      dispatchGlobalFocus({ type: 'move-to-next-tab' })
    }
  }, [rowData, columnDefs, lastFocused])


  return (
    <TabKeyJumpGroup id={gridWrapperTabId}>
      <div className={`ag-theme-alpine compact ${(darkMode ? 'dark' : '')} ${className}`}
        ref={gridWrapperRef} {...gridWrapperFocusMethods} tabIndex={0} onFocus={onGridWrapperFocused}>
        <AgGridReact
          ref={gridRef}
          rowData={rowData || []}
          columnDefs={columnDefs}
          multiSortKey="ctrl"
          rowSelection="multiple"
          undoRedoCellEditing
          undoRedoCellEditingLimit={20}
          onCellFocused={onCellFocused}>
        </AgGridReact>
      </div>
    </TabKeyJumpGroup>
  )
}

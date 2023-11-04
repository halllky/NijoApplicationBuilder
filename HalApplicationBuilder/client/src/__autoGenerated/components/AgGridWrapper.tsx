import { useRef } from "react"
import { useAppContext } from "../hooks/AppContext"
import { AgGridReact } from "ag-grid-react"
import { ColDef } from "ag-grid-community"

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

  return (
    <div className={`ag-theme-alpine compact ${(darkMode ? 'dark' : '')} ${className}`} tabIndex={0}>
      <AgGridReact
        ref={gridRef}
        rowData={rowData || []}
        columnDefs={columnDefs}
        multiSortKey="ctrl"
        rowSelection="multiple"
        undoRedoCellEditing
        undoRedoCellEditingLimit={20}>
      </AgGridReact>
    </div>
  )
}

import * as Layout from '../layout'
import { EditableGrid } from '../layout/EditableGrid'
import type { CellValueEditedEvent, EditableGridRef, GetColumnDefsFunction } from '../layout/EditableGrid/index.d'
import { type ColumnDefFactories } from '../layout/EditableGrid/useCellTypes'
import { useRef, useState, useCallback } from 'react'

interface MyRowData {
  id: number
  make: string
  model: string
  price: number
}

export function EditableGridExample() {
  const gridRef = useRef<EditableGridRef<MyRowData>>(null)
  const [rowData, setRowData] = useState<MyRowData[]>(() => [
    { id: 1, make: "Toyota", model: "Celica", price: 35000 },
    { id: 2, make: "Ford", model: "Mondeo", price: 32000 },
    { id: 3, make: "Porsche", model: "Boxster", price: 72000 },
  ])
  const [rowSelection, setRowSelection] = useState<Record<string, boolean>>({})

  const getColumnDefs: GetColumnDefsFunction<MyRowData> = useCallback((cellType: ColumnDefFactories<MyRowData>) => [
    cellType.text('make', 'メーカー', { defaultWidth: '150' }),
    cellType.text('model', 'モデル', { defaultWidth: '150' }),
    cellType.number('price', '価格', { defaultWidth: '100' }),
  ], [])

  const handleChangeCell: CellValueEditedEvent<MyRowData> = useCallback(e => {
    setRowData(prev => {
      const clone = [...prev]
      clone[e.rowIndex] = e.newRow
      return clone
    })
  }, [])

  return (
    <Layout.PageFrame
      headerContent={(
        <Layout.PageFrameTitle>
          編集可能グリッド (EditableGrid)
        </Layout.PageFrameTitle>
      )}
    >
      <div style={{ height: 400 }}>
        <EditableGrid<MyRowData>
          ref={gridRef}
          rows={rowData}
          getColumnDefs={getColumnDefs}
          onCellEdited={handleChangeCell}
          showCheckBox
          rowSelection={rowSelection}
          onRowSelectionChange={setRowSelection}
        />
      </div>
    </Layout.PageFrame>
  )
}

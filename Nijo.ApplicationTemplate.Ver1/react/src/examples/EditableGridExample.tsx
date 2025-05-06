import * as Layout from '../layout'
import { EditableGrid } from '../layout/EditableGrid'
import type { EditableGridRef, GetColumnDefsFunction } from '../layout/EditableGrid/index.d'
import { useCellTypes, type ColumnDefFactories } from '../layout/EditableGrid/useCellTypes'
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

    const cellTypes = useCellTypes<MyRowData>()

    const getColumnDefs: GetColumnDefsFunction<MyRowData> = useCallback((cellType: ColumnDefFactories<MyRowData>) => [
        cellType.text(
            'make' as const,
            'メーカー',
            { header: 'メーカー', defaultWidth: '150' }
        ),
        cellType.text(
            'model' as const,
            'モデル',
            { header: 'モデル', defaultWidth: '150' }
        ),
        cellType.number(
            'price' as const,
            '価格',
            { header: '価格', defaultWidth: '100' }
        ),
    ], [])

    const handleChangeCell = useCallback(
        (rowIndex: number, fieldPath: string, newValue: any) => {
            setRowData((prev) =>
                prev.map((row, i) => {
                    if (i !== rowIndex) return row
                    return { ...row, [fieldPath]: newValue }
                })
            )
        },
        []
    )

    return (
        <Layout.PageFrame>
            <h1>編集可能グリッド (EditableGrid)</h1>
            <div style={{ height: 400 }}>
                <EditableGrid<MyRowData>
                    ref={gridRef}
                    rows={rowData}
                    getColumnDefs={getColumnDefs}
                    onChangeCell={handleChangeCell}
                    showCheckBox
                    rowSelection={rowSelection}
                    onRowSelectionChange={setRowSelection}
                />
            </div>
        </Layout.PageFrame>
    )
}

import * as React from 'react'
import useEvent from 'react-use-event-hook'
import * as ReactHookForm from 'react-hook-form'
import * as Layout from '../../layout'
import * as Input from '../../input'
import { EditableGrid } from '../../layout/EditableGrid'
import type { CellValueEditedEvent, EditableGridRef, GetColumnDefsFunction } from '../../layout/EditableGrid/index.d'
import { type ColumnDefFactories } from '../../layout/EditableGrid/useCellTypes'

/** 画面全体のフォームの型 */
type MyFormData = {
  rows: MyRowData[]
}
/** グリッドの行の型 */
type MyRowData = {
  id: number
  make: string
  model: string
  price: number
}

export function EditableGridExample() {

  return (
    <Layout.PageFrame
      headerContent={(
        <Layout.PageFrameTitle>
          編集可能グリッド (EditableGrid)
        </Layout.PageFrameTitle>
      )}
      className="flex flex-col gap-4 px-8 py-4"
    >
      <p className="whitespace-pre-wrap">
        編集可能なグリッドです。<br />
        オプションの指定により読み取り専用とすることもできます。<br />
        基本的に、React Hook Form の useFieldArray と組み合わせて使用します。<br />
        行・列ともに仮想化されており、画面内に表示されている範囲のみがレンダリングされます。<br />
        TanStack の React Table および TanStack Virtual を使用しています。
      </p>

      <BasicGridExample />

    </Layout.PageFrame>
  )
}

/** 基本的なグリッドの実装例 */
const BasicGridExample = () => {

  // 基本的に useFieldArray と組み合わせて使用する。
  const { control } = ReactHookForm.useForm<MyFormData>({ defaultValues: getDefaultValue() })
  const { fields, update, append, remove } = ReactHookForm.useFieldArray({ name: 'rows', control })

  const [rowSelection, setRowSelection] = React.useState<Record<string, boolean>>({})

  // 画面側からグリッド内部の状態を参照ないし操作する場合はrefを使用する。
  const gridRef = React.useRef<EditableGridRef<MyRowData>>(null)

  // 列定義
  const getColumnDefs: GetColumnDefsFunction<MyRowData> = React.useCallback((cellType: ColumnDefFactories<MyRowData>) => [
    cellType.text('make', 'メーカー', { defaultWidth: '150' }),
    cellType.text('model', 'モデル', { defaultWidth: '150' }),
    cellType.number('price', '価格', { defaultWidth: '100' }),
  ], [])

  // セル変更時イベント
  const handleChangeCell: CellValueEditedEvent<MyRowData> = useEvent(e => {
    update(e.rowIndex, e.newRow)
  })

  // 行追加
  const handleAddRow = useEvent(() => {
    append({ id: fields.length + 1, make: '', model: '', price: 0 })
  })

  // 行削除
  const handleDeleteRow = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows().map(r => r.rowIndex) ?? []
    remove(selectedRows)
  })

  return (
    <div className="flex flex-col flex-wrap gap-1">
      <div className="flex gap-2">
        <Input.IconButton outline onClick={handleAddRow}>行追加</Input.IconButton>
        <Input.IconButton outline onClick={handleDeleteRow}>行削除</Input.IconButton>
      </div>
      <EditableGrid<MyRowData>
        ref={gridRef}
        rows={fields}
        getColumnDefs={getColumnDefs}
        onCellEdited={handleChangeCell}
        showCheckBox
        rowSelection={rowSelection}
        onRowSelectionChange={setRowSelection}
        className="h-[160px] w-full"
      />
    </div>
  )
}

const getDefaultValue = (): MyFormData => ({
  rows: [
    { id: 1, make: "Toyota", model: "Celica", price: 35000 },
    { id: 2, make: "Ford", model: "Mondeo", price: 32000 },
    { id: 3, make: "Porsche", model: "Boxster", price: 72000 },
  ]
})

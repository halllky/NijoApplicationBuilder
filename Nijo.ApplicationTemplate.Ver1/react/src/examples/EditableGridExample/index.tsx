import * as React from 'react'
import useEvent from 'react-use-event-hook'
import * as ReactHookForm from 'react-hook-form'
import * as Layout from '../../layout'
import * as Input from '../../input'
import { EditableGrid } from '../../layout/EditableGrid'
import type { CellValueEditedEvent, EditableGridRef, GetColumnDefsFunction } from '../../layout/EditableGrid/types'
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
  description1?: string
  description2?: string
  description3?: string
  description4?: string
  description5?: string
  description6?: string
  description7?: string
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
        行は仮想化されており、画面内に表示されている範囲のみがレンダリングされます。<br />
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

  // 少ない列を表示 or とても多い列を表示
  const [columnCountType, setColumnCountType] = React.useState<'less' | 'many'>('many')

  // 列定義
  const getColumnDefs: GetColumnDefsFunction<MyRowData> = React.useCallback((cellType: ColumnDefFactories<MyRowData>) => [
    cellType.text('make', 'メーカー', { defaultWidth: 150, isFixed: true }),
    cellType.text('model', 'モデル', { defaultWidth: 150 }),

    cellType.number('price', '価格', {
      defaultWidth: 100,
      // レンダリング処理のカスタマイズの例。ここでは、価格が負の数なら赤字で表示する。
      renderCell: (cell) => {
        const price = cell.getValue() as number
        return price < 0 ? <span className="text-rose-600">{price}</span> : price
      },
    }),

    cellType.text('description1', '説明1', { defaultWidth: 100, invisible: columnCountType !== 'many' }),
    cellType.text('description2', '説明2', { defaultWidth: 100, invisible: columnCountType !== 'many' }),
    cellType.text('description3', '説明3', { defaultWidth: 100, invisible: columnCountType !== 'many' }),
    cellType.text('description4', '説明4', { defaultWidth: 100, invisible: columnCountType !== 'many' }),
    cellType.text('description5', '説明5', { defaultWidth: 100, invisible: columnCountType !== 'many' }),
    cellType.text('description6', '説明6', { defaultWidth: 100, invisible: columnCountType !== 'many' }),
    cellType.text('description7', '説明7', { defaultWidth: 100, invisible: columnCountType !== 'many' }),
  ], [columnCountType])

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

  // 1000行追加
  const handleAdd1000Rows = useEvent(() => {
    append(Array.from({ length: 1000 }, (_, i) => ({ id: fields.length + i + 1, make: '', model: '', price: i })))
  })

  // コンソール出力
  const handleConsoleLog = useEvent(() => {
    console.table(fields)
  })

  return (
    <div className="flex flex-col flex-wrap gap-1">
      <div className="flex gap-2">
        <Input.IconButton outline onClick={handleAddRow}>行追加</Input.IconButton>
        <Input.IconButton outline onClick={handleDeleteRow}>行削除</Input.IconButton>
        <div className="basis-2"></div>
        <Input.IconButton outline onClick={handleAdd1000Rows}>1000行追加</Input.IconButton>
        <div className="flex-1"></div>
        <Input.IconButton outline onClick={handleConsoleLog}>コンソール出力</Input.IconButton>
      </div>
      <EditableGrid<MyRowData>
        ref={gridRef}
        rows={fields}
        getColumnDefs={getColumnDefs}
        onCellEdited={handleChangeCell}
        showCheckBox
        rowSelection={rowSelection}
        onRowSelectionChange={setRowSelection}
        className="h-[240px] w-full resize-y border border-gray-300"
      />
      <div className="flex items-center gap-4">
        <span className="text-sm text-gray-500">
          列定義の動的切り替え:
        </span>
        <label className="flex items-center gap-1">
          <input type="radio" name="columnCountType" value="less" checked={columnCountType === 'less'} onChange={() => setColumnCountType('less')} />
          一部の列だけ表示
        </label>
        <label className="flex items-center gap-1">
          <input type="radio" name="columnCountType" value="many" checked={columnCountType === 'many'} onChange={() => setColumnCountType('many')} />
          すべての列を表示
        </label>
      </div>
    </div>
  )
}

const getDefaultValue = (): MyFormData => ({
  rows: [
    { id: 1, make: "Toyota", model: "Celica", price: 35000 },
    { id: 2, make: "Ford", model: "Mondeo", price: 32000 },
    { id: 3, make: "Porsche", model: "Boxster", price: 72000 },
    { id: 4, make: "Toyota", model: "Celica", price: 35000 },
    { id: 5, make: "Ford", model: "Mondeo", price: -32000 },
    { id: 6, make: "Porsche", model: "Boxster", price: 72000 },
    { id: 7, make: "Toyota", model: "Celica", price: 35000 },
    { id: 8, make: "Ford", model: "Mondeo", price: 32000 },
    { id: 9, make: "Porsche", model: "Boxster", price: 72000 },
    { id: 10, make: "Toyota", model: "Celica", price: 35000 },
    { id: 11, make: "Ford", model: "Mondeo", price: 32000 },
    { id: 12, make: "Porsche", model: "Boxster", price: 72000 },
  ]
})

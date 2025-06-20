import * as React from "react"
import * as Input from "../../input"
import * as Layout from "../../layout"
import { VForm3 } from "../../layout"
import { 商品マスタDisplayData, 商品マスタSearchCondition } from "../../__autoGenerated/商品マスタ"
import { カテゴリマスタRefTarget } from "../../__autoGenerated/カテゴリマスタ"
import { 仕入先マスタRefTarget } from "../../__autoGenerated/仕入先マスタ"
import { useNavigate } from "react-router-dom"
import { Control, FieldValues } from "react-hook-form"

export const 商品マスタ一覧検索画面 = () => {
  const navigate = useNavigate()

  /** 一覧表示のカラム定義 */
  const getColumnDefs: Layout.GetColumnDefsFunction<商品マスタDisplayData> = React.useCallback(cellType => [
    cellType.text("values.ID", "商品ID"),
    cellType.text("values.商品名", "商品名"),
    cellType.number("values.価格", "価格"),
    cellType.text("values.カテゴリ.カテゴリ名", "カテゴリ"),
    cellType.text("values.仕入先.仕入先名", "仕入先"),
  ], [])

  return (
    <Layout.MultiView
      queryModel="商品マスタ"
      isReady={true}
      title="商品マスタ一覧検索"
      getColumnDefs={getColumnDefs}
      headerButtons={
        <button
          onClick={() => navigate('/商品マスタ/new')}
          className="px-3 py-1 bg-green-500 text-white rounded hover:bg-green-600"
        >
          新規登録
        </button>
      }
    >
      {({ reactHookFormMethods: { control } }) => (
        <VForm3.Root labelWidth="8rem">
          <VForm3.BreakPoint>
            <VForm3.Item label="商品ID">
              <Input.Word name="filter.ID" control={control} />
            </VForm3.Item>
            <VForm3.Item label="商品名">
              <Input.Word name="filter.商品名" control={control} />
            </VForm3.Item>
            <VForm3.Item label="価格">
              <Input.NumberInput name="filter.価格.from" control={control} />
              ～
              <Input.NumberInput name="filter.価格.to" control={control} />
            </VForm3.Item>
          </VForm3.BreakPoint>
          <VForm3.BreakPoint label="カテゴリ">
            <VForm3.Item label="カテゴリID">
              <Input.Word name="filter.カテゴリ.カテゴリID" control={control} />
            </VForm3.Item>
            <VForm3.Item label="カテゴリ名">
              <Input.Word name="filter.カテゴリ.カテゴリ名" control={control} />
            </VForm3.Item>
          </VForm3.BreakPoint>
          <VForm3.BreakPoint label="仕入先">
            <VForm3.Item label="仕入先ID">
              <Input.Word name="filter.仕入先.仕入先ID" control={control} />
            </VForm3.Item>
            <VForm3.Item label="仕入先名">
              <Input.Word name="filter.仕入先.仕入先名" control={control} />
            </VForm3.Item>
          </VForm3.BreakPoint>
          {/* TODO: ネストした要素の検索条件 */}
        </VForm3.Root>
      )}
    </Layout.MultiView>
  )
}

import * as React from "react"
import * as Input from "../../input"
import * as Layout from "../../layout"
import { VForm3 } from "../../layout"
import { 売上分析DisplayData, 売上分析SearchCondition } from "../../__autoGenerated/売上分析"
import { 店舗マスタRefTarget } from "../../__autoGenerated/店舗マスタ"
import { useNavigate } from "react-router-dom"

export const 売上分析一覧検索画面 = () => {
  const navigate = useNavigate()

  /** 一覧表示のカラム定義 */
  const getColumnDefs: Layout.GetColumnDefsFunction<売上分析DisplayData> = React.useCallback(cellType => [
    cellType.text("values.年月", "年月"),
    cellType.text("values.店舗.店舗名", "店舗"),
    cellType.number("values.売上合計", "売上合計"),
    cellType.number("values.客数", "客数"),
    cellType.number("values.客単価", "客単価"),
  ], [])

  return (
    <Layout.MultiView
      queryModel="売上分析"
      isReady={true}
      title="売上分析一覧検索"
      getColumnDefs={getColumnDefs}
      // 売上分析画面は通常、新規登録は行わないため、headerButtonsは空にします
      headerButtons={null}
    >
      {({ reactHookFormMethods: { control } }) => (
        <VForm3.Root labelWidth="8rem">
          <VForm3.BreakPoint>
            <VForm3.Item label="年月">
              <Input.DateInput name="filter.年月.from" yearMonth control={control} />
              ～
              <Input.DateInput name="filter.年月.to" yearMonth control={control} />
            </VForm3.Item>
          </VForm3.BreakPoint>
          <VForm3.BreakPoint label="店舗">
            <VForm3.Item label="店舗ID">
              <Input.Word name="filter.店舗.店舗ID" control={control} />
            </VForm3.Item>
            <VForm3.Item label="店舗名">
              <Input.Word name="filter.店舗.店舗名" control={control} />
            </VForm3.Item>
          </VForm3.BreakPoint>
          <VForm3.BreakPoint>
            <VForm3.Item label="売上合計">
              <Input.NumberInput name="filter.売上合計.from" control={control} />
              ～
              <Input.NumberInput name="filter.売上合計.to" control={control} />
            </VForm3.Item>
            <VForm3.Item label="客数">
              <Input.NumberInput name="filter.客数.from" control={control} />
              ～
              <Input.NumberInput name="filter.客数.to" control={control} />
            </VForm3.Item>
            <VForm3.Item label="客単価">
              <Input.NumberInput name="filter.客単価.from" control={control} />
              ～
              <Input.NumberInput name="filter.客単価.to" control={control} />
            </VForm3.Item>
          </VForm3.BreakPoint>
          {/* TODO: ネストした要素（カテゴリ別売上、時間帯別売上など）の検索条件 */}
        </VForm3.Root>
      )}
    </Layout.MultiView>
  )
}

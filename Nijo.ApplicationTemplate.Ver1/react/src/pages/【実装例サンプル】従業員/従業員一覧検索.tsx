// import * as React from "react"
// import * as Input from "../../input"
// import * as Layout from "../../layout"
// import { VForm3 } from "../../layout"
// import { 従業員DisplayData } from "../../__autoGenerated/従業員"
// import { useNavigate } from "react-router-dom"

// export const 従業員一覧検索 = () => {
//   const navigate = useNavigate()

//   /** 一覧表示のカラム定義 */
//   const getColumnDefs: Layout.GetColumnDefsFunction<従業員DisplayData> = React.useCallback(cellType => [
//     cellType.text("values.従業員ID", "従業員ID"),
//     cellType.text("values.氏名", "氏名"),
//     cellType.text("values.氏名カナ", "氏名カナ"),
//     cellType.date("values.退職日", "退職日"),
//   ], [])

//   return (
//     <Layout.MultiView
//       queryModel="従業員"
//       isReady={true}
//       title="従業員一覧検索"
//       getColumnDefs={getColumnDefs}
//       headerButtons={
//         <button
//           onClick={() => navigate('/従業員/new')}
//           className="px-3 py-1 bg-green-500 text-white rounded hover:bg-green-600"
//         >
//           新規登録
//         </button>
//       }
//     >
//       {({ reactHookFormMethods: { control } }) => (
//         <VForm3.Root labelWidth="8rem">
//           <VForm3.BreakPoint>
//             <VForm3.Item label="従業員ID">
//               <Input.Word name="filter.従業員ID" control={control} />
//             </VForm3.Item>
//             <VForm3.Item label="氏名">
//               <Input.Word name="filter.氏名" control={control} />
//             </VForm3.Item>
//             <VForm3.Item label="氏名カナ">
//               <Input.Word name="filter.氏名カナ" control={control} />
//             </VForm3.Item>
//           </VForm3.BreakPoint>
//           <VForm3.BreakPoint label="所属部署">
//             <VForm3.Item label="年度">
//               <Input.NumberInput name="filter.所属部署.年度.from" control={control} />
//               ～
//               <Input.NumberInput name="filter.所属部署.年度.to" control={control} />
//             </VForm3.Item>
//             <VForm3.Item label="部署コード">
//               <Input.Word name="filter.所属部署.部署.部署コード" control={control} />
//             </VForm3.Item>
//             <VForm3.Item label="部署名">
//               <Input.Word name="filter.所属部署.部署.部署名" control={control} />
//             </VForm3.Item>
//           </VForm3.BreakPoint>
//           <VForm3.BreakPoint>
//             <VForm3.Item label="退職日">
//               <Input.DateInput name="filter.退職日.from" control={control} />
//               ～
//               <Input.DateInput name="filter.退職日.to" control={control} />
//             </VForm3.Item>
//           </VForm3.BreakPoint>
//         </VForm3.Root>
//       )}
//     </Layout.MultiView>
//   )
// }

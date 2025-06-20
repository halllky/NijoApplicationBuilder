import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../input"
import * as Layout from "../../layout"
import * as AutoGenerated from "../../__autoGenerated/商品マスタ"
import * as カテゴリマスタAutoGenerated from "../../__autoGenerated/カテゴリマスタ"
import * as 仕入先マスタAutoGenerated from "../../__autoGenerated/仕入先マスタ"
import * as 倉庫マスタAutoGenerated from "../../__autoGenerated/倉庫マスタ"
import * as 従業員マスタAutoGenerated from "../../__autoGenerated/従業員マスタ"
import { useParams, useNavigate } from "react-router-dom"
// import { useDataSelectorDialog } from "../../parts/DataSelectorDialog" // 未実装

/**
 * 商品マスタデータ1件の詳細を閲覧・編集する画面。
 */
export const 商品マスタ詳細編集画面 = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isNew = id === "new"

  const [isLoading, setIsLoading] = React.useState<boolean>(true)
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null)

  const methods = ReactHookForm.useForm<AutoGenerated.商品マスタDisplayData>()
  const {
    control, handleSubmit, reset, setValue, watch, register,
    formState: { errors }
  } = methods

  // --- Row Selection States for Grids ---
  const [付属品RowSelection, set付属品RowSelection] = React.useState<Record<string, boolean>>({});
  const [在庫情報RowSelection, set在庫情報RowSelection] = React.useState<Record<string, boolean>>({});

  // --- Field Arrays --- (EditableGrid用)
  const { fields: 付属品Fields, update: update付属品, append: append付属品, remove: remove付属品 } =
    ReactHookForm.useFieldArray({ control, name: "商品詳細.付属品" });
  const { fields: 在庫情報Fields, update: update在庫情報, append: append在庫情報, remove: remove在庫情報 } =
    ReactHookForm.useFieldArray({ control, name: "在庫情報" });

  // 在庫情報内の在庫状況履歴のFieldArray (各在庫情報行ごとに必要になるが、一旦保留)

  // --- Data Selector Dialogs (未実装のためコメントアウト) ---
  // const { showDialog: showカテゴリDialog, dialogElement: カテゴリDialog } = useDataSelectorDialog(...);
  // const { showDialog: show仕入先Dialog, dialogElement: 仕入先Dialog } = useDataSelectorDialog(...);
  // const { showDialog: show倉庫Dialog, dialogElement: 倉庫Dialog } = useDataSelectorDialog(...);
  // const { showDialog: show担当者Dialog, dialogElement: 担当者Dialog } = useDataSelectorDialog(...);

  // --- Data Fetching ---
  React.useEffect(() => {
    const fetchData = async () => {
      setErrorMessage(null)
      setIsLoading(true);
      let initialData = AutoGenerated.createNew商品マスタDisplayData(); // 空データで初期化

      if (!isNew && id) {
        try {
          const response = await fetch(`/api/商品マスタ/${id}`)
          if (!response.ok) {
            if (response.status === 404) setErrorMessage("指定された商品が見つかりません。")
            else setErrorMessage(`データの取得に失敗しました。(ステータス: ${response.status})`)
          } else {
            initialData = await response.json();
          }
        } catch (error) {
          console.error("データ取得エラー:", error)
          setErrorMessage("データの取得中に予期せぬエラーが発生しました。")
        }
      }
      // ネストされたオブジェクト/配列の初期値保証
      if (!initialData.商品詳細) initialData.商品詳細 = AutoGenerated.createNew商品詳細DisplayData();
      if (!initialData.商品詳細.商品仕様) initialData.商品詳細.商品仕様 = AutoGenerated.createNew商品仕様DisplayData();
      if (!initialData.商品詳細.商品仕様.サイズ) initialData.商品詳細.商品仕様.サイズ = AutoGenerated.createNewサイズDisplayData();
      if (!initialData.商品詳細.付属品) initialData.商品詳細.付属品 = [];
      if (!initialData.在庫情報) initialData.在庫情報 = [];
      initialData.在庫情報.forEach(info => {
        if (!info.在庫状況履歴) info.在庫状況履歴 = [];
      });

      reset(initialData);
      setIsLoading(false);
    }
    fetchData()
  }, [id, isNew, reset])

  // --- Submit Handler ---
  const onSubmit = React.useCallback(async (data: AutoGenerated.商品マスタDisplayData) => {
    console.log("onSubmit called. id:", id);
    setErrorMessage(null)
    setIsLoading(true);
    try {
      // isNew の代わりに id === 'new' を直接使用する
      const currentIsNew = id === 'new';
      console.log("currentIsNew:", currentIsNew, "id for decision:", id);
      const method = currentIsNew ? "POST" : "PUT";
      const url = currentIsNew ? "/api/商品マスタ" : `/api/商品マスタ/${id}`;
      console.log("method:", method, "url:", url);

      // TODO: API仕様に合わせて送信データを整形 (RefのID化など)
      const submitData = data;
      const body = JSON.stringify(submitData);

      const response = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: body,
      })
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        const message = errorData.message || `保存に失敗しました。(ステータス: ${response.status})`
        setErrorMessage(message)
        console.error("保存エラー:", errorData)
        throw new Error(message)
      }
      navigate("/商品マスタ") // 仮に一覧画面のパスを /商品マスタ とする
    } catch (error) {
      console.error("保存処理エラー:", error)
      if (!errorMessage) setErrorMessage("保存中に予期せぬエラーが発生しました。")
    } finally {
      setIsLoading(false);
    }
  }, [id, navigate])

  // --- Editable Grid Refs and Column Defs (簡易版) ---
  const 付属品GridRef = React.useRef<Layout.EditableGridRef<ReactHookForm.FieldArrayWithId<AutoGenerated.商品マスタDisplayData, "商品詳細.付属品", "id">>>(null);
  const 在庫情報GridRef = React.useRef<Layout.EditableGridRef<ReactHookForm.FieldArrayWithId<AutoGenerated.商品マスタDisplayData, "在庫情報", "id">>>(null);

  const get付属品ColumnDefs: Layout.GetColumnDefsFunction<ReactHookForm.FieldArrayWithId<AutoGenerated.商品マスタDisplayData, "商品詳細.付属品", "id">> = React.useCallback(cellType => [
    cellType.text("values.付属品ID", "付属品ID", { editable: true, required: true }),
    cellType.text("values.付属品名", "付属品名", { editable: true }),
    cellType.number("values.数量", "数量", { editable: true }),
  ], []);

  const get在庫情報ColumnDefs: Layout.GetColumnDefsFunction<ReactHookForm.FieldArrayWithId<AutoGenerated.商品マスタDisplayData, "在庫情報", "id">> = React.useCallback(cellType => [
    // cellType.reference("values.倉庫", "倉庫", { fetchPath: "/api/倉庫マスタ", getDisplayValue: item => item.倉庫名, editable: true, required: true }), // Dialog未実装
    cellType.text("values.倉庫.ID", "倉庫ID", { editable: true, required: true }), // 代替：ID直接入力
    cellType.number("values.在庫数", "在庫数", { editable: true }),
    cellType.date("values.棚卸日時", "棚卸日時", { editable: true }),
    // 在庫状況履歴はさらにネストしたGridが必要になるため、ここでは省略
  ], []);

  return (
    <Layout.PageFrame
      headerContent={(
        <>
          <Input.IconButton fill onClick={(e) => { if (!isLoading) handleSubmit(onSubmit)(e); }}>保存</Input.IconButton>
          <Input.IconButton fill onClick={() => { if (!isLoading) navigate(-1); }}>キャンセル</Input.IconButton>
        </>
      )}
    >
      {isLoading ? (
        <div className="flex justify-center items-center h-64">読み込み中...</div>
      ) : errorMessage ? (
        <div className="m-4 p-4 border border-red-400 bg-red-100 text-red-700 rounded">
          <p>エラー: {errorMessage}</p>
          <button onClick={() => navigate(-1)} className="mt-2 px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-700">戻る</button>
        </div>
      ) : (
        <ReactHookForm.FormProvider {...methods}>
          <Layout.VForm3.Root>
            {/* --- 基本情報 --- */}
            <Layout.VForm3.BreakPoint>
              <Layout.VForm3.Item label="商品ID" required={AutoGenerated.商品マスタConstraints.values.ID.required}>
                <Input.Word name="values.ID" control={control} readOnly={false}
                  rules={{ required: "必須", maxLength: { value: 10, message: `10文字以内` } }} />
              </Layout.VForm3.Item>
              <Layout.VForm3.Item label="商品名" required={AutoGenerated.商品マスタConstraints.values.商品名.required}>
                <Input.Word name="values.商品名" control={control}
                  rules={{ required: "必須", maxLength: { value: 100, message: `100文字以内` } }} />
              </Layout.VForm3.Item>
              <Layout.VForm3.Item label="価格" required={AutoGenerated.商品マスタConstraints.values.価格.required}>
                <Input.NumberInput name="values.価格" control={control}
                  rules={{ required: "必須", min: { value: 0, message: "0以上" } }} />
              </Layout.VForm3.Item>
              <Layout.VForm3.Item label="カテゴリ" required={AutoGenerated.商品マスタConstraints.values.カテゴリ.required}>
                {/* DataSelectorDialog未実装のため、WordでID直接入力 */}
                <Input.Word name="values.カテゴリ.カテゴリID" control={control} rules={{ required: "必須" }} />
                {/* <Input.IconButton disabled>検索</Input.IconButton> */}
              </Layout.VForm3.Item>
              <Layout.VForm3.Item label="仕入先" required={AutoGenerated.商品マスタConstraints.values.仕入先.required}>
                {/* DataSelectorDialog未実装のため、WordでID直接入力 */}
                <Input.Word name="values.仕入先.仕入先ID" control={control} rules={{ required: "必須" }} />
                {/* <Input.IconButton disabled>検索</Input.IconButton> */}
              </Layout.VForm3.Item>
            </Layout.VForm3.BreakPoint>

            {/* --- 商品詳細 --- */}
            <div className="mt-4 p-4 border rounded">
              <label className="block font-bold mb-2">商品詳細</label>
              <Layout.VForm3.FullWidthItem label="説明文">
                <textarea {...register("商品詳細.values.説明文")} className="border border-gray-300 p-1 w-full" rows={3} />
              </Layout.VForm3.FullWidthItem>
              <div className="mt-4 p-4 border rounded">
                <label className="block font-bold mb-2">商品仕様</label>
                <Layout.VForm3.BreakPoint>
                  <Layout.VForm3.Item label="重量(g)">
                    <Input.NumberInput name="商品詳細.商品仕様.values.重量" control={control} rules={{ min: 0 }} />
                  </Layout.VForm3.Item>
                </Layout.VForm3.BreakPoint>
              </div>
              <div className="mt-4 p-4 border rounded">
                <label className="block font-bold mb-2">サイズ(mm)</label>
                <Layout.VForm3.BreakPoint>
                  <Layout.VForm3.Item label="幅">
                    <Input.NumberInput name="商品詳細.商品仕様.サイズ.values.幅" control={control} rules={{ min: 0 }} />
                  </Layout.VForm3.Item>
                  <Layout.VForm3.Item label="高さ">
                    <Input.NumberInput name="商品詳細.商品仕様.サイズ.values.高さ" control={control} rules={{ min: 0 }} />
                  </Layout.VForm3.Item>
                  <Layout.VForm3.Item label="奥行">
                    <Input.NumberInput name="商品詳細.商品仕様.サイズ.values.奥行" control={control} rules={{ min: 0 }} />
                  </Layout.VForm3.Item>
                </Layout.VForm3.BreakPoint>
              </div>

              <Layout.VForm3.FullWidthItem label={(
                <div className="flex flex-row items-center">
                  <div>付属品</div>
                  <div className="basis-4"></div>
                  <Input.IconButton fill onClick={() => append付属品(AutoGenerated.createNew付属品DisplayData())}>追加</Input.IconButton>
                  <Input.IconButton fill onClick={() => {
                    const selected = 付属品GridRef.current?.getCheckedRows();
                    if (selected) remove付属品(selected.map(r => r.rowIndex));
                  }}>削除</Input.IconButton>
                </div>
              )}>
                <Layout.EditableGrid
                  ref={付属品GridRef}
                  rows={付属品Fields}
                  getColumnDefs={get付属品ColumnDefs}
                  rowSelection={付属品RowSelection}
                  onRowSelectionChange={set付属品RowSelection}
                  onChangeRow={e => {
                    for (const x of e.changedRows) {
                      update付属品(x.rowIndex, x.newRow)
                    }
                  }}
                />
              </Layout.VForm3.FullWidthItem>
            </div>

            {/* --- 在庫情報 --- */}
            <Layout.VForm3.FullWidthItem label={(
              <div className="flex flex-row items-center">
                <div>在庫情報</div>
                <div className="basis-4"></div>
                <Input.IconButton fill onClick={() => append在庫情報(AutoGenerated.createNew在庫情報DisplayData())}>追加</Input.IconButton>
                <Input.IconButton fill onClick={() => {
                  const selected = 在庫情報GridRef.current?.getCheckedRows();
                  if (selected) remove在庫情報(selected.map(r => r.rowIndex));
                }}>削除</Input.IconButton>
              </div>
            )}>
              <Layout.EditableGrid
                ref={在庫情報GridRef}
                rows={在庫情報Fields}
                getColumnDefs={get在庫情報ColumnDefs}
                rowSelection={在庫情報RowSelection}
                onRowSelectionChange={set在庫情報RowSelection}
                onChangeRow={e => {
                  for (const x of e.changedRows) {
                    update在庫情報(x.rowIndex, x.newRow)
                  }
                }}
              />
            </Layout.VForm3.FullWidthItem>

          </Layout.VForm3.Root>
        </ReactHookForm.FormProvider>
      )}
      {/* ダイアログ要素 (未実装) */}
    </Layout.PageFrame>
  )
}

// --- 在庫状況履歴Grid (サブコンポーネント - 簡易版) ---
/*
interface 在庫状況履歴GridProps {
    parentRowIndex: number;
    control: ReactHookForm.Control<AutoGenerated.商品マスタDisplayData>;
}
const 在庫状況履歴Grid: React.FC<在庫状況履歴GridProps> = ({ parentRowIndex, control }) => {
    const { fields, append, remove } = ReactHookForm.useFieldArray({
        control,
        name: `在庫情報.${parentRowIndex}.在庫状況履歴`
    });
    const gridRef = React.useRef<Layout.EditableGridRef<any>>(null);

    const getColumnDefs: Layout.GetColumnDefsFunction<any> = React.useCallback(cellType => [
        cellType.text("values.履歴ID", "履歴ID", { editable: true, required: true }),
        cellType.date("values.変更日時", "変更日時", { editable: true, required: true }),
        cellType.number("values.変更前在庫数", "変更前", { editable: true }),
        cellType.number("values.変更後在庫数", "変更後", { editable: true }),
        // cellType.reference("values.担当者", "担当者", { fetchPath: "/api/従業員マスタ", getDisplayValue: item => item.氏名, editable: true, required: true }), // Dialog未実装
         cellType.text("values.担当者.従業員ID", "担当者ID", { editable: true, required: true }), // 代替
    ], []);

    return (
        <div className="p-4 border-t">
            <div className="flex flex-row items-center mb-2">
                <div>在庫状況履歴</div>
                <div className="basis-4"></div>
                <Input.IconButton fill onClick={() => append(AutoGenerated.createNew在庫状況履歴DisplayData())}>追加</Input.IconButton>
                <Input.IconButton fill onClick={() => {
                    const selected = gridRef.current?.getSelectedRows();
                    if (selected) remove(selected.map(r => r.rowIndex));
                }}>削除</Input.IconButton>
            </div>
             <Layout.EditableGrid
                ref={gridRef}
                rows={fields}
                getColumnDefs={getColumnDefs}
                onChangeCell={(rowIndex, fieldName, value) => {
                    setValue(`在庫情報.${parentRowIndex}.在庫状況履歴.${rowIndex}.${fieldName}`, value);
                }}
            />
        </div>
    );
};
*/

import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import * as Input from "../../input"
import * as Layout from "../../layout"
import * as AutoGenerated from "../../__autoGenerated/店舗マスタ"
import * as 従業員マスタAutoGenerated from "../../__autoGenerated/従業員マスタ"
import { useParams, useNavigate } from "react-router-dom"
import useEvent from "react-use-event-hook"
// import { useDataSelectorDialog } from "../../parts/DataSelectorDialog" // 未実装のためコメントアウト

/**
 * 店舗マスタデータ1件の詳細を閲覧・編集する画面。
 */
export const 店舗マスタ詳細編集画面 = () => {
    const { id } = useParams<{ id: string }>()
    const navigate = useNavigate()
    const isNew = id === "new"

    // 読み込み状態
    const [isLoading, setIsLoading] = React.useState<boolean>(true)
    // エラーメッセージ
    const [errorMessage, setErrorMessage] = React.useState<string | null>(null)

    const methods = ReactHookForm.useForm<AutoGenerated.店舗マスタDisplayData>()
    const { control, handleSubmit, reset, setValue, watch } = methods // setValue, watch を追加

    // 従業員選択ダイアログ (未実装のためコメントアウト)
    /*
    const { showDialog: show従業員Dialog, dialogElement: 従業員Dialog } = useDataSelectorDialog<従業員マスタAutoGenerated.従業員マスタRefTarget>({
        title: "従業員選択",
        getQueryParameter: 従業員マスタAutoGenerated.toQueryParameterOf従業員マスタSearchCondition,
        getDisplayValue: (item) => item.氏名 ?? '(名称未設定)', // 表示する値（例: 氏名）
        getColumns: (cell) => [ // ダイアログの列定義
            cell.text('従業員ID', '従業員ID', { search: true }),
            cell.text('氏名', '氏名', { search: true }),
            cell.text('氏名カナ', '氏名カナ', { search: true }),
        ],
        fetchPath: '/api/従業員マスタ',
        onSelect: (selected) => {
            if (selected) {
                setValue('values.店長', selected); // 選択された従業員データをフォームにセット
            }
        }
    });
    */

    // 店舗データの取得と初期値設定
    React.useEffect(() => {
        const fetchData = async () => {
            setErrorMessage(null)
            setIsLoading(true);
            let initialData = AutoGenerated.createNew店舗マスタDisplayData();

            if (!isNew && id) {
                try {
                    const response = await fetch(`/api/店舗マスタ/${id}`)
                    if (!response.ok) {
                        if (response.status === 404) setErrorMessage("指定された店舗が見つかりません。")
                        else setErrorMessage(`データの取得に失敗しました。(ステータス: ${response.status})`)
                    } else {
                        initialData = await response.json();
                    }
                } catch (error) {
                    console.error("データ取得エラー:", error)
                    setErrorMessage("データの取得中に予期せぬエラーが発生しました。")
                }
            }
            // ネストされたオブジェクトの初期値も確認・設定
            if (!initialData.店舗マスタの住所) {
                initialData.店舗マスタの住所 = AutoGenerated.createNew店舗マスタの住所DisplayData();
            }
            if (!initialData.営業時間) {
                initialData.営業時間 = AutoGenerated.createNew営業時間DisplayData();
            }
            reset(initialData);
            setIsLoading(false);
        }
        fetchData()
    }, [id, isNew, reset])

    // データ保存処理
    const onSubmit = useEvent(async (data: AutoGenerated.店舗マスタDisplayData) => {
        setErrorMessage(null)
        setIsLoading(true);
        try {
            const method = isNew ? "POST" : "PUT"
            const url = isNew ? "/api/店舗マスタ" : `/api/店舗マスタ/${id}`

            // 送信前に店長のIDのみに整形する（オブジェクト全体ではなくIDを期待する場合）
            // ※APIの仕様に合わせて調整が必要
            const submitData = {
                ...data,
                values: {
                    ...data.values,
                    店長: data.values.店長?.従業員ID ? { 従業員ID: data.values.店長.従業員ID } : undefined, // IDのみ送信する場合
                }
            }
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
            navigate("/店舗マスタ") // 仮に一覧画面のパスを /店舗マスタ とする
        } catch (error) {
            console.error("保存処理エラー:", error)
            if (!errorMessage) setErrorMessage("保存中に予期せぬエラーが発生しました。")
        } finally {
            setIsLoading(false);
        }
    })

    // watchで参照フィールドの値を取得 (ダイアログ未実装のため表示用)
    // const watched従業員 = watch('values.店長');

    return (
        <Layout.PageFrame
            headerContent={(
                <>
                    <Input.IconButton fill onClick={(e) => { if (!isLoading) handleSubmit(onSubmit)(e); }}>
                        保存
                    </Input.IconButton>
                    <Input.IconButton fill onClick={() => { if (!isLoading) navigate(-1); }}>
                        キャンセル
                    </Input.IconButton>
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
                        {/* 店舗基本情報 */}
                        <Layout.VForm3.BreakPoint>
                            <Layout.VForm3.Item label="店舗ID" required={AutoGenerated.店舗マスタConstraints.values.店舗ID.required}>
                                <Input.Word name="values.店舗ID" control={control} readOnly={!isNew}
                                    rules={{
                                        required: "店舗IDは必須です。",
                                        maxLength: { value: AutoGenerated.店舗マスタConstraints.values.店舗ID.maxLength!, message: `店舗IDは${AutoGenerated.店舗マスタConstraints.values.店舗ID.maxLength}文字以内` }
                                    }} />
                            </Layout.VForm3.Item>

                            <Layout.VForm3.Item label="店舗名" required={AutoGenerated.店舗マスタConstraints.values.店舗名.required}>
                                <Input.Word name="values.店舗名" control={control}
                                    rules={{
                                        required: AutoGenerated.店舗マスタConstraints.values.店舗名.required ? "店舗名は必須です。" : undefined,
                                        maxLength: { value: AutoGenerated.店舗マスタConstraints.values.店舗名.maxLength!, message: `店舗名は${AutoGenerated.店舗マスタConstraints.values.店舗名.maxLength}文字以内` }
                                    }} />
                            </Layout.VForm3.Item>

                            <Layout.VForm3.Item label="電話番号" required={AutoGenerated.店舗マスタConstraints.values.電話番号.required}>
                                <Input.Word name="values.電話番号" control={control}
                                    rules={{
                                        required: AutoGenerated.店舗マスタConstraints.values.電話番号.required ? "電話番号は必須です。" : undefined,
                                        maxLength: { value: AutoGenerated.店舗マスタConstraints.values.電話番号.maxLength!, message: `電話番号は${AutoGenerated.店舗マスタConstraints.values.電話番号.maxLength}文字以内` },
                                        // pattern: { value: /^[0-9-]+$/, message: "電話番号の形式が正しくありません。" } // 必要に応じてパターン追加
                                    }} />
                            </Layout.VForm3.Item>
                        </Layout.VForm3.BreakPoint>

                        {/* 住所 */}
                        <div className="mt-4 p-4 border rounded">
                            <label className="block font-bold mb-2">住所</label>
                            <Layout.VForm3.BreakPoint>
                                <Layout.VForm3.Item label="郵便番号" required={AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.郵便番号.required}>
                                    <Input.Word name="店舗マスタの住所.values.郵便番号" control={control}
                                        rules={{
                                            required: AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.郵便番号.required ? "郵便番号は必須です。" : undefined,
                                            maxLength: { value: AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.郵便番号.maxLength!, message: `郵便番号は${AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.郵便番号.maxLength}文字以内` },
                                            // pattern: { value: /^[0-9]{3}-?[0-9]{4}$/, message: "郵便番号の形式（例: 123-4567）が正しくありません。" } // 必要に応じてパターン追加
                                        }} />
                                </Layout.VForm3.Item>

                                <Layout.VForm3.Item label="都道府県" required={AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.都道府県.required}>
                                    <Input.Word name="店舗マスタの住所.values.都道府県" control={control}
                                        rules={{
                                            required: AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.都道府県.required ? "都道府県は必須です。" : undefined,
                                            maxLength: { value: AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.都道府県.maxLength!, message: `都道府県は${AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.都道府県.maxLength}文字以内` }
                                        }} />
                                </Layout.VForm3.Item>

                                <Layout.VForm3.Item label="市区町村" required={AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.市区町村.required}>
                                    <Input.Word name="店舗マスタの住所.values.市区町村" control={control}
                                        rules={{
                                            required: AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.市区町村.required ? "市区町村は必須です。" : undefined,
                                            maxLength: { value: AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.市区町村.maxLength!, message: `市区町村は${AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.市区町村.maxLength}文字以内` }
                                        }} />
                                </Layout.VForm3.Item>

                                <Layout.VForm3.Item label="番地・建物名" required={AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.番地建物名.required}>
                                    <Input.Word name="店舗マスタの住所.values.番地建物名" control={control}
                                        rules={{
                                            required: AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.番地建物名.required ? "番地・建物名は必須です。" : undefined,
                                            maxLength: { value: AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.番地建物名.maxLength!, message: `番地・建物名は${AutoGenerated.店舗マスタConstraints.店舗マスタの住所.values.番地建物名.maxLength}文字以内` }
                                        }} />
                                </Layout.VForm3.Item>
                            </Layout.VForm3.BreakPoint>
                        </div>

                        {/* 営業時間 */}
                        <div className="mt-4 p-4 border rounded">
                            <label className="block font-bold mb-2">営業時間</label>
                            <Layout.VForm3.BreakPoint>
                                <Layout.VForm3.Item label="開店時間" required={AutoGenerated.店舗マスタConstraints.営業時間.values.開店時間.required}>
                                    {/* TimeInputがないためWordで代用 */}
                                    <Input.Word name="営業時間.values.開店時間" control={control}
                                        rules={{
                                            required: AutoGenerated.店舗マスタConstraints.営業時間.values.開店時間.required ? "開店時間は必須です。" : undefined,
                                            maxLength: { value: AutoGenerated.店舗マスタConstraints.営業時間.values.開店時間.maxLength!, message: `開店時間は${AutoGenerated.店舗マスタConstraints.営業時間.values.開店時間.maxLength}文字以内` },
                                            pattern: { value: /^([01]?[0-9]|2[0-3]):[0-5][0-9]$/, message: "HH:MM形式で入力してください" }
                                        }} />
                                </Layout.VForm3.Item>

                                <Layout.VForm3.Item label="閉店時間" required={AutoGenerated.店舗マスタConstraints.営業時間.values.閉店時間.required}>
                                    {/* TimeInputがないためWordで代用 */}
                                    <Input.Word name="営業時間.values.閉店時間" control={control}
                                        rules={{
                                            required: AutoGenerated.店舗マスタConstraints.営業時間.values.閉店時間.required ? "閉店時間は必須です。" : undefined,
                                            maxLength: { value: AutoGenerated.店舗マスタConstraints.営業時間.values.閉店時間.maxLength!, message: `閉店時間は${AutoGenerated.店舗マスタConstraints.営業時間.values.閉店時間.maxLength}文字以内` },
                                            pattern: { value: /^([01]?[0-9]|2[0-3]):[0-5][0-9]$/, message: "HH:MM形式で入力してください" }
                                        }} />
                                </Layout.VForm3.Item>
                            </Layout.VForm3.BreakPoint>
                        </div>

                        {/* 店長 */}
                        <div className="mt-4 p-4 border rounded">
                            <label className="block font-bold mb-2">店長</label>
                            <Layout.VForm3.BreakPoint>
                                <Layout.VForm3.Item label="店長 (従業員ID)" required={AutoGenerated.店舗マスタConstraints.values.店長.required}>
                                    {/* DataSelectorDialog未実装のため、WordでID直接入力 */}
                                    <Input.Word name="values.店長.従業員ID" control={control}
                                        rules={{ required: AutoGenerated.店舗マスタConstraints.values.店長.required ? "店長は必須です。" : undefined }}
                                    />
                                    {/* <Input.IconButton onClick={show従業員Dialog} disabled>検索</Input.IconButton> */}
                                    {/* 選択された従業員の名前などを表示する領域を追加してもよい */}
                                    {/* <span>{watched従業員?.氏名 ?? ''}</span> */}
                                </Layout.VForm3.Item>
                            </Layout.VForm3.BreakPoint>
                        </div>

                    </Layout.VForm3.Root>
                </ReactHookForm.FormProvider>
            )
            }
            {/* ダイアログ要素をレンダリング (未実装のためコメントアウト) */}
            {/* {従業員Dialog} */}
        </Layout.PageFrame>
    )
}

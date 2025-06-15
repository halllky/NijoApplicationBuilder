import * as Layout from '../layout'
import * as Input from '../input'
import { useForm, FormProvider } from 'react-hook-form'

interface MyFormData {
    name: string
    age: number
}

export function VFormExample() {
    const methods = useForm<MyFormData>()
    const { control, handleSubmit } = methods

    const onSubmit = (data: MyFormData) => {
        console.log(data)
        alert(`送信データ: ${JSON.stringify(data)}`)
    }

    return (
        <Layout.PageFrame>
            <h1>フォームレイアウト (VForm3)</h1>
            <FormProvider {...methods}>
                <form onSubmit={handleSubmit(onSubmit)}>
                    <Layout.VForm3.Root labelWidth="8rem">
                        <Layout.VForm3.BreakPoint label="基本情報">
                            <Layout.VForm3.Item label="名前" required>
                                <Input.Word control={control} name="name" />
                            </Layout.VForm3.Item>
                            <Layout.VForm3.Item label="年齢">
                                <Input.NumberInput control={control} name="age" />
                            </Layout.VForm3.Item>
                        </Layout.VForm3.BreakPoint>
                        <div className="mt-4">
                            <button type="submit" className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600">
                                送信
                            </button>
                        </div>
                    </Layout.VForm3.Root>
                </form>
            </FormProvider>
        </Layout.PageFrame>
    )
}

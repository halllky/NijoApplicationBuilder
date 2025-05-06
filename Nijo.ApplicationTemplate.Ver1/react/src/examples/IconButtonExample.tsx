import * as Input from '../input'
import * as Layout from '../layout'

export function IconButtonExample() {
    return (
        <Layout.PageFrame>
            <h1>アイコンボタン (IconButton)</h1>
            <Input.IconButton onClick={() => alert('Clicked!')}>クリック</Input.IconButton>
        </Layout.PageFrame>
    )
}

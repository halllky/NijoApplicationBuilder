import * as Input from '../input'
import * as Layout from '../layout'

export function HyperLinkExample() {
    return (
        <Layout.PageFrame>
            <h1>ハイパーリンク (HyperLink)</h1>
            <Input.HyperLink to="/">ホームへ</Input.HyperLink>
        </Layout.PageFrame>
    )
}

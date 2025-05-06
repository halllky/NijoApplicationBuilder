import * as Layout from '../layout'

export function PageFrameExample() {
    return (
        <Layout.PageFrame headerContent={<h2>ページフレームヘッダー</h2>}>
            <h1>ページフレーム (PageFrame)</h1>
            <p>これがページフレームのコンテンツ部分です。</p>
        </Layout.PageFrame>
    )
}

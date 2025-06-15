import * as Layout from '../layout'

export function PageFrameExample() {
  return (
    <Layout.PageFrame
      headerContent={(
        <Layout.PageFrameTitle>
          ここがヘッダー
        </Layout.PageFrameTitle>
      )}
      className="flex flex-col justify-center items-center"
    >
      <h1>ページフレーム (PageFrame)</h1>
      <p>
        ページの枠組みを作成するコンポーネントです。
        ヘッダーとメインコンテンツを持つことができます。
      </p>
    </Layout.PageFrame>
  )
}

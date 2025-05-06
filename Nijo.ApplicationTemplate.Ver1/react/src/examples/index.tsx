import { Outlet } from 'react-router-dom'
import * as Input from '../input'
import * as Layout from '../layout'

// 各実装例コンポーネントのインポート
import { WordExample } from './WordExample'
import { NumberInputExample } from './NumberInputExample'
import { HyperLinkExample } from './HyperLinkExample'
import { IconButtonExample } from './IconButtonExample'
import { DateInputExample } from './DateInputExample'
import { EditableGridExample } from './EditableGridExample'
import { VFormExample } from './VFormExample'
import { PageFrameExample } from './PageFrameExample'
import { MainLayoutExample } from './MainLayoutExample'

/** UIコンポーネントへのリンク一覧を表示するコンポーネント */
export default function ComponentExampleIndex() {
  return (
    <Layout.PageFrame>
      <h1>UIコンポーネント仕様書兼実装例カタログ</h1>

      <h2>入力フォーム</h2>
      {/* to の先頭に / は不要 (相対パス) */}
      <Input.HyperLink to="word">単語入力</Input.HyperLink>
      <br />
      <Input.HyperLink to="number-input">数値入力</Input.HyperLink>
      <br />
      <Input.HyperLink to="hyperlink">ハイパーリンク</Input.HyperLink>
      <br />
      <Input.HyperLink to="icon-button">アイコンボタン</Input.HyperLink>
      <br />
      <Input.HyperLink to="date-input">日付入力</Input.HyperLink>
      <br />

      <h2>レイアウト</h2>
      {/* MultiView は削除済み */}
      <Input.HyperLink to="editable-grid">編集可能グリッド</Input.HyperLink>
      <br />
      <Input.HyperLink to="vform">フォームレイアウト</Input.HyperLink>
      <br />
      <Input.HyperLink to="page-frame">ページフレーム</Input.HyperLink>
      <br />
      <Input.HyperLink to="main-layout">メインレイアウト</Input.HyperLink>
      <br />
    </Layout.PageFrame>
  )
}

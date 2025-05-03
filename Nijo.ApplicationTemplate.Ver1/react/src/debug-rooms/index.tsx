import React from 'react';
import { Link, Routes, Route } from 'react-router-dom';
import { 基本的入力フォームの実装例 } from './基本的入力フォームの実装例';
import { IconButtonの実装例 } from './IconButtonの実装例';
import { EditableGridの実装例 } from './EditableGridの実装例';
import { VForm3 } from '../layout/VForm3';
import { RouteObjectWithSideMenuSetting } from "../routes"

// フォームレイアウトの実装例
const VForm3の実装例: React.FC = () => {
  return (
    <div className="p-4">
      <h1 className="text-xl font-bold mb-4">VForm3 (レスポンシブフォーム) 実装例</h1>

      <VForm3.Root labelWidth="10rem">
        <VForm3.BreakPoint label="個人情報">
          <VForm3.Item label="氏名" required>
            <input type="text" className="border border-gray-300 p-1" />
          </VForm3.Item>
          <VForm3.Item label="年齢">
            <input type="number" className="border border-gray-300 p-1" />
          </VForm3.Item>
          <VForm3.Item label="メールアドレス">
            <input type="email" className="border border-gray-300 p-1 w-full" />
          </VForm3.Item>
        </VForm3.BreakPoint>

        <VForm3.BreakPoint label="住所">
          <VForm3.Item label="郵便番号">
            <input type="text" className="border border-gray-300 p-1" />
          </VForm3.Item>
          <VForm3.Item label="都道府県">
            <select className="border border-gray-300 p-1">
              <option>選択してください</option>
              <option>東京都</option>
              <option>大阪府</option>
              <option>愛知県</option>
            </select>
          </VForm3.Item>
          <VForm3.Item label="市区町村">
            <input type="text" className="border border-gray-300 p-1 w-full" />
          </VForm3.Item>
        </VForm3.BreakPoint>

        <VForm3.FullWidthItem label="備考">
          <textarea
            className="border border-gray-300 p-1 w-full h-24"
            placeholder="備考を入力してください"
          />
        </VForm3.FullWidthItem>
      </VForm3.Root>

      <div className="mt-6 p-4 border rounded bg-gray-50">
        <h2 className="text-lg font-semibold mb-2">説明</h2>
        <ul className="list-disc pl-5 space-y-1">
          <li><code>VForm3.Root</code>: フォームのルートコンテナで、 <code>labelWidth</code> プロパティでラベル幅を指定できます。</li>
          <li><code>VForm3.BreakPoint</code>: レスポンシブ対応の折り返しが発生する単位です。画面サイズが小さくなると、これらが縦に積み重なります。<code>label</code> プロパティでグループラベルを設定できます。</li>
          <li><code>VForm3.Item</code>: ラベルと入力フィールドのペア。<code>required</code> プロパティで必須項目を示せます。</li>
          <li><code>VForm3.FullWidthItem</code>: 常に画面幅いっぱいに表示される項目です。長いテキストエリアなどに適しています。</li>
          <li>このコンポーネントを使うことで、レスポンシブ対応のきれいなフォームレイアウトを簡単に作成できます。</li>
          <li>画面サイズを変更すると、自動的に <code>BreakPoint</code> 単位で折り返しが発生します。</li>
        </ul>
      </div>
    </div>
  );
};

/**
 * デバッグルーム
 *
 * このアプリケーションに含まれるUIコンポーネントを理解するためのスクリーンショットや、
 * 実際に動作を試せる実装例を配置している。
 *
 * 頻繁に仕様変更が入る可能性が高いため、デバッグルームといえど変更容易性を重視して作成する。
 */
export const DebugRooms: React.FC = () => {
  // 開発環境でない場合は表示しない
  if (!import.meta.env.DEV) {
    return null;
  }

  return (
    <Routes>
      <Route path="/" element={<DebugRoomIndex />} />
      <Route path="/basic-form" element={<基本的入力フォームの実装例 />} />
      <Route path="/icon-button" element={<IconButtonの実装例 />} />
      <Route path="/editable-grid" element={<EditableGridの実装例 />} />
      <Route path="/vform3" element={<VForm3の実装例 />} />
    </Routes>
  );
};

const DebugRoomIndex: React.FC = () => {
  return (
    <div className="p-4">
      <h1 className="text-xl font-bold mb-4">Debug Rooms - コンポーネント実装例</h1>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <ComponentCard
          title="基本的入力フォーム"
          description="文字数値系.tsx、日付時刻系.tsxのコンポーネント（Word, NumberInput, DateInput）の使用例"
          link="/debug-rooms/basic-form"
        />

        <ComponentCard
          title="IconButton"
          description="様々なスタイルやオプションを持つボタンコンポーネントの使用例"
          link="/debug-rooms/icon-button"
        />

        <ComponentCard
          title="EditableGrid"
          description="編集可能なグリッドコンポーネントの使用例"
          link="/debug-rooms/editable-grid"
        />

        <ComponentCard
          title="VForm3"
          description="レスポンシブ対応のフォームレイアウトコンポーネントの使用例"
          link="/debug-rooms/vform3"
        />
      </div>
    </div>
  );
};

const ComponentCard: React.FC<{
  title: string;
  description: string;
  link: string;
}> = ({ title, description, link }) => {
  return (
    <div className="border rounded-lg p-4 hover:shadow-md transition-shadow">
      <h2 className="text-lg font-semibold mb-2">{title}</h2>
      <p className="text-gray-600 mb-4">{description}</p>
      <Link
        to={link}
        className="inline-block px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 transition-colors"
      >
        実装例を見る
      </Link>
    </div>
  );
};

/** 各種デバッグルームのルーティング定義 */
export const DEBUG_ROOMS_ROUTES: RouteObjectWithSideMenuSetting[] = [
  { path: "/debug-rooms", element: <DebugRooms />, sideMenuLabel: "【開発用】デバッグルーム" },
  { path: "/debug-rooms/basic-form", element: <基本的入力フォームの実装例 /> },
  { path: "/debug-rooms/icon-button", element: <IconButtonの実装例 /> },
  { path: "/debug-rooms/editable-grid", element: <EditableGridの実装例 /> },
  { path: "/debug-rooms/vform3", element: <VForm3の実装例 /> },
]

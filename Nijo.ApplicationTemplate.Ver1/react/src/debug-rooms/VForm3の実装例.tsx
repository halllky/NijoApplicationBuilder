import React, { useState } from 'react';
import { VForm3 } from '../layout/VForm3';
import { IconButton } from '../input/IconButton';

/**
 * VForm3レイアウトコンポーネントの使用方法を示す実装例
 */
export const VForm3の実装例: React.FC = () => {
  // フォームの状態を管理
  const [formData, setFormData] = useState({
    name: '',
    age: '',
    email: '',
    postalCode: '',
    prefecture: '',
    city: '',
    address: '',
    gender: '',
    hobbies: [] as string[],
    remarks: '',
    agreement: false
  });

  // 入力変更ハンドラ
  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value, type } = e.target;

    if (type === 'checkbox') {
      const checked = (e.target as HTMLInputElement).checked;
      setFormData(prev => ({ ...prev, [name]: checked }));
    } else if (type === 'radio') {
      setFormData(prev => ({ ...prev, [name]: value }));
    } else {
      setFormData(prev => ({ ...prev, [name]: value }));
    }
  };

  // 趣味のチェックボックス変更ハンドラ
  const handleHobbyChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { value, checked } = e.target;
    setFormData(prev => {
      if (checked) {
        return { ...prev, hobbies: [...prev.hobbies, value] };
      } else {
        return { ...prev, hobbies: prev.hobbies.filter(hobby => hobby !== value) };
      }
    });
  };

  // フォーム送信ハンドラ
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    alert(`送信データ:\n${JSON.stringify(formData, null, 2)}`);
  };

  // リセットハンドラ
  const handleReset = () => {
    setFormData({
      name: '',
      age: '',
      email: '',
      postalCode: '',
      prefecture: '',
      city: '',
      address: '',
      gender: '',
      hobbies: [],
      remarks: '',
      agreement: false
    });
  };

  return (
    <div className="p-4">
      <h1 className="text-xl font-bold mb-4">VForm3 (レスポンシブフォーム) 実装例</h1>

      <form onSubmit={handleSubmit}>
        <VForm3.Root labelWidth="10rem">
          {/* 個人情報グループ */}
          <VForm3.BreakPoint label="個人情報">
            <VForm3.Item label="氏名" required>
              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleChange}
                className="border border-gray-300 p-1 w-full"
                required
              />
            </VForm3.Item>

            <VForm3.Item label="年齢">
              <input
                type="number"
                name="age"
                value={formData.age}
                onChange={handleChange}
                className="border border-gray-300 p-1"
              />
            </VForm3.Item>

            <VForm3.Item label="メールアドレス">
              <input
                type="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                className="border border-gray-300 p-1 w-full"
              />
            </VForm3.Item>

            <VForm3.Item label="性別">
              <div className="flex space-x-4">
                <label className="flex items-center">
                  <input
                    type="radio"
                    name="gender"
                    value="male"
                    checked={formData.gender === 'male'}
                    onChange={handleChange}
                    className="mr-1"
                  />
                  男性
                </label>
                <label className="flex items-center">
                  <input
                    type="radio"
                    name="gender"
                    value="female"
                    checked={formData.gender === 'female'}
                    onChange={handleChange}
                    className="mr-1"
                  />
                  女性
                </label>
                <label className="flex items-center">
                  <input
                    type="radio"
                    name="gender"
                    value="other"
                    checked={formData.gender === 'other'}
                    onChange={handleChange}
                    className="mr-1"
                  />
                  その他
                </label>
              </div>
            </VForm3.Item>
          </VForm3.BreakPoint>

          {/* 住所グループ */}
          <VForm3.BreakPoint label="住所">
            <VForm3.Item label="郵便番号">
              <input
                type="text"
                name="postalCode"
                value={formData.postalCode}
                onChange={handleChange}
                className="border border-gray-300 p-1"
                placeholder="123-4567"
              />
            </VForm3.Item>

            <VForm3.Item label="都道府県">
              <select
                name="prefecture"
                value={formData.prefecture}
                onChange={handleChange}
                className="border border-gray-300 p-1"
              >
                <option value="">選択してください</option>
                <option value="tokyo">東京都</option>
                <option value="osaka">大阪府</option>
                <option value="aichi">愛知県</option>
                <option value="hokkaido">北海道</option>
                <option value="fukuoka">福岡県</option>
              </select>
            </VForm3.Item>

            <VForm3.Item label="市区町村">
              <input
                type="text"
                name="city"
                value={formData.city}
                onChange={handleChange}
                className="border border-gray-300 p-1 w-full"
              />
            </VForm3.Item>

            <VForm3.Item label="番地・建物名">
              <input
                type="text"
                name="address"
                value={formData.address}
                onChange={handleChange}
                className="border border-gray-300 p-1 w-full"
              />
            </VForm3.Item>
          </VForm3.BreakPoint>

          {/* 趣味グループ */}
          <VForm3.BreakPoint label="趣味・特技">
            <VForm3.Item label="趣味">
              <div className="grid grid-cols-2 gap-2">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    name="hobbies"
                    value="reading"
                    checked={formData.hobbies.includes('reading')}
                    onChange={handleHobbyChange}
                    className="mr-1"
                  />
                  読書
                </label>
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    name="hobbies"
                    value="sports"
                    checked={formData.hobbies.includes('sports')}
                    onChange={handleHobbyChange}
                    className="mr-1"
                  />
                  スポーツ
                </label>
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    name="hobbies"
                    value="music"
                    checked={formData.hobbies.includes('music')}
                    onChange={handleHobbyChange}
                    className="mr-1"
                  />
                  音楽
                </label>
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    name="hobbies"
                    value="travel"
                    checked={formData.hobbies.includes('travel')}
                    onChange={handleHobbyChange}
                    className="mr-1"
                  />
                  旅行
                </label>
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    name="hobbies"
                    value="cooking"
                    checked={formData.hobbies.includes('cooking')}
                    onChange={handleHobbyChange}
                    className="mr-1"
                  />
                  料理
                </label>
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    name="hobbies"
                    value="gaming"
                    checked={formData.hobbies.includes('gaming')}
                    onChange={handleHobbyChange}
                    className="mr-1"
                  />
                  ゲーム
                </label>
              </div>
            </VForm3.Item>
          </VForm3.BreakPoint>

          {/* 全幅アイテム */}
          <VForm3.FullWidthItem label="備考">
            <textarea
              name="remarks"
              value={formData.remarks}
              onChange={handleChange}
              className="border border-gray-300 p-1 w-full h-24"
              placeholder="備考を入力してください"
            />
          </VForm3.FullWidthItem>

          {/* 同意チェックボックス */}
          <VForm3.FullWidthItem>
            <label className="flex items-center">
              <input
                type="checkbox"
                name="agreement"
                checked={formData.agreement}
                onChange={handleChange}
                className="mr-2"
                required
              />
              <span>利用規約に同意します <span className="text-red-500">*</span></span>
            </label>
          </VForm3.FullWidthItem>

          {/* 送信ボタン */}
          <VForm3.FullWidthItem>
            <div className="flex justify-center space-x-4 mt-4">
              <IconButton outline onClick={handleReset}>
                リセット
              </IconButton>
              <IconButton submit fill>
                送信
              </IconButton>
            </div>
          </VForm3.FullWidthItem>
        </VForm3.Root>
      </form>

      <div className="mt-6 p-4 border rounded bg-gray-50">
        <h2 className="text-lg font-semibold mb-2">VForm3コンポーネントの説明</h2>
        <ul className="list-disc pl-5 space-y-1">
          <li><code>VForm3.Root</code>: フォームのルートコンテナ。<code>labelWidth</code> プロパティでラベル幅を指定できます。</li>
          <li><code>VForm3.BreakPoint</code>: レスポンシブ対応の折り返しが発生する単位。画面サイズが小さくなると、縦に積み重なります。<code>label</code> プロパティでグループラベルを設定できます。</li>
          <li><code>VForm3.Item</code>: ラベルと入力フィールドのペア。<code>required</code> プロパティで必須項目を示せます。</li>
          <li><code>VForm3.FullWidthItem</code>: 常に画面幅いっぱいに表示される項目。長いテキストエリアなどに適しています。</li>
        </ul>

        <h2 className="text-lg font-semibold mt-4 mb-2">コンポーネントの特徴</h2>
        <ul className="list-disc pl-5 space-y-1">
          <li>レスポンシブ対応: 画面サイズに応じて自動的にレイアウトが変わります。ブラウザの幅を変更してみてください。</li>
          <li>グループ化: <code>BreakPoint</code> コンポーネントで関連する項目をグループ化できます。</li>
          <li>ラベルの一貫性: すべてのフォーム項目で一貫したラベルの表示位置と幅を維持します。</li>
          <li>必須項目の表示: 必須項目は自動的に赤いアスタリスクが表示されます。</li>
          <li>全幅アイテム: <code>FullWidthItem</code> を使うことで、グリッドの幅全体を使うことができます。</li>
        </ul>

        <div className="mt-4 p-3 bg-blue-50 border border-blue-200 rounded">
          <p className="text-sm text-blue-800">
            <strong>ヒント:</strong> ブラウザのウィンドウサイズを変更すると、フォームのレイアウトが自動的に調整されることを確認できます。
            特に、横幅が狭くなると <code>BreakPoint</code> ごとに縦に積み重なる様子を観察できます。
          </p>
        </div>
      </div>
    </div>
  );
};

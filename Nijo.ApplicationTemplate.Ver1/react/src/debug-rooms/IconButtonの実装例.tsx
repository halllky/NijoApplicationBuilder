import React, { useState } from 'react';
import { IconButton } from '../input/IconButton';

// Heroiconsのアイコンをインポート（例としてここでは独自にSVGを定義）
const SaveIcon = (props: { className?: string }) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={props.className}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M17.593 3.322c1.1.128 1.907 1.077 1.907 2.185V21L12 17.25 4.5 21V5.507c0-1.108.806-2.057 1.907-2.185a48.507 48.507 0 0111.186 0z" />
  </svg>
);

const TrashIcon = (props: { className?: string }) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={props.className}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
  </svg>
);

export const IconButtonの実装例: React.FC = () => {
  const [isLoading, setIsLoading] = useState(false);

  const handleClick = () => {
    setIsLoading(true);
    setTimeout(() => {
      setIsLoading(false);
    }, 2000);
  };

  return (
    <div className="p-4">
      <h1 className="text-xl font-bold mb-4">IconButton 実装例</h1>

      <div className="space-y-8">
        <section>
          <h2 className="text-lg font-semibold mb-2">基本スタイル</h2>
          <div className="flex flex-wrap gap-4">
            <div>
              <p className="mb-1 text-sm">標準ボタン</p>
              <IconButton onClick={() => alert('標準ボタンがクリックされました')}>
                通常ボタン
              </IconButton>
            </div>

            <div>
              <p className="mb-1 text-sm">塗りつぶしボタン (fill)</p>
              <IconButton fill onClick={() => alert('fillボタンがクリックされました')}>
                Fill ボタン
              </IconButton>
            </div>

            <div>
              <p className="mb-1 text-sm">アウトラインボタン (outline)</p>
              <IconButton outline onClick={() => alert('outlineボタンがクリックされました')}>
                Outline ボタン
              </IconButton>
            </div>

            <div>
              <p className="mb-1 text-sm">下線ボタン (underline)</p>
              <IconButton underline onClick={() => alert('underlineボタンがクリックされました')}>
                Underline ボタン
              </IconButton>
            </div>
          </div>
        </section>

        <section>
          <h2 className="text-lg font-semibold mb-2">アイコン付きボタン</h2>
          <div className="flex flex-wrap gap-4">
            <div>
              <p className="mb-1 text-sm">アイコン左</p>
              <IconButton icon={SaveIcon} fill onClick={() => alert('保存がクリックされました')}>
                保存する
              </IconButton>
            </div>

            <div>
              <p className="mb-1 text-sm">アイコン右 (iconRight)</p>
              <IconButton icon={SaveIcon} iconRight fill onClick={() => alert('保存がクリックされました')}>
                保存する
              </IconButton>
            </div>

            <div>
              <p className="mb-1 text-sm">アイコンのみ (hideText)</p>
              <IconButton icon={TrashIcon} outline hideText onClick={() => alert('削除がクリックされました')}>
                削除
              </IconButton>
            </div>
          </div>
        </section>

        <section>
          <h2 className="text-lg font-semibold mb-2">ローディング状態</h2>
          <div className="flex flex-wrap gap-4">
            <div>
              <p className="mb-1 text-sm">ローディング (loading)</p>
              <IconButton loading fill>
                読み込み中
              </IconButton>
            </div>

            <div>
              <p className="mb-1 text-sm">クリックでローディング開始</p>
              <IconButton
                loading={isLoading}
                fill
                icon={SaveIcon}
                onClick={handleClick}
              >
                保存
              </IconButton>
            </div>
          </div>
        </section>

        <section>
          <h2 className="text-lg font-semibold mb-2">その他のオプション</h2>
          <div className="flex flex-wrap gap-4">
            <div>
              <p className="mb-1 text-sm">送信ボタン (submit)</p>
              <IconButton submit fill>
                フォーム送信
              </IconButton>
            </div>

            <div>
              <p className="mb-1 text-sm">小さいボタン (mini)</p>
              <IconButton mini fill>
                ミニボタン
              </IconButton>
            </div>

            <div>
              <p className="mb-1 text-sm">インラインボタン (inline)</p>
              <p>テキスト中に <IconButton inline underline>インラインボタン</IconButton> を配置できます。</p>
            </div>
          </div>
        </section>

        <div className="p-4 border rounded bg-gray-50">
          <h2 className="text-lg font-semibold mb-2">説明</h2>
          <ul className="list-disc pl-5 space-y-1">
            <li><code>fill</code>: 塗りつぶしスタイルのボタン</li>
            <li><code>outline</code>: 枠線スタイルのボタン</li>
            <li><code>underline</code>: 下線スタイルのボタン</li>
            <li><code>icon</code>: アイコンコンポーネントを指定</li>
            <li><code>iconRight</code>: アイコンを右側に配置</li>
            <li><code>hideText</code>: テキストを非表示にしてアイコンのみ表示（アイコン必須）</li>
            <li><code>loading</code>: ローディング状態の表示</li>
            <li><code>submit</code>: フォーム送信ボタンとして機能</li>
            <li><code>mini</code>: 小さいサイズのボタン</li>
            <li><code>inline</code>: インラインテキスト内に配置可能なボタン</li>
            <li><code>onClick</code>: クリックイベントハンドラ</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

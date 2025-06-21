import React, { useState } from 'react'
import * as Input from '../input'
import { useHttpRequest } from '../util/useHttpRequest'
import useEvent from 'react-use-event-hook';
import QueryEditor from '../クエリエディタ'

export const Home: React.FC = () => {
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { post } = useHttpRequest();

  // データベースを初期化 (データ消去＆再作成)
  const [nowProcessing, setNowProcessing] = useState(false);
  const handleResetDatabase = useEvent(async () => {
    if (nowProcessing) return;
    if (!window.confirm('データベースを初期化 (データ消去＆再作成) しますか？')) {
      return;
    }
    setMessage(null);
    setError(null);
    setNowProcessing(true);
    try {
      const result = await post<{ success: boolean; message: string; error?: string; stackTrace?: string }, Record<string, never>>(
        '/api/debug-info/destroy-and-reset-database',
        {},
      );

      if (result && result.success) {
        setMessage(result.message || 'データベースが正常に初期化されました。');
      } else if (result) {
        setError(result.message || `データベースの初期化に失敗しました。\n${result.error}\n${result.stackTrace}`);
      } else {
        setError('データベースの初期化リクエスト中にエラーが発生しました。');
      }
    } catch (e: unknown) {
      setError(`予期せぬエラーが発生しました: ${e instanceof Error ? e.message : '不明なエラー'}`);
      console.error('Unhandled error during database reset:', e);
    } finally {
      setNowProcessing(false);
    }
  });

  const handleShowSwagger = useEvent(() => {
    window.open(`${import.meta.env.VITE_BACKEND_API}swagger/index.html`, '_blank')
  })

  return (
    <div className="container">
      <h1>アプリケーションテンプレート</h1>
      <p>
        このアプリケーションは、NijoApplicationBuilder で作成するアプリケーションのサンプルです。
        このアプリケーションでは React.js と ASP.NET Core を使用しています。
      </p>

      <div className="flex flex-col gap-2 m-4 p-2 border border-gray-300">
        <h2>開発用デバッグ機能</h2>
        <div>
          <Input.IconButton onClick={handleResetDatabase} fill loading={nowProcessing}>
            データベースを初期化 (データ消去＆再作成)
          </Input.IconButton>
          {message && <p style={{ color: 'green', marginTop: '10px' }}>{message}</p>}
          {error && <p style={{ color: 'red', marginTop: '10px' }}>{error}</p>}
        </div>
        <div>
          <Input.IconButton onClick={handleShowSwagger} fill>
            Swaggerを表示
          </Input.IconButton>
        </div>
      </div>

      <QueryEditor />
    </div>
  )
}

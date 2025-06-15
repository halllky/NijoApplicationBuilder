import React, { useState } from 'react'
import * as Input from '../input'
import { useHttpRequest } from '../util/useHttpRequest'
import useEvent from 'react-use-event-hook';

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

  return (
    <div className="container">
      <h1>アプリケーションテンプレート</h1>
      <p>
        このアプリケーションは、NijoApplicationBuilder で作成するアプリケーションのサンプルです。
        このアプリケーションでは React.js と ASP.NET Core を使用しています。
      </p>

      <div style={{ marginTop: '20px', padding: '10px', border: '1px solid #eee' }}>
        <h2>開発用デバッグ機能</h2>
        <Input.IconButton onClick={handleResetDatabase} fill loading={nowProcessing}>
          データベースを初期化 (データ消去＆再作成)
        </Input.IconButton>
        {message && <p style={{ color: 'green', marginTop: '10px' }}>{message}</p>}
        {error && <p style={{ color: 'red', marginTop: '10px' }}>{error}</p>}
      </div>
    </div>
  )
}

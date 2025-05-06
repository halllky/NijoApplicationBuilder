import React, { useState } from 'react'
import { useHttpRequest } from '../util/useHttpRequest'

export const Home: React.FC = () => {
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { post } = useHttpRequest();

  const handleResetDatabase = async () => {
    setMessage(null);
    setError(null);
    try {
      const result = await post<{ success: boolean; message: string; error?: string; stackTrace?: string }, Record<string, never>>(
        '/api/debug-info/destroy-and-reset-database',
        {},
      );

      if (result && result.success) {
        setMessage(result.message || 'データベースが正常に初期化されました。');
      } else if (result) {
        setError(result.message || 'データベースの初期化に失敗しました。');
        if (result.error) console.error('Database reset error:', result.error);
        if (result.stackTrace) console.error('Stack trace:', result.stackTrace);
      } else {
        setError('データベースの初期化リクエスト中にエラーが発生しました。');
      }
    } catch (e: any) {
      setError(`予期せぬエラーが発生しました: ${e.message}`);
      console.error('Unhandled error during database reset:', e);
    }
  };

  return (
    <div className="container">
      <h1>アプリケーションテンプレート</h1>
      <p>
        このアプリケーションは、NijoApplicationBuilder で作成するアプリケーションのサンプルです。
        このアプリケーションでは React.js と ASP.NET Core を使用しています。
      </p>

      <div style={{ marginTop: '20px', padding: '10px', border: '1px solid #eee' }}>
        <h2>開発用デバッグ機能</h2>
        <button
          onClick={handleResetDatabase}
          style={{
            padding: '10px 15px',
            backgroundColor: '#dc3545',
            color: 'white',
            border: 'none',
            borderRadius: '5px',
            cursor: 'pointer',
          }}
        >
          データベースを初期化 (データ消去＆再作成)
        </button>
        {message && <p style={{ color: 'green', marginTop: '10px' }}>{message}</p>}
        {error && <p style={{ color: 'red', marginTop: '10px' }}>{error}</p>}
      </div>
    </div>
  )
}

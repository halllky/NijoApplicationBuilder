import { useCallback } from 'react'
import { useHttpRequest } from './Http'

export const useDummyDataGenerator = () => {
  const { post } = useHttpRequest()

  return useCallback(async () => {
    let hasError = false

    // インラインでダミーデータを作成して登録APIを呼ぶ処理がここに生成されます

    return hasError

    // 自動生成されるコードでpostを使うのでpostへの依存は必要
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [post])
}

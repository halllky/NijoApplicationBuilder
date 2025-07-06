import '@testing-library/jest-dom'
import { beforeAll, afterEach, afterAll } from 'vitest'
import { cleanup } from '@testing-library/react'

// テスト前の設定
beforeAll(() => {
  // 必要に応じてグローバルなmock設定を追加
})

// 各テスト後のクリーンアップ
afterEach(() => {
  cleanup()
})

// 全テスト後のクリーンアップ
afterAll(() => {
  // 必要に応じてクリーンアップ処理を追加
})

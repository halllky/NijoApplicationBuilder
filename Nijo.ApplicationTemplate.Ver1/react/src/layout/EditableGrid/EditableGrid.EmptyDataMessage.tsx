import * as React from 'react';
import { memo } from 'react';

/**
 * 空データ表示コンポーネント
 */
export const EmptyDataMessage = memo(() => (
  <div
    className="p-4 text-center text-gray-500"
    role="status"
    aria-live="polite"
  >
    データがありません
  </div>
))
EmptyDataMessage.displayName = 'EmptyDataMessage'

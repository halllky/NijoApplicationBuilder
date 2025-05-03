import * as React from 'react';
import { memo } from 'react';

interface HeaderCellProps {
  header: string;
}

/**
 * ヘッダーセルコンポーネント
 */
export const HeaderCell = memo(({ header }: HeaderCellProps) => {
  return (
    <div className="w-full h-full font-bold p-2">
      {header}
    </div>
  )
})
HeaderCell.displayName = 'HeaderCell'

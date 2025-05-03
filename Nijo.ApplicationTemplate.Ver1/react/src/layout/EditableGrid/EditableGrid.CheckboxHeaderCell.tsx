import * as React from 'react';
import { memo } from 'react';
import { type VirtualItem } from '@tanstack/react-virtual';

interface CheckboxHeaderCellProps {
  allRowsSelected: boolean;
  onToggleAllRows: (checked: boolean) => void;
  showCheckBox: boolean | ((row: any, rowIndex: number) => boolean) | undefined;
  virtualColumn: VirtualItem;
}

/**
 * チェックボックスヘッダーコンポーネント
 */
export const CheckboxHeaderCell = memo(({
  allRowsSelected,
  onToggleAllRows,
  showCheckBox,
  virtualColumn
}: CheckboxHeaderCellProps) => {
  return (
    <div
      className="border-b border-r border-gray-300 font-bold"
      style={{
        position: 'absolute',
        left: virtualColumn.start + 'px',
        width: virtualColumn.size + 'px',
        height: '40px',
      }}
      role="columnheader"
      aria-colindex={1}
    >
      <div className="w-10 h-10 flex justify-center items-center">
        {showCheckBox && (
          <input
            type="checkbox"
            checked={allRowsSelected}
            onChange={(e) => onToggleAllRows(e.target.checked)}
            aria-label="全行選択"
          />
        )}
      </div>
    </div>
  )
})
CheckboxHeaderCell.displayName = 'CheckboxHeaderCell'

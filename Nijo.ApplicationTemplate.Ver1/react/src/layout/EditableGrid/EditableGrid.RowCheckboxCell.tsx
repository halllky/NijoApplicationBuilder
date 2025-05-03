import * as React from 'react';
import { memo } from 'react';
import { type VirtualItem } from '@tanstack/react-virtual';

interface RowCheckboxCellProps {
  rowIndex: number;
  isSelected: boolean;
  showCheckBox: boolean | undefined;
  onToggleRow: (rowIndex: number, checked: boolean) => void;
  virtualColumn: VirtualItem;
}

/**
 * 行チェックボックスコンポーネント
 */
export const RowCheckboxCell = memo(({
  rowIndex,
  isSelected,
  showCheckBox,
  onToggleRow,
  virtualColumn
}: RowCheckboxCellProps) => {
  return (
    <div
      style={{
        position: 'absolute',
        left: virtualColumn.start + 'px',
        width: virtualColumn.size + 'px',
        height: '100%',
      }}
      className="border-b border-r border-gray-300"
      role="cell"
      aria-colindex={1}
    >
      <div className="w-10 h-8 flex justify-center items-center">
        {showCheckBox && (
          <input
            type="checkbox"
            checked={isSelected}
            onChange={(e) => onToggleRow(rowIndex, e.target.checked)}
            aria-label={`行${rowIndex + 1}を選択`}
          />
        )}
      </div>
    </div>
  )
})
RowCheckboxCell.displayName = 'RowCheckboxCell'

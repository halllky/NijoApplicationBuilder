import * as React from 'react';
import { memo } from 'react';

interface RowCheckboxCellProps {
  rowIndex: number;
  isChecked: boolean;
  onToggle: (checked: boolean) => void;
  showCheckBox: boolean;
}

/**
 * 行チェックボックスコンポーネント
 */
export const RowCheckboxCell = memo(({
  rowIndex,
  isChecked,
  onToggle,
  showCheckBox
}: RowCheckboxCellProps) => {
  return (
    <div className="w-full flex justify-center items-center">
      {showCheckBox && (
        <input
          type="checkbox"
          checked={isChecked}
          onChange={(e) => onToggle(e.target.checked)}
          aria-label={`行${rowIndex + 1}を選択`}
        />
      )}
    </div>
  )
})
RowCheckboxCell.displayName = 'RowCheckboxCell'

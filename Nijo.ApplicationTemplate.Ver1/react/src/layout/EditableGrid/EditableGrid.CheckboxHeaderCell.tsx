import * as React from 'react';
import { memo } from 'react';

interface CheckboxHeaderCellProps {
  isChecked: boolean;
  onToggle: (checked: boolean) => void;
  showCheckBox: boolean;
}

/**
 * チェックボックスヘッダーコンポーネント
 */
export const CheckboxHeaderCell = memo(({
  isChecked,
  onToggle,
  showCheckBox
}: CheckboxHeaderCellProps) => {
  return (
    <div className="w-10 h-10 flex justify-center items-center">
      {showCheckBox && (
        <input
          type="checkbox"
          checked={isChecked}
          onChange={(e) => onToggle(e.target.checked)}
          aria-label="全行選択"
        />
      )}
    </div>
  )
})
CheckboxHeaderCell.displayName = 'CheckboxHeaderCell'

import * as React from 'react';
import { memo } from 'react';
import { type Cell } from '@tanstack/react-table';
import { type VirtualItem } from '@tanstack/react-virtual';

interface DataCellProps {
  cell: Cell<any, unknown>;
  rowIndex: number;
  colIndex: number;
  isActive: boolean;
  isInRange: boolean;
  isReadOnly: boolean;
  isEditing: boolean;
  editValue: string;
  onEditValueChange: (value: string) => void;
  onConfirmEdit: () => void;
  onCancelEdit: () => void;
  onStartEditing: () => void;
  onClickCell: () => void;
  onMouseDown: () => void;
  onMouseMove: () => void;
  virtualColumn: VirtualItem;
}

/**
 * データセルコンポーネント
 */
export const DataCell = memo(({
  cell,
  rowIndex,
  colIndex,
  isActive,
  isInRange,
  isReadOnly,
  isEditing,
  editValue,
  onEditValueChange,
  onConfirmEdit,
  onCancelEdit,
  onStartEditing,
  onClickCell,
  onMouseDown,
  onMouseMove,
  virtualColumn
}: DataCellProps) => {
  const cellValue = cell.getValue()

  return (
    <div
      style={{
        position: 'absolute',
        left: virtualColumn.start + 'px',
        width: virtualColumn.size + 'px',
        height: '100%',
      }}
      className="border-b border-r border-gray-300"
      onMouseDown={onMouseDown}
      onMouseMove={onMouseMove}
      role="cell"
      aria-colindex={colIndex + 2} // +2 for row header and 0-indexing
      aria-selected={isActive || isInRange}
      data-testid={`cell-${rowIndex}-${colIndex}`}
    >
      {isActive && isEditing && !isReadOnly ? (
        <input
          className="w-full h-full outline-none border-none p-1"
          value={editValue}
          onChange={(e) => onEditValueChange(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && !e.nativeEvent.isComposing) {
              e.preventDefault()
              onConfirmEdit()
            } else if (e.key === 'Escape') {
              e.preventDefault()
              onCancelEdit()
            }
          }}
          autoFocus
          aria-label={`${rowIndex + 1}行目 ${colIndex + 1}列目を編集`}
        />
      ) : (
        <div
          className={`p-1 h-8 w-full overflow-hidden ${isActive ? 'bg-blue-100' : ''} ${isInRange ? 'bg-blue-50' : ''}`}
          onClick={onClickCell}
          onDoubleClick={() => {
            if (!isReadOnly) {
              onStartEditing()
            }
          }}
          tabIndex={isActive ? 0 : -1}
        >
          {cellValue?.toString() || ''}
        </div>
      )}
    </div>
  )
})
DataCell.displayName = 'DataCell'

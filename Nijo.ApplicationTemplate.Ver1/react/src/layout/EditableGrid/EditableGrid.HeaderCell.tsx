import * as React from 'react';
import { memo } from 'react';
import { flexRender, type Header } from '@tanstack/react-table';
import { type VirtualItem } from '@tanstack/react-virtual';

interface HeaderCellProps {
  header: Header<any, unknown>;
  virtualColumn: VirtualItem;
}

/**
 * ヘッダーセルコンポーネント
 */
export const HeaderCell = memo(({ header, virtualColumn }: HeaderCellProps) => {
  return (
    <div
      key={header.id}
      className="border-b border-r border-gray-300 font-bold p-2"
      style={{
        position: 'absolute',
        left: virtualColumn.start + 'px',
        width: virtualColumn.size + 'px',
        height: '40px',
      }}
      role="columnheader"
      aria-colindex={virtualColumn.index + 1}
    >
      {header.isPlaceholder ? null : (
        flexRender(
          header.column.columnDef.header,
          header.getContext()
        )
      )}
    </div>
  )
})
HeaderCell.displayName = 'HeaderCell'

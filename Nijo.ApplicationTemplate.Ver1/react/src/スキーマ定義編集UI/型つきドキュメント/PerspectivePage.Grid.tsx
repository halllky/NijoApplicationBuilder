import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { PerspectiveNode, PerspectivePageData } from './types';
import { EditableGridRef } from '../../layout';

// PerspectivePageGridが公開するAPIの型
export interface PerspectivePageGridRef {
  selectRow: (startRowIndex: number, endRowIndex: number) => void;
}

export const PerspectivePageGrid = React.forwardRef(({
  formMethods,
  className,
}: {
  formMethods: ReactHookForm.UseFormReturn<PerspectivePageData>;
  className: string;
}, ref: React.ForwardedRef<PerspectivePageGridRef>) => {

  const { control } = formMethods;
  const { fields, append, remove, update } = ReactHookForm.useFieldArray({ name: 'perspective.nodes', control });

  const gridRef = React.useRef<EditableGridRef<PerspectiveNode>>(null);

  const getColumnDefs: Layout.GetColumnDefsFunction<PerspectiveNode> = React.useCallback((cellType) => [
    cellType.text('label', '', {
      defaultWidth: 540,
      isFixed: true,
      renderCell: (context) => {
        const indent = context.row.original.indent;
        return (
          <div className="flex-1 inline-flex text-left truncate">
            {Array.from({ length: indent }).map((_, i) => (
              <React.Fragment key={i}>
                <div className="basis-[20px] min-w-[20px] relative leading-none">
                  {i >= 1 && (
                    <div className="absolute top-[-1px] bottom-[-1px] left-0 border-l border-gray-400 border-dotted leading-none"></div>
                  )}
                </div>
              </React.Fragment>
            ))}
            <span className="flex-1 truncate">
              {context.cell.getValue() as string}
            </span>
          </div>
        );
      },
    }),
  ], []);

  const handleAddNode = useEvent(() => {
    append({
      nodeId: crypto.randomUUID(),
      label: '',
      indent: 0,
      entityId: undefined,
      parentId: undefined,
      comments: [],
    });
  });

  const handleDeleteNode = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange();
    if (!selectedRange) return;
    const removedIndexes = Array.from({ length: selectedRange.endRow - selectedRange.startRow + 1 }, (_, i) => selectedRange.startRow + i);
    remove(removedIndexes);
  });

  const handleChangeRow: Layout.RowChangeEvent<PerspectiveNode> = useEvent(e => {
    for (const changedRow of e.changedRows) {
      update(changedRow.rowIndex, changedRow.newRow);
    }
  });

  React.useImperativeHandle(ref, () => ({
    selectRow: (startRowIndex: number, endRowIndex: number) => {
      gridRef.current?.selectRow(startRowIndex, endRowIndex);
    },
  }), [gridRef]);

  return (
    <div className={`flex flex-col h-full ${className || ''}`}>
      <div className="flex gap-1 mb-1">
        <Input.IconButton outline mini hideText icon={Icon.PlusIcon} onClick={handleAddNode}>
          新しいノードを追加
        </Input.IconButton>
        <Input.IconButton outline mini hideText icon={Icon.TrashIcon} onClick={handleDeleteNode} disabled={fields.length === 0}>
          選択行のノードを削除
        </Input.IconButton>
      </div>
      <Layout.EditableGrid
        ref={gridRef}
        rows={fields}
        getColumnDefs={getColumnDefs}
        onChangeRow={handleChangeRow}
        className="flex-1"
      />
    </div>
  );
})

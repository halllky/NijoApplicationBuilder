import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { ArrowsRightLeftIcon, ArrowUturnLeftIcon, ArrowUturnRightIcon } from '@heroicons/react/24/outline';

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

  // 選択されている行の情報を保持するためのstate
  const [selectedRowIndices, setSelectedRowIndices] = React.useState<number[]>([]);

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
    setSelectedRowIndices([]); // 削除後は選択解除
  });

  const handleIncreaseIndent = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange();
    if (!selectedRange) return;
    for (let i = selectedRange.startRow; i <= selectedRange.endRow; i++) {
      const currentIndent = fields[i].indent;
      const newIndent = currentIndent + 1;

      let parentId: string | undefined = undefined;
      // 自分より前にあるノードを逆順に見て、新しいインデントより小さいインデントを持つ最初のノードを親とする
      for (let j = i - 1; j >= 0; j--) {
        if (fields[j].indent < newIndent) {
          parentId = fields[j].nodeId;
          break;
        }
      }
      update(i, { ...fields[i], indent: newIndent, parentId });
    }
  });

  const handleDecreaseIndent = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange();
    if (!selectedRange) return;
    for (let i = selectedRange.startRow; i <= selectedRange.endRow; i++) {
      const currentIndent = fields[i].indent;
      if (currentIndent > 0) {
        const newIndent = currentIndent - 1;
        let parentId: string | undefined = undefined;
        // 自分より前にあるノードを逆順に見て、新しいインデントより小さいインデントを持つ最初のノードを親とする
        for (let j = i - 1; j >= 0; j--) {
          if (fields[j].indent < newIndent) {
            parentId = fields[j].nodeId;
            break;
          }
        }
        update(i, { ...fields[i], indent: newIndent, parentId });
      }
    }
  });

  const handleChangeRow: Layout.RowChangeEvent<PerspectiveNode> = useEvent(e => {
    for (const changedRow of e.changedRows) {
      update(changedRow.rowIndex, changedRow.newRow);
    }
  });

  React.useImperativeHandle(ref, () => ({
    selectRow: (startRowIndex: number, endRowIndex: number) => {
      gridRef.current?.selectRow(startRowIndex, endRowIndex);
      // 選択行が変更されたときに state も更新
      setSelectedRowIndices(Array.from({ length: endRowIndex - startRowIndex + 1 }, (_, i) => startRowIndex + i));
    },
  }), [gridRef]);

  // グリッドの選択状態が変わったときに呼び出されるハンドラ
  const handleSelectionChange = useEvent((updater: React.SetStateAction<Record<string, boolean>>) => {
    const newSelection = typeof updater === 'function' ? updater({}) : updater; // updaterが関数の場合、現在の選択状態を引数に取る必要があるが、ここでは空のオブジェクトを渡して新しい選択状態を得る
    const selectedIndices = Object.entries(newSelection)
      .filter(([, isSelected]) => isSelected)
      .map(([index]) => parseInt(index, 10));
    setSelectedRowIndices(selectedIndices);
  });

  // インデント操作ボタンの無効状態を判定
  const isIndentControlDisabled = selectedRowIndices.length === 0;
  const isDecreaseIndentDisabled = selectedRowIndices.length === 0 || selectedRowIndices.some(index => fields[index]?.indent === 0);

  return (
    <div className={`flex flex-col h-full ${className || ''}`}>
      <div className="flex gap-1 mb-1">
        <Input.IconButton outline mini hideText icon={Icon.PlusIcon} onClick={handleAddNode}>
          新しいノードを追加
        </Input.IconButton>
        <Input.IconButton outline mini hideText icon={Icon.TrashIcon} onClick={handleDeleteNode} disabled={fields.length === 0 || selectedRowIndices.length === 0}>
          選択行のノードを削除
        </Input.IconButton>
        <Input.IconButton outline mini hideText icon={ArrowUturnRightIcon} onClick={handleIncreaseIndent} disabled={isIndentControlDisabled}>
          インデントを増やす
        </Input.IconButton>
        <Input.IconButton outline mini hideText icon={ArrowUturnLeftIcon} onClick={handleDecreaseIndent} disabled={isDecreaseIndentDisabled}>
          インデントを減らす
        </Input.IconButton>
      </div>
      <Layout.EditableGrid
        ref={gridRef}
        rows={fields}
        getColumnDefs={getColumnDefs}
        onChangeRow={handleChangeRow}
        onRowSelectionChange={handleSelectionChange}
        className="flex-1"
      />
    </div>
  );
})

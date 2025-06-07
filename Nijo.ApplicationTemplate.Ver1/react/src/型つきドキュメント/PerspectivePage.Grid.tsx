import * as React from 'react';
import * as ReactHookForm from 'react-hook-form';
import useEvent from 'react-use-event-hook';
import * as Layout from '../layout';
import { Entity, Perspective, PerspectivePageData } from './types';
import { MentionUtil } from './MentionTextarea';
import { UUID } from 'uuidjs';

export interface EntityTypePageProps {
  perspective: Perspective | undefined;
  useFieldArrayReturn: ReactHookForm.UseFieldArrayReturn<PerspectivePageData, 'perspective.nodes', 'uniqueId'>;
  onChangeRow: Layout.RowChangeEvent<GridRowType>;
  onSelectedRowChanged: (rowIndex: number) => void;
  setValue: ReactHookForm.UseFormSetValue<PerspectivePageData>;
  className?: string;
}

export type EntityTypePageRef = {
  /** 行選択 */
  selectRow: (startRowIndex: number, endRowIndex: number) => void;
  /** 行挿入 */
  insertRow: () => void;
  /** 下挿入 */
  insertRowBelow: () => void;
  /** 行削除 */
  deleteRow: () => void;
  /** 上に移動 */
  moveUp: () => void;
  /** 下に移動 */
  moveDown: () => void;
  /** インデント上げ */
  indentUp: () => void;
  /** インデント下げ */
  indentDown: () => void;
}

// グリッドの行の型
type GridRowType = Entity;

export const EntityTypePage = React.forwardRef<EntityTypePageRef, EntityTypePageProps>(({
  perspective,
  useFieldArrayReturn: {
    insert,
    remove,
    update,
    move,
    fields,
  },
  onSelectedRowChanged,
  setValue,
  onChangeRow,
  className,
}, ref) => {

  const gridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null);

  // グリッドの列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<GridRowType> = React.useCallback(cellType => {
    const columns: Layout.EditableGridColumnDef<GridRowType>[] = [];
    columns.push(
      cellType.text('entityName', '', {
        columnId: 'col:entity-name',
        defaultWidth: 540,
        isFixed: true,
        renderCell: context => {
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
                {MentionUtil.toPlainText(context.cell.getValue() as string)}
              </span>
            </div>
          );
        },
      })
    );
    if (perspective) {
      perspective.attributes.forEach((attrDef) => {
        columns.push(
          cellType.other(attrDef.attributeName, {
            columnId: `col:${attrDef.attributeId}`,
            defaultWidth: 120,
            onStartEditing: e => {
              e.setEditorInitialValue(e.row.attributeValues[attrDef.attributeId] ?? '');
            },
            onEndEditing: e => {
              const clone = window.structuredClone(e.row);
              if (String(e.value).trim() === '') {
                delete clone.attributeValues[attrDef.attributeId];
              } else {
                clone.attributeValues[attrDef.attributeId] = String(e.value);
              }
              e.setEditedRow(clone);
            },
            renderCell: context => {
              const value = context.row.original.attributeValues[attrDef.attributeId];
              return <PlainCell>{MentionUtil.toPlainText(value)}</PlainCell>;
            },
          })
        );
      });
    }
    return columns;
  }, [perspective?.attributes]);

  const PlainCell = ({ children, className }: {
    children?: React.ReactNode
    className?: string
  }) => {
    return (
      <div className={`flex-1 inline-flex text-left truncate ${className ?? ''}`}>
        <span className="flex-1 truncate">
          {children}
        </span>
      </div>
    )
  }

  // 選択されている行のインデックス
  const [selectedRowIndex, setSelectedRowIndex] = React.useState<number | undefined>(undefined);
  const handleActiveCellChanged = useEvent((cell: Layout.CellPosition | null) => {
    if (cell?.rowIndex !== selectedRowIndex && cell?.rowIndex !== undefined) {
      onSelectedRowChanged(cell.rowIndex);
    }
    setSelectedRowIndex(cell?.rowIndex);
  })

  // --------------------------------------
  // 編集

  // 選択行の位置に行挿入
  const handleInsertRow = useEvent(() => {
    if (!perspective?.perspectiveId) return;
    const newRow: GridRowType = {
      entityId: UUID.generate(),
      typeId: perspective.perspectiveId,
      entityName: '',
      indent: 0,
      attributeValues: {},
      comments: [],
    };
    const selectedRange = gridRef.current?.getSelectedRange();
    if (!selectedRange) {
      insert(0, newRow);
    } else {
      insert(selectedRange.startRow, newRow);
    }
  });

  // 下に行挿入
  const handleInsertRowBelow = useEvent(() => {
    if (!perspective?.perspectiveId) return;
    const newRow: GridRowType = {
      entityId: UUID.generate(),
      typeId: perspective.perspectiveId,
      entityName: '',
      indent: 0,
      attributeValues: {},
      comments: [],
    };
    const selectedRange = gridRef.current?.getSelectedRange();
    if (!selectedRange) {
      insert(fields.length, newRow);
    } else {
      insert(selectedRange.endRow + 1, newRow);
    }
  });

  // 行削除
  const handleDeleteRow = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange();
    if (!selectedRange) return;
    const removedIndexes = Array.from({ length: selectedRange.endRow - selectedRange.startRow + 1 }, (_, i) => selectedRange.startRow + i);
    remove(removedIndexes);
  });

  // 選択行を上に移動
  const handleMoveUp = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows();
    if (!selectedRows) return;

    const startRow = selectedRows[0].rowIndex;
    const endRow = startRow + selectedRows.length - 1;
    if (startRow === 0) return;

    // 選択範囲の外側（1つ上）の行を選択範囲の下に移動させる
    move(startRow - 1, endRow);
    // 行選択
    gridRef.current?.selectRow(startRow - 1, endRow - 1);
  });

  // 選択行を下に移動
  const handleMoveDown = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows();
    if (!selectedRows) return;

    const startRow = selectedRows[0].rowIndex;
    const endRow = startRow + selectedRows.length - 1;
    if (endRow >= fields.length - 1) return;

    // 選択範囲の外側（1つ下）の行を選択範囲の上に移動させる
    move(endRow + 1, startRow);
    // 行選択
    gridRef.current?.selectRow(startRow + 1, endRow + 1);
  });

  // 選択行のインデントを下げる
  const handleIndentDown = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows();
    if (!selectedRows) return;
    for (const x of selectedRows) {
      const currentItem = fields[x.rowIndex];
      if (currentItem) {
        update(x.rowIndex, { ...currentItem, indent: Math.max(0, currentItem.indent - 1) });
      }
    }
  });

  // 選択行のインデントを上げる
  const handleIndentUp = useEvent(() => {
    const selectedRows = gridRef.current?.getSelectedRows();
    if (!selectedRows) return;
    for (const x of selectedRows) {
      const currentItem = fields[x.rowIndex];
      if (currentItem) {
        update(x.rowIndex, { ...currentItem, indent: currentItem.indent + 1 });
      }
    }
  });

  // --------------------------------------

  // グリッドの列幅の自動保存
  const gridColumnStorage: Layout.EditableGridAutoSaveStorage = React.useMemo(() => ({
    loadState: () => {
      return perspective?.gridStates?.['root-grid'] ?? null
    },
    saveState: (value) => setValue('perspective.gridStates.root-grid', value),
  }), [setValue, perspective?.gridStates])

  // グリッドのキーボード操作
  const handleKeyDown: Layout.EditableGridKeyboardEventHandler = useEvent((e, isEditing) => {
    // 編集中の処理の制御はCellEditorに任せる
    if (isEditing) return { handled: false };

    if (!e.ctrlKey && e.key === 'Enter') {
      // 行挿入(Enter)
      handleInsertRow();
    } else if (e.ctrlKey && e.key === 'Enter') {
      // 下挿入(Ctrl + Enter)
      handleInsertRowBelow();
    } else if (e.shiftKey && e.key === 'Delete') {
      // 行削除(Shift + Delete)
      handleDeleteRow();
    } else if (e.altKey && e.key === 'ArrowUp') {
      // 上に移動(Alt + ↑)
      handleMoveUp();
    } else if (e.altKey && e.key === 'ArrowDown') {
      // 下に移動(Alt + ↓)
      handleMoveDown();
    } else if (e.shiftKey && e.key === 'Tab') {
      // インデント下げ(Shift + Tab)
      handleIndentDown();
    } else if (e.key === 'Tab') {
      // インデント上げ(Tab)
      handleIndentUp();
    } else {
      return { handled: false };
    }
    return { handled: true }
  })

  React.useImperativeHandle(ref, () => ({
    selectRow: (startRowIndex: number, endRowIndex: number) => {
      gridRef.current?.selectRow(startRowIndex, endRowIndex);
    },
    insertRow: handleInsertRow,
    insertRowBelow: handleInsertRowBelow,
    deleteRow: handleDeleteRow,
    moveUp: handleMoveUp,
    moveDown: handleMoveDown,
    indentUp: handleIndentUp,
    indentDown: handleIndentDown,
  }));

  return (
    <div className={`h-full flex flex-col gap-1 ${className ?? ''}`}>
      <div className="flex-1 overflow-y-auto">
        <Layout.EditableGrid
          ref={gridRef}
          rows={fields}
          getColumnDefs={getColumnDefs}
          onChangeRow={onChangeRow}
          onActiveCellChanged={handleActiveCellChanged}
          onKeyDown={handleKeyDown}
          className="h-full border border-gray-300"
          storage={gridColumnStorage}
        />
      </div>
    </div>
  );
});

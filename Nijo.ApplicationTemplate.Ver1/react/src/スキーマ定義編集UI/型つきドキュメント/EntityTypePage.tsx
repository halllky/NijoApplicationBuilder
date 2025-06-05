import * as React from 'react';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { UUID } from 'uuidjs';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { PerspectivePageData, Entity } from './types';
import { EntityTypeEditDialog } from './EntityTypeEditDialog';

export interface EntityTypePageProps {
  formMethods: ReactHookForm.UseFormReturn<PerspectivePageData>;
  className?: string;
}

export interface EntityTypePageGridRef {
  selectRow: (startRowIndex: number, endRowIndex: number) => void;
}

// グリッドの行の型
type GridRowType = Entity;

export const EntityTypePage = React.forwardRef<EntityTypePageGridRef, EntityTypePageProps>(({
  formMethods,
  className,
}, ref) => {
  const perspective = ReactHookForm.useWatch({ name: 'perspective', control: formMethods.control });
  const perspectiveId = ReactHookForm.useWatch({ name: 'perspective.perspectiveId', control: formMethods.control });

  const gridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null);

  const { pushDialog } = Layout.useDialogContext();
  const { control } = formMethods;
  const { fields, insert, remove, update } = ReactHookForm.useFieldArray({
    control,
    name: 'perspective.nodes',
    keyName: 'uniqueId',
  });

  React.useImperativeHandle(ref, () => ({
    selectRow: (startRowIndex: number, endRowIndex: number) => {
      gridRef.current?.selectRow(startRowIndex, endRowIndex);
    },
  }), [gridRef]);

  // グリッドの列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<GridRowType> = React.useCallback(cellType => {
    const columns: Layout.EditableGridColumnDef<GridRowType>[] = [];
    columns.push(
      cellType.text('entityName', '名称', {
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
                {context.cell.getValue() as string}
              </span>
            </div>
          );
        },
      })
    );
    if (perspective && perspective.attributes) {
      perspective.attributes.forEach((attrDef) => {
        columns.push(
          cellType.other(attrDef.attributeName, {
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
              return <PlainCell>{value}</PlainCell>;
            },
          })
        );
      });
    }
    return columns;
  }, [perspective]);

  const handleInsertRow = useEvent(() => {
    const newRow: GridRowType = {
      entityId: UUID.generate(),
      typeId: perspectiveId,
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

  const handleInsertRowBelow = useEvent(() => {
    const newRow: GridRowType = {
      entityId: UUID.generate(),
      typeId: perspectiveId,
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

  const handleDeleteRow = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange();
    if (!selectedRange) return;
    const removedIndexes = Array.from({ length: selectedRange.endRow - selectedRange.startRow + 1 }, (_, i) => selectedRange.startRow + i);
    remove(removedIndexes);
  });

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

  const handleChangeRow: Layout.RowChangeEvent<GridRowType> = useEvent(e => {
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow);
    }
  });

  const handleOpenEntityTypeEditDialog = useEvent(() => {
    if (!perspective) {
      alert("エンティティ型が選択されていません。");
      return;
    }

    pushDialog({ title: 'エンティティ型の編集', className: "max-w-lg max-h-[80vh]" }, ({ closeDialog }) => (
      <EntityTypeEditDialog
        initialEntityType={perspective}
        onApply={(updatedEntityType) => {
          formMethods.setValue('perspective', updatedEntityType);
          closeDialog();
        }}
        onCancel={closeDialog}
      />
    ));
  });

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

  return (
    <div className={`h-full flex flex-col gap-1 pl-1 pt-1 ${className ?? ''}`}>
      <div className="flex flex-wrap gap-1 items-center">
        <Input.IconButton outline mini hideText icon={Icon.PlusIcon} onClick={handleInsertRow}>行挿入</Input.IconButton>
        <Input.IconButton outline mini hideText icon={Icon.PlusIcon} onClick={handleInsertRowBelow}>下挿入</Input.IconButton>
        <Input.IconButton outline mini hideText icon={Icon.TrashIcon} onClick={handleDeleteRow}>行削除</Input.IconButton>
        <div className="basis-2"></div>
        <Input.IconButton outline mini hideText icon={Icon.ChevronDoubleLeftIcon} onClick={handleIndentDown}>インデント下げ</Input.IconButton>
        <Input.IconButton outline mini hideText icon={Icon.ChevronDoubleRightIcon} onClick={handleIndentUp}>インデント上げ</Input.IconButton>
        <div className="basis-2"></div>
        <Input.IconButton outline mini hideText icon={Icon.PencilSquareIcon} onClick={handleOpenEntityTypeEditDialog}>型定義編集</Input.IconButton>
        <div className="flex-1"></div>
      </div>
      <div className="flex-1 overflow-y-auto">
        <Layout.EditableGrid
          ref={gridRef}
          rows={fields}
          getColumnDefs={getColumnDefs}
          onChangeRow={handleChangeRow}
          className="h-full border-y border-l border-gray-300"
        />
      </div>
    </div>
  );
});

import * as React from 'react';
import * as ReactHookForm from 'react-hook-form';
import useEvent from 'react-use-event-hook';
import * as Icon from '@heroicons/react/24/outline';
import * as Input from '../input';
import * as Layout from '../layout';
import { applyFormatCondition, Entity, FormatCondition, Perspective, PerspectivePageData } from './types';
import { MentionTextarea, MentionUtil } from './MentionTextarea';
import { UUID } from 'uuidjs';
import { usePersonalSettings } from './PersonalSettings';

export interface EntityTypePageProps {
  perspective: Perspective | undefined;
  useFieldArrayReturn: ReactHookForm.UseFieldArrayReturn<PerspectivePageData, 'perspective.nodes', 'uniqueId'>;
  onChangeRow: Layout.RowChangeEvent<GridRowType>;
  onSelectedRowChanged: (rowIndex: number) => void;
  reset: ReactHookForm.UseFormReset<PerspectivePageData>;
  className?: string;
}

export type EntityTypePageRef = {
  /** 行選択 */
  selectRow: (startRowIndex: number, endRowIndex: number) => void;
}

// グリッドの行の型
type GridRowType = Entity;

/** 左端の列のID */
const COLUMN_ID_ENTITY_NAME = 'col:entity-name';

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
  reset,
  onChangeRow,
  className,
}, ref) => {

  const gridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null);

  // グリッドの列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<GridRowType> = React.useCallback(cellType => {
    const columns: Layout.EditableGridColumnDef<GridRowType>[] = [];
    columns.push(
      cellType.text('entityName', '', {
        columnId: COLUMN_ID_ENTITY_NAME,
        defaultWidth: 540,
        isFixed: true,
        renderCell: context => {
          const indent = context.row.original.indent;
          const text = context.cell.getValue() as string
          return (
            <div className="w-full flex-1 inline-flex text-left px-1">
              {Array.from({ length: indent }).map((_, i) => (
                <React.Fragment key={i}>
                  <div className="basis-[20px] min-w-[20px] relative leading-none">
                    {i >= 1 && (
                      <div className="absolute top-[-1px] bottom-[-1px] left-0 border-l border-gray-400 border-dotted leading-none"></div>
                    )}
                  </div>
                </React.Fragment>
              ))}
              <span className={`flex-1 ${perspective?.wrapEntityName ? 'whitespace-pre-wrap' : 'truncate'}`}>
                {MentionUtil.toPlainText(text || '-')}
              </span>
            </div>
          );
        },
      })
    );
    if (perspective) {
      perspective.attributes.forEach((attrDef) => {
        // グリッドで非表示の場合はスキップ
        if (attrDef.invisibleInGrid) return;

        columns.push(
          cellType.other(attrDef.attributeName, {
            columnId: `col:${attrDef.attributeId}`,
            defaultWidth: 120,
            onStartEditing: e => {
              e.setEditorInitialValue(e.row.attributeValues[attrDef.attributeId] ?? '');
            },
            onEndEditing: e => {
              const clone = window.structuredClone(e.row);
              const trimmed = String(e.value).trim();
              if (trimmed === '') {
                delete clone.attributeValues[attrDef.attributeId];
              } else if (attrDef.attributeType === 'select' && !attrDef.selectOptions?.includes(trimmed)) {
                delete clone.attributeValues[attrDef.attributeId];
              } else {
                clone.attributeValues[attrDef.attributeId] = trimmed;
              }
              e.setEditedRow(clone);
            },
            getOptions: attrDef.attributeType === 'select'
              ? (() => attrDef.selectOptions?.map(x => ({ value: x, label: x })) ?? [])
              : undefined,
            renderCell: context => {
              const value = context.row.original.attributeValues[attrDef.attributeId];
              return (
                <div className={`w-full px-1 text-left `}>
                  <span className={`block w-full ${attrDef.attributeType === 'description' ? 'whitespace-pre-wrap' : 'truncate'}`}>
                    {MentionUtil.toPlainText(value)}
                  </span>
                </div>
              )
            },
          })
        );
      });
    }
    return columns;
  }, [perspective?.attributes, perspective?.wrapEntityName]);

  // 選択されている行のインデックス
  const [selectedRowIndex, setSelectedRowIndex] = React.useState<number | undefined>(undefined);
  const handleActiveCellChanged = useEvent((cell: Layout.CellPosition | null) => {
    if (cell?.rowIndex !== selectedRowIndex && cell?.rowIndex !== undefined) {
      onSelectedRowChanged(cell.rowIndex);
    }
    setSelectedRowIndex(cell?.rowIndex);
  })

  // select型の列のID
  const selectColumnIds: Set<string> = React.useMemo(() => {
    return new Set(perspective?.attributes.filter(attr => attr.attributeType === 'select').map(attr => `col:${attr.attributeId}`) ?? [])
  }, [perspective?.attributes]);

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
    saveState: (value) => {
      // isDirtyがtrueにならないようにするため、setValueではなくresetを使う
      reset(defaultValues => {
        defaultValues.perspective.gridStates = { 'root-grid': value }
        return defaultValues
      })
    },
  }), [reset, perspective?.gridStates])

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
    } else if (e.altKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      // 上下に移動(Alt + ↑↓)
      // ただし、select型のセルの場合、選択肢のドロップダウン展開もAlt+↑↓で行うため、その場合は移動しない
      const activeCell = gridRef.current?.getActiveCell();
      const activeColumnId = activeCell?.getColumnDef().columnId;
      if (activeColumnId && selectColumnIds.has(activeColumnId)) {
        return { handled: false };
      } else if (e.key === 'ArrowUp') {
        handleMoveUp();
      } else if (e.key === 'ArrowDown') {
        handleMoveDown();
      }
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
  }));

  const { personalSettings } = usePersonalSettings()

  const getRowClassName = React.useCallback((row: GridRowType) => {
    return applyFormatCondition(row, perspective?.formatConditions)?.gridRowTextColor ?? '';
  }, [perspective?.formatConditions]);

  const showHorizontalBorder = React.useMemo(() => {
    const hasDescription = perspective?.attributes
      .some(attr => attr.attributeType === 'description' && !attr.invisibleInGrid);
    return perspective?.wrapEntityName || hasDescription;
  }, [perspective?.attributes, perspective?.wrapEntityName]);

  return (
    <div className={`flex flex-col ${className ?? ''}`}>
      {!personalSettings.hideGridButtons && (
        <div className="flex flex-wrap gap-1 items-center p-1">
          <Input.IconButton outline mini onClick={handleInsertRow}>行挿入(Enter)</Input.IconButton>
          <Input.IconButton outline mini onClick={handleInsertRowBelow}>下挿入(Ctrl + Enter)</Input.IconButton>
          <Input.IconButton outline mini onClick={handleDeleteRow}>行削除(Shift + Delete)</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini onClick={handleMoveUp}>上に移動(Alt + ↑)</Input.IconButton>
          <Input.IconButton outline mini onClick={handleMoveDown}>下に移動(Alt + ↓)</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini onClick={handleIndentDown}>インデント下げ(Shift + Tab)</Input.IconButton>
          <Input.IconButton outline mini onClick={handleIndentUp}>インデント上げ(Tab)</Input.IconButton>
        </div>
      )}
      <Layout.EditableGrid
        ref={gridRef}
        rows={fields}
        getColumnDefs={getColumnDefs}
        editorComponent={CellEditorWithMention}
        onChangeRow={onChangeRow}
        onActiveCellChanged={handleActiveCellChanged}
        onKeyDown={handleKeyDown}
        className="flex-1"
        storage={gridColumnStorage}
        getRowClassName={getRowClassName}
        showHorizontalBorder={showHorizontalBorder}
      />
    </div>
  );
});

/**
 * メンションを含むセル編集エディタ
 */
const CellEditorWithMention = React.forwardRef(({
  value,
  onChange,
  showOptions,
}: Layout.CellEditorTextareaProps, ref: React.ForwardedRef<Layout.CellEditorTextareaRef>) => {

  const textareaRef = React.useRef<HTMLTextAreaElement>(null);

  React.useImperativeHandle(ref, () => ({
    focus: () => textareaRef.current?.focus(),
    select: () => textareaRef.current?.select(),
    value: value ?? '',
  }), [value, onChange])

  return (
    <>
      <MentionTextarea
        ref={textareaRef}
        value={value ?? ''}
        onChange={onChange}
        className="flex-1 mx-[3px]"
      />
      {showOptions && (
        <Icon.ChevronDownIcon className="w-4 cursor-pointer" />
      )}
    </>
  )
})

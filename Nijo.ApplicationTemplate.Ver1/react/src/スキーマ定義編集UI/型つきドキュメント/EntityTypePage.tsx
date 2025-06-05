import * as React from 'react';
import * as Layout from '../../layout';
import { Entity, Perspective } from './types';

export interface EntityTypePageProps {
  perspectiveAttributes: Perspective['attributes'] | undefined;
  perspectiveId: string | undefined;
  rows: GridRowType[];
  onChangeRow: Layout.RowChangeEvent<GridRowType>;
  className?: string;
}

// グリッドの行の型
type GridRowType = Entity;

export const EntityTypePage = React.forwardRef<
  Layout.EditableGridRef<GridRowType>,
  EntityTypePageProps
>(({
  perspectiveAttributes,
  perspectiveId,
  rows,
  onChangeRow,
  className,
}, ref) => {
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
    if (perspectiveAttributes) {
      perspectiveAttributes.forEach((attrDef) => {
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
  }, [perspectiveAttributes]);

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
      <div className="flex-1 overflow-y-auto">
        <Layout.EditableGrid
          ref={ref}
          rows={rows}
          getColumnDefs={getColumnDefs}
          onChangeRow={onChangeRow}
          className="h-full border-y border-l border-gray-300"
        />
      </div>
    </div>
  );
});

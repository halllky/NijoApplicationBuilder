import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as ReactTable from '@tanstack/react-table';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { UUID } from 'uuidjs';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { SERVER_DOMAIN, NIJOUI_CLIENT_ROUTE_PARAMS } from '../routing';
import { TypedOutliner, OutlinerItem, OutlinerAttribute } from './types';
import { AttributeEditDialog, AttributeColumnRowType } from './AttributeEditDialog';

// グリッドの行の型
type GridRowType = OutlinerItem;

export const OutlinerPage = () => {
  const { [NIJOUI_CLIENT_ROUTE_PARAMS.OUTLINER_ID]: outlinerId } = ReactRouter.useParams();
  const [outlinerData, setOutlinerData] = React.useState<TypedOutliner | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const { pushDialog } = Layout.useDialogContext();

  const formMethods = ReactHookForm.useForm<TypedOutliner>({
    // defaultValues: outlinerData が読み込まれた後に設定
  });
  const { control, handleSubmit, reset, setValue, getValues } = formMethods;
  const { fields, insert, remove, update, append } = ReactHookForm.useFieldArray({
    control,
    name: 'items',
    keyName: 'uniqueId',
  });
  const { fields: attributeFields, replace: replaceAttributes } = ReactHookForm.useFieldArray({
    control,
    name: 'attributes',
    keyName: 'uniqueAttributeId',
  });

  // データ読み込み
  React.useEffect(() => {
    if (!outlinerId) {
      setError('outlinerIdが指定されていません。');
      setIsLoading(false);
      return;
    }

    const loadData = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await fetch(`${SERVER_DOMAIN}/typed-outliner/load?typeId=${outlinerId}`);
        if (!response.ok) {
          throw new Error(`データの読み込みに失敗しました: ${response.statusText}`);
        }
        const data: TypedOutliner = await response.json();
        setOutlinerData(data);
        reset(data);
      } catch (err) {
        if (err instanceof Error) {
          setError(err.message);
        } else {
          setError('不明なエラーが発生しました。');
        }
        setOutlinerData(null);
      } finally {
        setIsLoading(false);
      }
    };

    loadData();
  }, [outlinerId, reset]);

  // グリッドの列定義
  const getColumnDefs: Layout.GetColumnDefsFunction<GridRowType> = React.useCallback(cellType => {
    const columns: Layout.EditableGridColumnDef<GridRowType>[] = [];

    columns.push(
      cellType.text('itemName', '名称', {
        defaultWidth: 540,
        isFixed: true,
        renderCell: (context: ReactTable.CellContext<GridRowType, unknown>) => {
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

    attributeFields.forEach((attrField) => {
      columns.push(
        cellType.other(attrField.attributeName, {
          defaultWidth: 120,
          onStartEditing: e => {
            e.setEditorInitialValue(e.row.attributes[attrField.attributeId] ?? '');
          },
          onEndEditing: e => {
            const clone = window.structuredClone(e.row);
            if (e.value.trim() === '') {
              delete clone.attributes[attrField.attributeId];
            } else {
              clone.attributes[attrField.attributeId] = e.value;
            }
            e.setEditedRow(clone);
          },
          renderCell: context => {
            const value = context.row.original.attributes[attrField.attributeId];
            return <PlainCell>{value}</PlainCell>;
          },
        })
      );
    });

    return columns;
  }, [attributeFields]);


  const gridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null);

  const handleInsertRow = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange();
    const newRow: OutlinerItem = {
      itemId: UUID.generate(),
      itemName: '',
      indent: selectedRange && fields[selectedRange.startRow] ? fields[selectedRange.startRow].indent : 0,
      attributes: {},
    };
    if (!selectedRange) {
      insert(0, newRow);
    } else {
      insert(selectedRange.startRow, newRow);
    }
  });

  const handleInsertRowBelow = useEvent(() => {
    const selectedRange = gridRef.current?.getSelectedRange();
    const newRow: OutlinerItem = {
      itemId: UUID.generate(),
      itemName: '',
      indent: selectedRange && fields[selectedRange.endRow] ? fields[selectedRange.endRow].indent : 0,
      attributes: {},
    };
    if (!selectedRange) {
      append(newRow);
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
      update(x.rowIndex, x.newRow as OutlinerItem);
    }
  });

  const handleOpenAttributeEditDialog = useEvent(() => {
    const currentAttributes = getValues('attributes').map(attr => ({ ...attr, uniqueId: UUID.generate() } as AttributeColumnRowType));

    pushDialog({ title: '列の編集', className: "max-w-96 max-h-96" }, ({ closeDialog }) => (
      <AttributeEditDialog
        initialAttributes={currentAttributes}
        onApply={(newAttributes) => {
          const attributesToSave = newAttributes.map(({ uniqueId, ...rest }) => rest);
          replaceAttributes(attributesToSave);
          closeDialog();
        }}
        onCancel={closeDialog}
      />
    ))
  });

  const onSubmit = useEvent(async (data: TypedOutliner) => {
    if (!outlinerId) {
      setError('outlinerIdが指定されていません。保存できません。');
      return;
    }
    const dataToSave: TypedOutliner = {
      ...data,
      items: data.items.map(item => {
        const { uniqueId, ...rest } = item as any;
        return rest as OutlinerItem;
      }),
      attributes: data.attributes.map(attr => {
        const { uniqueAttributeId, ...rest } = attr as any;
        return rest as OutlinerAttribute;
      }),
    };

    try {
      const response = await fetch(`${SERVER_DOMAIN}/typed-outliner/save`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(dataToSave),
      });
      if (!response.ok) {
        throw new Error(`データの保存に失敗しました: ${response.statusText}`);
      }
      alert('保存しました。');
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('不明なエラーが発生しました。');
      }
    }
  });

  if (isLoading) {
    return <Layout.NowLoading />;
  }
  if (error) {
    return <div className="p-4 text-red-600">エラー: {error}</div>;
  }
  if (!outlinerData) {
    return <div className="p-4">データが見つかりません。</div>;
  }

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form onSubmit={handleSubmit(onSubmit)} className="h-full flex flex-col gap-1 pl-1 pt-1">
        <div className="flex flex-wrap gap-1 items-center">
          <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleInsertRow}>行挿入</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleInsertRowBelow}>下挿入</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.TrashIcon} onClick={handleDeleteRow}>行削除</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini icon={Icon.ChevronDoubleLeftIcon} onClick={handleIndentDown}>インデント下げ</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.ChevronDoubleRightIcon} onClick={handleIndentUp}>インデント上げ</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini icon={Icon.AdjustmentsHorizontalIcon} onClick={handleOpenAttributeEditDialog}>列の編集</Input.IconButton>
          <div className="flex-1"></div>
          <Input.IconButton submit={true} outline mini icon={Icon.ArrowDownOnSquareIcon} className="font-bold">保存</Input.IconButton>
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
      </form>
    </ReactHookForm.FormProvider>
  );
};

// -----------------------------

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

// ReactHookForm.FieldArrayWithId の型定義が GridRowType に合わない場合があるため、
// OutlinerItem に uniqueId を追加した型を useFieldArray に渡すことを検討。
// 今回は keyName を指定することで対応。
//
// また、react-hook-form の control下にある配列要素 (fields) は、
// OutlinerItem のプロパティに加えて `id` (useFieldArrayのデフォルトのkeyName) または
// 指定した `keyName` (今回は `uniqueId`) を持つオブジェクトになります。
// そのため、GridRowType は実質的に OutlinerItem & { uniqueId: string } のような形になります。
// 描画や更新時にはこの点を考慮する必要があります。
//
// `getColumnDefs` 内の `cellType.text` や `cellType.other` の第一引数 (accessorKey) は、
// `OutlinerItem` のプロパティ名と一致させる必要があります。
// 動的属性の場合、`attributes[attrField.attributeId]` のようにアクセスするため、
// accessorKey を直接指定するのではなく、`renderCell` などで値を取得・表示します。
// `cellType.other` の第一引数はヘッダー表示名として利用されます。

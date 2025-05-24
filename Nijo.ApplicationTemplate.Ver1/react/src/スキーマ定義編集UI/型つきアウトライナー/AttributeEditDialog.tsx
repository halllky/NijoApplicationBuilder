import * as React from 'react';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { UUID } from 'uuidjs';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { OutlinerAttribute } from './types';

export type AttributeColumnRowType = OutlinerAttribute & { uniqueId: string };

export const AttributeEditDialog = ({
  initialAttributes,
  onApply,
  onCancel,
}: {
  initialAttributes: AttributeColumnRowType[];
  onApply: (attributes: AttributeColumnRowType[]) => void;
  onCancel: () => void;
}) => {
  const formMethods = ReactHookForm.useForm<{ attributes: AttributeColumnRowType[] }>({
    defaultValues: { attributes: initialAttributes },
  });
  const { control, handleSubmit } = formMethods;
  const { fields, remove, update, move, append } = ReactHookForm.useFieldArray({
    control,
    name: 'attributes',
    keyName: 'uniqueId',
  });

  const attributeGridRef = React.useRef<Layout.EditableGridRef<AttributeColumnRowType>>(null);

  const getAttributeColumnDefs: Layout.GetColumnDefsFunction<AttributeColumnRowType> = React.useCallback(cellType => [
    cellType.text('attributeName', '列名', { defaultWidth: 200 }),
    cellType.other('操作', {
      defaultWidth: 120,
      renderCell: (context) => (
        <div className="flex gap-1">
          <Input.IconButton
            mini
            icon={Icon.ArrowUpIcon}
            onClick={() => move(context.row.index, context.row.index - 1)}
            disabled={context.row.index === 0}
          >
            上へ
          </Input.IconButton>
          <Input.IconButton
            mini
            icon={Icon.ArrowDownIcon}
            onClick={() => move(context.row.index, context.row.index + 1)}
            disabled={context.row.index === fields.length - 1}
          >
            下へ
          </Input.IconButton>
          <Input.IconButton
            mini
            icon={Icon.TrashIcon}
            onClick={() => remove(context.row.index)}
          >
            削除
          </Input.IconButton>
        </div>
      ),
    }),
  ], [remove, move, fields.length]);

  const handleApply = useEvent((data: { attributes: AttributeColumnRowType[] }) => {
    onApply(data.attributes);
  });

  const handleAddAttributeRow = useEvent(() => {
    const newAttributeName = window.prompt('新しい属性名を入力してください:');
    if (newAttributeName && newAttributeName.trim() !== '') {
      append({
        uniqueId: UUID.generate(),
        attributeId: UUID.generate(),
        attributeName: newAttributeName.trim(),
      } as AttributeColumnRowType);
    }
  });

  const handleChangeRow: Layout.RowChangeEvent<AttributeColumnRowType> = useEvent(e => {
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow as AttributeColumnRowType);
    }
  });

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form onSubmit={handleSubmit(handleApply)} className="h-full flex flex-col gap-2">
        <div className="flex-1 overflow-y-auto">
          <Layout.EditableGrid
            ref={attributeGridRef}
            rows={fields}
            getColumnDefs={getAttributeColumnDefs}
            onChangeRow={handleChangeRow}
            className="h-full border-y border-l border-gray-300"
          />
        </div>
        <div className="flex justify-between items-center p-1 border-t border-color-4">
          <Input.IconButton icon={Icon.PlusCircleIcon} onClick={handleAddAttributeRow}>列を追加</Input.IconButton>
          <div className="flex gap-2">
            <Input.IconButton onClick={onCancel}>キャンセル</Input.IconButton>
            <Input.IconButton submit={true} fill>適用</Input.IconButton>
          </div>
        </div>
      </form>
    </ReactHookForm.FormProvider>
  );
};

import * as React from 'react';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { UUID } from 'uuidjs';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { EntityType, EntityAttribute } from './types';

// ダイアログ内の属性グリッドの行の型
export type AttributeRowForEdit = EntityAttribute & { uniqueId: string };

export const EntityTypeEditDialog = ({
  initialEntityType,
  onApply,
  onCancel,
}: {
  initialEntityType: EntityType;
  onApply: (updatedEntityType: EntityType) => void;
  onCancel: () => void;
}) => {
  const formMethods = ReactHookForm.useForm<EntityType & { attributesGrid: AttributeRowForEdit[] }>({
    defaultValues: {
      ...initialEntityType,
      attributesGrid: initialEntityType.attributes.map(attr => ({ ...attr, uniqueId: UUID.generate() })),
    },
  });
  const { control, handleSubmit, watch } = formMethods;

  // 属性編集グリッド用の useFieldArray
  const { fields: attributeFields, remove, update, move, append } = ReactHookForm.useFieldArray({
    control,
    name: 'attributesGrid',
    keyName: 'uniqueId',
  });

  const attributeGridRef = React.useRef<Layout.EditableGridRef<AttributeRowForEdit>>(null);

  const getAttributeColumnDefs: Layout.GetColumnDefsFunction<AttributeRowForEdit> = React.useCallback(cellType => [
    cellType.text('attributeName', '属性名', { defaultWidth: 200 }),
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
            disabled={context.row.index === attributeFields.length - 1}
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
  ], [remove, move, attributeFields.length]);

  const handleApply = useEvent((formData: EntityType & { attributesGrid: AttributeRowForEdit[] }) => {
    const { attributesGrid, ...restOfEntityType } = formData;
    const finalAttributes = attributesGrid.map(({ uniqueId, ...attr }) => attr);
    const updatedEntityType = { ...restOfEntityType, attributes: finalAttributes };

    onApply(updatedEntityType);
  });

  const handleAddAttributeRow = useEvent(() => {
    const newAttributeName = window.prompt('新しい属性名を入力してください:');
    if (newAttributeName && newAttributeName.trim() !== '') {
      append({
        uniqueId: UUID.generate(),
        attributeId: UUID.generate(),
        attributeName: newAttributeName.trim(),
      });
    }
  });

  const handleChangeAttributeRow: Layout.RowChangeEvent<AttributeRowForEdit> = useEvent(e => {
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow as AttributeRowForEdit);
    }
  });

  // typeNameの変更を監視してフォームの値に反映（もし直接編集する場合）
  // ReactHookForm.Controllerを使ってInput.Textと連携させるのがより良い
  const typeNameValue = watch('typeName');

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form onSubmit={handleSubmit(handleApply)} className="h-full flex flex-col gap-2 p-2">
        <div>
          <label className="block text-sm font-medium text-gray-700">エンティティ型名:</label>
          <Input.Word<EntityType & { attributesGrid: AttributeRowForEdit[] }, "typeName">
            control={control}
            name={"typeName"}
            className="w-full"
          />
        </div>

        <div className="font-semibold mt-2">属性定義:</div>
        <div className="flex-1 overflow-y-auto border rounded">
          <Layout.EditableGrid
            ref={attributeGridRef}
            rows={attributeFields}
            getColumnDefs={getAttributeColumnDefs}
            onChangeRow={handleChangeAttributeRow}
            className="h-full"
          />
        </div>
        <div className="flex justify-between items-center p-1 border-t">
          <Input.IconButton icon={Icon.PlusCircleIcon} onClick={handleAddAttributeRow}>属性を追加</Input.IconButton>
          <div className="flex gap-2">
            <Input.IconButton onClick={onCancel}>キャンセル</Input.IconButton>
            <Input.IconButton submit={true} fill>適用</Input.IconButton>
          </div>
        </div>
      </form>
    </ReactHookForm.FormProvider>
  );
};

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { UUID } from 'uuidjs';

import * as Input from '../input';
import * as Layout from '../layout';
import { Perspective, EntityAttribute, FormatCondition, AVAILABLEFORMAT } from './types';

export type EntityTypeSettingsDialogProps = {
  initialEntityType: Perspective;
  onApply: (updatedEntityType: Perspective) => void;
  onCancel: () => void;
}

// ダイアログ内の属性グリッドの行の型
export type AttributeRowForEdit = EntityAttribute & { uniqueId: string };

export const EntityTypeEditDialog = ({
  initialEntityType,
  onApply,
  onCancel,
}: EntityTypeSettingsDialogProps) => {
  // フォーム
  const formMethods = ReactHookForm.useForm<Perspective & { attributesGrid: AttributeRowForEdit[] }>({
    defaultValues: {
      ...initialEntityType,
      attributesGrid: initialEntityType.attributes.map(attr => ({ ...attr, uniqueId: UUID.generate() })),
    },
  });
  const { register, control, handleSubmit, formState: { isDirty } } = formMethods;

  const attributeGridRef = React.useRef<Layout.EditableGridRef<AttributeRowForEdit>>(null);
  const { fields: attributeFields, remove, update, move, append } = ReactHookForm.useFieldArray({
    control,
    name: 'attributesGrid',
    keyName: 'uniqueId',
  });

  // 属性編集グリッド: 選択肢編集
  const [editingSelectOptionsAttributeIndex, setEditingSelectOptionsAttributeIndex] = React.useState<number | undefined>(undefined);
  const [editingSelectOptionsAttributeName, setEditingSelectOptionsAttributeName] = React.useState<string | undefined>(undefined);
  const [editingSelectOptions, setEditingSelectOptions] = React.useState<string[] | undefined>(undefined);
  const handleCancelEditingSelectOptions = useEvent(() => {
    setEditingSelectOptionsAttributeIndex(undefined);
    setEditingSelectOptionsAttributeName(undefined);
    setEditingSelectOptions(undefined);
  })
  const handleApplyEditingSelectOptions = useEvent((editedSelectOptions: string[]) => {
    if (editingSelectOptionsAttributeIndex !== undefined) {
      const row = attributeFields[editingSelectOptionsAttributeIndex];
      update(editingSelectOptionsAttributeIndex, { ...row, selectOptions: editedSelectOptions });
    }
    handleCancelEditingSelectOptions();
  })

  // 属性編集グリッド
  const getAttributeColumnDefs: Layout.GetColumnDefsFunction<AttributeRowForEdit> = React.useCallback(cellType => [
    cellType.text('attributeName', '属性名', { defaultWidth: 200 }),
    cellType.text('attributeType', '属性型', {
      defaultWidth: 100,
      getOptions: () => [
        { value: 'word', label: '単語' },
        { value: 'description', label: '文章' },
        { value: 'select', label: '選択' },
      ] satisfies { value: AttributeRowForEdit['attributeType'], label: string }[],
    }),
    cellType.other('', {
      defaultWidth: 140,
      renderCell: (context) => {
        const handleEditSelectOptions = () => {
          setEditingSelectOptionsAttributeIndex(context.row.index);
          setEditingSelectOptionsAttributeName(context.row.original.attributeName);
          setEditingSelectOptions(context.row.original.selectOptions ?? [])
        }

        if (context.row.original.attributeType !== 'select') {
          return undefined
        }
        return (
          <div className="w-full flex gap-1">
            <span className="truncate">
              {context.row.original.selectOptions?.join(', ')}
            </span>
            <Input.IconButton
              mini
              icon={Icon.PencilIcon}
              onClick={handleEditSelectOptions}
              hideText
            >
              編集
            </Input.IconButton>
          </div>
        )
      },
    }),
    cellType.boolean('invisibleInGrid', 'グリッドで非表示', {
      defaultWidth: 140,
    }),
    cellType.boolean('invisibleInDetail', '詳細欄で非表示', {
      defaultWidth: 140,
    }),
    cellType.other('操作', {
      defaultWidth: 180,
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
  ], [remove, move, attributeFields.length, setEditingSelectOptions]);

  const handleChangeAttributeRow: Layout.RowChangeEvent<AttributeRowForEdit> = useEvent(e => {
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow as AttributeRowForEdit);
    }
  });

  const handleAddAttributeRow = useEvent(() => {
    append({
      uniqueId: UUID.generate(),
      attributeId: UUID.generate(),
      attributeName: '',
      attributeType: 'word',
    })
  })

  // キャンセル
  const handleCancel = useEvent(() => {
    if (isDirty && !window.confirm('キャンセルしますか？')) return;
    onCancel()
  })

  // 適用
  const handleApply = useEvent((formData: Perspective & { attributesGrid: AttributeRowForEdit[] }) => {
    const { attributesGrid, ...restOfEntityType } = formData;
    const finalAttributes: EntityAttribute[] = attributesGrid.map(({ uniqueId, ...attr }) => attr);
    const updatedEntityType: Perspective = {
      ...restOfEntityType,
      attributes: finalAttributes,
    };

    onApply(updatedEntityType);
  });

  return (
    <Layout.ModalDialog open className="relative w-[90vw] h-[90vh] bg-white flex flex-col gap-1 relative border border-gray-400" onOutsideClick={handleCancel}>
      <ReactHookForm.FormProvider {...formMethods}>
        <form onSubmit={handleSubmit(handleApply)} className="h-full flex flex-col">

          <h1 className="font-bold select-none text-gray-700 px-8 py-1 border-b border-gray-200">
            ドキュメント詳細設定
          </h1>

          <div className="flex-1 overflow-y-auto px-8 pt-2 pb-32">
            <div className="flex items-center gap-1">
              <label className="basis-52 text-sm text-gray-500">ドキュメント名</label>
              <input type="text" {...register('name')} className="flex-1 px-1 border border-gray-400" />
            </div>

            <div className="flex items-center gap-1 mt-2">
              <label className="basis-52 text-sm text-gray-500">詳細画面での属性名の横幅</label>
              <input type="text" {...register('detailPageLabelWidth')} className="flex-1 px-1 border border-gray-400" />
            </div>

            <div className="flex flex-col mt-4 h-80 resize-y overflow-y-auto">
              <div className="flex items-center gap-1">
                <div className="text-sm text-gray-500">属性定義</div>
                <Input.IconButton icon={Icon.PlusCircleIcon} onClick={handleAddAttributeRow}>追加</Input.IconButton>
              </div>
              <Layout.EditableGrid
                ref={attributeGridRef}
                rows={attributeFields}
                getColumnDefs={getAttributeColumnDefs}
                onChangeRow={handleChangeAttributeRow}
                className="flex-1 border border-gray-400"
              />
            </div>

            <FormatConditionGrid
              formMethods={formMethods}
              className="mt-4 h-56 resize-y overflow-y-auto"
            />
          </div>

          <div className="flex justify-end items-center gap-4 py-2 px-8 border-t border-gray-200">
            <Input.IconButton onClick={handleCancel}>キャンセル</Input.IconButton>
            <Input.IconButton submit fill>適用</Input.IconButton>
          </div>
        </form>
      </ReactHookForm.FormProvider>

      {editingSelectOptions && (
        <SelectOptionsEditor
          attributeName={editingSelectOptionsAttributeName}
          defaultValues={editingSelectOptions}
          onApply={handleApplyEditingSelectOptions}
          onCancel={handleCancelEditingSelectOptions}
        />
      )}
    </Layout.ModalDialog>
  );
};

/**
 * select型の属性の選択肢を編集するダイアログ
 */
const SelectOptionsEditor = ({
  attributeName,
  defaultValues,
  onApply,
  onCancel,
}: {
  attributeName: string | undefined
  defaultValues: string[]
  onApply: (selectOptions: string[]) => void
  onCancel: () => void
}) => {

  type GridRowType = { value: string }

  const memorizedDefaultValues = React.useMemo(() => {
    return defaultValues.map(value => ({ value }))
  }, [defaultValues]);

  const formMethods = ReactHookForm.useForm<{ selectOptions: GridRowType[] }>({
    defaultValues: { selectOptions: memorizedDefaultValues },
  });
  const { fields, remove, update, move, append } = ReactHookForm.useFieldArray({
    control: formMethods.control,
    name: 'selectOptions',
  });

  const getColumnDefs: Layout.GetColumnDefsFunction<GridRowType> = React.useCallback(cellType => [
    cellType.text('value', attributeName ?? '', { defaultWidth: 240 }),
    cellType.other('', {
      defaultWidth: 180,
      renderCell: (context) => (
        <div className="flex gap-1">
          <Input.IconButton mini icon={Icon.ArrowUpIcon} onClick={() => move(context.row.index, context.row.index - 1)} />
          <Input.IconButton mini icon={Icon.ArrowDownIcon} onClick={() => move(context.row.index, context.row.index + 1)} />
          <Input.IconButton mini icon={Icon.TrashIcon} onClick={() => remove(context.row.index)} />
        </div>
      ),
    }),
  ], [attributeName, move, remove]);

  const handleChangeRow: Layout.RowChangeEvent<GridRowType> = useEvent(e => {
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow);
    }
  });

  const handleApply = useEvent(() => {
    const selectOptions = formMethods.getValues('selectOptions');
    onApply(selectOptions.map(x => x.value.trim()).filter(x => x !== ''));
  });

  return (
    <div className="absolute inset-0 z-10 flex justify-center items-center">
      <div className="w-md h-96 bg-white p-2 flex flex-col gap-1 border border-gray-500">
        <div className="flex items-center gap-1">
          <Input.IconButton icon={Icon.PlusCircleIcon} onClick={() => append({ value: '' })}>追加</Input.IconButton>
        </div>
        {/* グリッド */}
        <Layout.EditableGrid
          rows={fields}
          getColumnDefs={getColumnDefs}
          onChangeRow={handleChangeRow}
          className="flex-1"
        />
        <div className="flex justify-between items-center">
          <Input.IconButton onClick={onCancel}>キャンセル</Input.IconButton>
          <div className="flex-1"></div>
          <Input.IconButton fill onClick={handleApply}>適用</Input.IconButton>
        </div>
      </div>
    </div>
  )
}

/**
 * 書式条件を編集するグリッド
 */
const FormatConditionGrid = ({ formMethods, className }: {
  formMethods: ReactHookForm.UseFormReturn<Perspective & { attributesGrid: AttributeRowForEdit[] }>
  className?: string
}) => {
  const { fields, remove, update, move, append } = ReactHookForm.useFieldArray({
    control: formMethods.control,
    name: 'formatConditions',
  });

  const attributesGrid = ReactHookForm.useWatch({ name: 'attributesGrid', control: formMethods.control });
  const getColumnDefs: Layout.GetColumnDefsFunction<FormatCondition> = React.useCallback(cellType => [
    cellType.text('if.attributeId', '属性', {
      defaultWidth: 240,
      renderCell: (context) => {
        const attribute = attributesGrid.find(x => x.attributeId === context.row.original.if.attributeId);
        return (
          <div className="px-1 truncate">
            {attribute?.attributeName ?? context.row.original.if.attributeId} が
          </div>
        )
      },
      getOptions: () => attributesGrid.map(x => ({ label: x.attributeName, value: x.attributeId })),
    }),
    cellType.text('if.search', '検索文字列', {
      defaultWidth: 240,
      renderCell: (context) => {
        return (
          <div className="px-1 truncate">
            "{context.row.original.if.search}"
          </div>
        )
      },
    }),
    cellType.text('if.logic', '比較演算子', {
      defaultWidth: 160,
      renderCell: (context) => {
        let text = '';
        if (context.row.original.if.logic === 'equals') {
          text = 'と等しい場合：';
        } else if (context.row.original.if.logic === 'includes') {
          text = 'を含む場合：';
        } else if (context.row.original.if.logic === 'notEquals') {
          text = 'と等しくない場合：';
        } else if (context.row.original.if.logic === 'notIncludes') {
          text = 'を含まない場合：';
        } else {
          text = context.row.original.if.logic;
        }
        return (
          <div className="px-1 truncate">
            {text}
          </div>
        )
      },
      getOptions: () => [
        { value: 'equals', label: 'と等しい場合' },
        { value: 'includes', label: 'を含む場合' },
        { value: 'notEquals', label: 'と等しくない場合' },
        { value: 'notIncludes', label: 'を含まない場合' },
      ] satisfies { value: FormatCondition['if']['logic'], label: string }[],
    }),
    cellType.text('then.gridRowTextColor', 'グリッドのテキスト色', {
      defaultWidth: 180,
      renderCell: (context) => {
        const textColor = context.row.original.then.gridRowTextColor;
        return (
          <div className={`w-full px-1 truncate ${textColor ?? ''}`}>
            {textColor}
          </div>
        )
      },
      getOptions: () => AVAILABLEFORMAT.GRID_TEXT_COLOR.map(color => ({ label: color, value: color })),
    }),
    cellType.text('then.graphNodeColor', 'グラフのノードのスタイル', {
      defaultWidth: 240,
      renderCell: (context) => {
        const graphNodeColor = context.row.original.then.graphNodeColor;
        let styleName = '';
        if (context.row.original.then.invisibleInGraph) {
          styleName = '非表示';
        } else if (graphNodeColor) {
          styleName = Object.entries(AVAILABLEFORMAT.GRAPH_NODE_COLOR)
            .find(([key, value]) => value === graphNodeColor)
            ?.[0] ?? '';
          if (!styleName) styleName = `color: ${graphNodeColor}`;
        }
        return (
          <div
            className={`w-full px-1 truncate`}
            style={{
              color: graphNodeColor,
              backgroundColor: graphNodeColor
                ? `${graphNodeColor}44` // EditableGridのアクティブセルのカーソルが見えなくなるのを避けるため半透明にする
                : undefined,
            }}
          >
            {styleName}
          </div>
        )
      },
      onEndEditing: ev => {
        if (!ev.value) {
          ev.setEditedRow({ ...ev.row, then: { ...ev.row.then, graphNodeColor: undefined } });
        } else if (ev.value === GRAPH_INVISIBLE) {
          ev.setEditedRow({ ...ev.row, then: { ...ev.row.then, graphNodeColor: undefined, invisibleInGraph: true } });
        } else {
          const selectedValue = AVAILABLEFORMAT.GRAPH_NODE_COLOR[ev.value as keyof typeof AVAILABLEFORMAT.GRAPH_NODE_COLOR];
          if (selectedValue) ev.setEditedRow({ ...ev.row, then: { ...ev.row.then, graphNodeColor: selectedValue, invisibleInGraph: false } });
        }
      },
      getOptions: () => [
        { label: '非表示', value: GRAPH_INVISIBLE },
        ...Object.keys(AVAILABLEFORMAT.GRAPH_NODE_COLOR).map(label => ({ label, value: label })),
      ],
    }),
  ], [attributesGrid]);

  const handleAddFormatCondition = useEvent(() => {
    append({
      if: { attributeId: '', logic: 'equals', search: '' },
      then: { gridRowTextColor: '' },
    })
  })

  const handleChangeRow: Layout.RowChangeEvent<FormatCondition> = useEvent(e => {
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow);
    }
  });

  return (
    <div className={`flex flex-col gap-1 ${className ?? ''}`}>
      <div className="flex items-center gap-1">
        <div className="text-sm text-gray-500">
          書式条件（先頭のものほど優先）
        </div>
        <Input.IconButton icon={Icon.PlusCircleIcon} onClick={handleAddFormatCondition}>追加</Input.IconButton>
      </div>
      <Layout.EditableGrid
        rows={fields}
        getColumnDefs={getColumnDefs}
        onChangeRow={handleChangeRow}
        className="flex-1 border border-gray-400"
      />
    </div>
  )
}

const GRAPH_INVISIBLE = '::invisibleInGraph::'

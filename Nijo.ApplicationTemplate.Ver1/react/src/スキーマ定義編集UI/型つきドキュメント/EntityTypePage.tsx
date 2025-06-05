import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { UUID } from 'uuidjs';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { NIJOUI_CLIENT_ROUTE_PARAMS } from '../routing';
import { EntityType, EntityTypePageData, Entity, EntityAttribute } from './types';
import { NijoUiOutletContextType } from '../types';
import { EntityTypeEditDialog } from './EntityTypeEditDialog';

// グリッドの行の型
type GridRowType = Entity & { uniqueId: string };

export const EntityTypePage = () => {
  const { [NIJOUI_CLIENT_ROUTE_PARAMS.ENTITY_TYPE_ID]: entityTypeId } = ReactRouter.useParams();
  const { typedDoc } = ReactRouter.useOutletContext<NijoUiOutletContextType>();

  const [currentEntityType, setCurrentEntityType] = React.useState<EntityType | null>(null);
  const [entitiesForGrid, setEntitiesForGrid] = React.useState<GridRowType[]>([]);
  const [isLoading, setIsLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const { pushDialog } = Layout.useDialogContext();

  const formMethods = ReactHookForm.useForm<{ entities: GridRowType[] }>();
  const { control, handleSubmit, reset, formState: { isDirty } } = formMethods;
  const { fields, insert, remove, update, append } = ReactHookForm.useFieldArray({
    control,
    name: 'entities',
    keyName: 'uniqueId',
  });

  // データ読み込み
  React.useEffect(() => {
    const loadData = async () => {
      setIsLoading(true);
      setError(null);
      if (!entityTypeId) {
        setError('entityTypeIdが指定されていません。');
        setIsLoading(false);
        setEntitiesForGrid([]);
        reset({ entities: [] });
        return;
      }
      try {
        const pageData = await typedDoc.loadEntityTypePageData(entityTypeId);
        if (pageData) {
          setCurrentEntityType(pageData.entityType);
          setEntitiesForGrid(pageData.entities.map(e => ({ ...e, uniqueId: UUID.generate() })));
          reset({ entities: pageData.entities.map(d => ({ ...d, uniqueId: UUID.generate() })) });
        } else {
          setCurrentEntityType(null);
          setEntitiesForGrid([]);
          reset({ entities: [] });
        }
      } catch (err) {
        if (err instanceof Error) {
          setError(err.message);
        } else {
          setError('不明なエラーが発生しました。');
        }
        setEntitiesForGrid([]);
      } finally {
        setIsLoading(false);
      }
    };
    loadData();
  }, [typedDoc, reset, entityTypeId]);

  // 保存処理
  const onSubmit = useEvent(async (data: { entities: GridRowType[] }) => {
    setIsLoading(true);
    setError(null);
    if (!entityTypeId || !currentEntityType) {
      setError('entityTypeIdまたはエンティティ型情報が読み込まれていません。保存できません。');
      setIsLoading(false);
      return;
    }
    try {
      const entitiesToSave: Entity[] = data.entities.map(({ uniqueId, ...rest }) => rest);
      const pageDataToSave: EntityTypePageData = {
        entityType: currentEntityType,
        entities: entitiesToSave,
      };
      await typedDoc.saveEntities(pageDataToSave);
      const reloadedPageData = await typedDoc.loadEntityTypePageData(entityTypeId);
      if (reloadedPageData) {
        setCurrentEntityType(reloadedPageData.entityType);
        setEntitiesForGrid(reloadedPageData.entities.map(e => ({ ...e, uniqueId: UUID.generate() })));
        reset({ entities: reloadedPageData.entities.map(d => ({ ...d, uniqueId: UUID.generate() })) });
      } else {
        setCurrentEntityType(null);
        setEntitiesForGrid([]);
        reset({ entities: [] });
      }
      alert('保存しました。');
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('不明なエラーが発生しました。');
      }
    } finally {
      setIsLoading(false);
    }
  });

  // 画面離脱防止
  const blocker = ReactRouter.useBlocker(
    ({ currentLocation, nextLocation }) =>
      isDirty && currentLocation.pathname !== nextLocation.pathname
  );
  React.useEffect(() => {
    if (blocker && blocker.state === "blocked") {
      if (window.confirm("編集中の内容がありますが、ページを離れてもよろしいですか？")) {
        blocker.proceed();
      } else {
        blocker.reset();
      }
    }
  }, [blocker]);

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
    if (currentEntityType && currentEntityType.attributes) {
      currentEntityType.attributes.forEach((attrDef) => {
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
  }, [currentEntityType]);

  const gridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null);

  const handleInsertRow = useEvent(() => {
    const newRow: GridRowType = {
      entityId: UUID.generate(),
      typeId: entityTypeId,
      entityName: '',
      indent: 0,
      attributeValues: {},
      comments: [],
      uniqueId: UUID.generate(),
    };
    const selectedRange = gridRef.current?.getSelectedRange();
    if (!selectedRange) {
      insert(0, newRow);
    } else {
      insert(selectedRange.startRow, newRow);
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
      update(x.rowIndex, x.newRow as GridRowType);
    }
  });

  const handleOpenEntityTypeEditDialog = useEvent(() => {
    if (!currentEntityType) {
      alert("エンティティ型が選択されていません。");
      return;
    }

    pushDialog({ title: 'エンティティ型の編集', className: "max-w-lg max-h-[80vh]" }, ({ closeDialog }) => (
      <EntityTypeEditDialog
        initialEntityType={currentEntityType}
        onApply={(updatedEntityType) => {
          setCurrentEntityType(updatedEntityType);
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

  if (isLoading) {
    return <Layout.NowLoading />;
  }
  if (error) {
    return <div className="p-4 text-red-600">エラー: {error}</div>;
  }

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form onSubmit={handleSubmit(onSubmit)} className="h-full flex flex-col gap-1 pl-1 pt-1">
        <div className="flex flex-wrap gap-1 items-center">
          <Input.IconButton outline mini icon={Icon.PlusIcon} onClick={handleInsertRow}>行挿入</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.TrashIcon} onClick={handleDeleteRow}>行削除</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini icon={Icon.ChevronDoubleLeftIcon} onClick={handleIndentDown}>インデント下げ</Input.IconButton>
          <Input.IconButton outline mini icon={Icon.ChevronDoubleRightIcon} onClick={handleIndentUp}>インデント上げ</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini icon={Icon.PencilSquareIcon} onClick={handleOpenEntityTypeEditDialog}>型定義編集</Input.IconButton>
          <div className="flex-1"></div>
          <Input.IconButton outline mini onClick={() => console.log(JSON.parse(localStorage.getItem('typedDocument') ?? '{}'))}>（デバッグ用）console.log</Input.IconButton>
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

import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { UUID } from 'uuidjs';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { NIJOUI_CLIENT_ROUTE_PARAMS } from '../routing';
import { NijoUiOutletContextType } from '../types';
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels';
import { PerspectivePageGraph } from './PerspectivePage.Graph';
import { EntityTypePage } from './EntityTypePage';
import { EntityDetailPane } from './EntityDetailPane';
import { EntityTypeEditDialog } from './EntityTypeEditDialog';
import { Entity, Perspective, PerspectivePageData } from './types';

export const PerspectivePage = () => {
  const { [NIJOUI_CLIENT_ROUTE_PARAMS.PERSPECTIVE_ID]: perspectiveId } = ReactRouter.useParams();
  const [isLoaded, setIsLoaded] = React.useState(false);
  const [isSaving, setIsSaving] = React.useState(false);
  const [defaultValues, setDefaultValues] = React.useState<PerspectivePageData | null>(null);
  const [error, setError] = React.useState<string | null>(null);

  const {
    typedDoc: {
      isReady,
      loadPerspectivePageData,
      savePerspective,
    }
  } = ReactRouter.useOutletContext<NijoUiOutletContextType>();

  // データ読み込み
  React.useEffect(() => {
    (async () => {
      if (!isReady) return;
      setIsLoaded(false);
      setError(null);
      if (!perspectiveId) {
        setError('perspectiveIdが指定されていません。');
        setIsLoaded(true);
        return;
      }
      const pageData = await loadPerspectivePageData(perspectiveId);
      if (!pageData) {
        setError(`指定されたPerspectiveが見つかりません: ${perspectiveId}`);
        setIsLoaded(true);
        return;
      }
      setDefaultValues(pageData); // 読み込んだデータでフォームをリセット
      setIsLoaded(true);
    })()
  }, [isReady, perspectiveId]);

  // 保存処理
  const onSubmit = useEvent(async (data: PerspectivePageData) => {
    setIsSaving(true);
    setError(null);

    // 保存
    await savePerspective(data);

    // 保存後再読み込み
    if (perspectiveId) {
      const reloadedPageData = await loadPerspectivePageData(perspectiveId);
      if (reloadedPageData) {
        setDefaultValues(reloadedPageData);
      }
    }
    alert('保存しました。');
    setIsSaving(false);
  });

  if (!isReady || !isLoaded || isSaving) {
    return <Layout.NowLoading />;
  }
  if (error) {
    return <div className="p-4 text-red-600">エラー: {error}</div>
  }
  if (!defaultValues) {
    return <div className="p-4">データが見つかりません。</div>
  }
  return (
    <AfterLoaded
      key={perspectiveId}
      defaultValues={defaultValues}
      onSubmit={onSubmit}
    />
  )
}

/**
 * データ読み込み後のフォーム
 */
export const AfterLoaded = ({ defaultValues, onSubmit }: {
  defaultValues: PerspectivePageData
  onSubmit: (data: PerspectivePageData) => Promise<void>
}) => {
  const formMethods = ReactHookForm.useForm<PerspectivePageData>({ defaultValues });
  const { handleSubmit, formState: { isDirty }, getValues, control, setValue, watch } = formMethods;
  const { pushDialog } = Layout.useDialogContext()
  const [selectedEntityIndex, setSelectedEntityIndex] = React.useState<number>()
  const gridRef = React.useRef<Layout.EditableGridRef<GridRowType>>(null)

  // グリッドの行の型 (EntityTypePageから移動してきたGridRowType相当)
  type GridRowType = Entity;

  const { fields, insert, remove, update } = ReactHookForm.useFieldArray({
    control,
    name: 'perspective.nodes',
    keyName: 'uniqueId',
  });

  const perspective = watch('perspective')

  // EntityTypePageから移動してきたハンドラ群
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

  const handleOpenEntityTypeEditDialog = useEvent(() => {
    if (!perspective) {
      alert("エンティティ型が選択されていません。");
      return;
    }
    pushDialog({ title: 'エンティティ型の編集', className: "max-w-lg max-h-[80vh]" }, ({ closeDialog }) => (
      <EntityTypeEditDialog
        initialEntityType={perspective}
        onApply={(updatedEntityType) => {
          setValue('perspective', updatedEntityType);
          closeDialog();
        }}
        onCancel={closeDialog}
      />
    ));
  });

  const handleChangeRow: Layout.RowChangeEvent<GridRowType> = useEvent(e => { // 追加
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow);
    }
  });

  const handleEntityChangedInDetailPage = useEvent((entity: Entity) => {
    if (selectedEntityIndex === undefined) return;
    update(selectedEntityIndex, entity);
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

  const handleNodeDoubleClick = useEvent((nodeId: string) => {
    const nodes = getValues('perspective.nodes');
    const rowIndex = nodes.findIndex(n => n.entityId === nodeId);
    if (rowIndex === -1 || !gridRef.current) {
      setSelectedEntityIndex(undefined);
    } else {
      gridRef.current.selectRow(rowIndex, rowIndex);
      setSelectedEntityIndex(rowIndex);
    }
  });

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form onSubmit={handleSubmit(onSubmit)} className="h-full flex flex-col gap-1 pl-1 pt-1">
        <div className="flex flex-wrap gap-1 items-center mb-2">
          <div className="flex-1 font-semibold">{getValues('perspective.name')}</div>
          <Input.IconButton outline mini onClick={() => console.log(JSON.parse(localStorage.getItem('typedDocument') ?? '{}'))}>（デバッグ用）console.log</Input.IconButton>
          <Input.IconButton outline mini hideText icon={Icon.PlusIcon} onClick={handleInsertRow}>行挿入</Input.IconButton>
          <Input.IconButton outline mini hideText icon={Icon.PlusIcon} onClick={handleInsertRowBelow}>下挿入</Input.IconButton>
          <Input.IconButton outline mini hideText icon={Icon.TrashIcon} onClick={handleDeleteRow}>行削除</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini hideText icon={Icon.ChevronDoubleLeftIcon} onClick={handleIndentDown}>インデント下げ</Input.IconButton>
          <Input.IconButton outline mini hideText icon={Icon.ChevronDoubleRightIcon} onClick={handleIndentUp}>インデント上げ</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini hideText icon={Icon.PencilSquareIcon} onClick={handleOpenEntityTypeEditDialog}>型定義編集</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton submit={true} outline mini icon={Icon.ArrowDownOnSquareIcon} className="font-bold">保存</Input.IconButton>
        </div>

        <PanelGroup direction="horizontal">

          <Panel collapsible minSize={12}>
            <PanelGroup direction="vertical">

              {/* グラフ */}
              <Panel collapsible minSize={12}>
                <PerspectivePageGraph
                  formMethods={formMethods}
                  onNodeDoubleClick={handleNodeDoubleClick}
                  className="h-full border border-gray-300"
                />
              </Panel>

              <PanelResizeHandle className="h-1" />

              {/* グリッド */}
              <Panel collapsible minSize={12}>
                <EntityTypePage
                  ref={gridRef}
                  perspectiveAttributes={perspective?.attributes}
                  perspectiveId={perspective?.perspectiveId}
                  rows={fields}
                  onChangeRow={handleChangeRow}
                  onSelectedRowChanged={setSelectedEntityIndex}
                  className="h-full"
                />
              </Panel>
            </PanelGroup>
          </Panel>

          <PanelResizeHandle className="w-1" />

          {/* 詳細画面 */}
          <Panel collapsible minSize={12}>
            {selectedEntityIndex !== undefined && (
              <EntityDetailPane
                key={fields[selectedEntityIndex].entityId}
                entity={fields[selectedEntityIndex]}
                onEntityChanged={handleEntityChangedInDetailPage}
                perspective={perspective}
                entityIndex={selectedEntityIndex}
              />
            )}
          </Panel>

        </PanelGroup>

      </form>
    </ReactHookForm.FormProvider>
  );
};

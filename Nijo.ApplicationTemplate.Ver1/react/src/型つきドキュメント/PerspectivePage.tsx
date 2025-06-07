import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';
import { UUID } from 'uuidjs';

import * as Input from '../input';
import * as Layout from '../layout';
import { NIJOUI_CLIENT_ROUTE_PARAMS } from '../routes';
import { NijoUiOutletContextType } from '../スキーマ定義編集UI/types';
import { Panel, PanelGroup, PanelGroupStorage, PanelResizeHandle } from 'react-resizable-panels';
import { PerspectivePageGraph } from './PerspectivePage.Graph';
import { EntityTypePage, EntityTypePageRef } from './PerspectivePage.Grid';
import { EntityDetailPane } from './PerspectivePage.Details';
import { EntityTypeEditDialog } from './PerspectivePage.Settings';
import { Entity, Perspective, PerspectivePageData } from './types';

export const PerspectivePage = () => {
  const { [NIJOUI_CLIENT_ROUTE_PARAMS.PERSPECTIVE_ID]: perspectiveId } = ReactRouter.useParams();
  const [isLoaded, setIsLoaded] = React.useState(false);
  const [isSaving, setIsSaving] = React.useState(false);
  const [defaultValues, setDefaultValues] = React.useState<PerspectivePageData | null>(null);
  const [error, setError] = React.useState<string | null>(null);

  const [afterLoadedRef, setAfterLoadedRef] = React.useState<AfterLoadedRef | null>(null);
  const afterLoadedRefCallback = React.useCallback((ref: AfterLoadedRef) => {
    setAfterLoadedRef(ref);
  }, []);

  // クエリパラメータでフォーカス対象が指定されている場合はそのエンティティを選択する
  const [searchParams] = ReactRouter.useSearchParams();
  React.useEffect(() => {
    if (!afterLoadedRef) return;

    const focusEntityId = searchParams.get(NIJOUI_CLIENT_ROUTE_PARAMS.FOCUS_ENTITY_ID);
    if (!focusEntityId) return;
    afterLoadedRef.selectEntity(focusEntityId);
  }, [afterLoadedRef, searchParams]);

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
      ref={afterLoadedRefCallback}
      key={perspectiveId}
      defaultValues={defaultValues}
      onSubmit={onSubmit}
    />
  )
}

// --------------------------------------

type AfterLoadedProps = {
  defaultValues: PerspectivePageData
  onSubmit: (data: PerspectivePageData) => Promise<void>
}

type AfterLoadedRef = {
  selectEntity: (entityId: string) => void
}

/**
 * データ読み込み後のフォーム
 */
export const AfterLoaded = React.forwardRef<AfterLoadedRef, AfterLoadedProps>(({ defaultValues, onSubmit }, ref) => {
  const formMethods = ReactHookForm.useForm<PerspectivePageData>({ defaultValues });
  const { handleSubmit, formState: { isDirty }, getValues, control, setValue, watch } = formMethods;
  const { pushDialog } = Layout.useDialogContext()
  const [selectedEntityIndex, setSelectedEntityIndex] = React.useState<number>()
  const gridRef = React.useRef<EntityTypePageRef>(null)

  // グリッドの行の型 (EntityTypePageから移動してきたGridRowType相当)
  type GridRowType = Entity;

  const useFieldArrayReturn = ReactHookForm.useFieldArray({
    control,
    name: 'perspective.nodes',
    keyName: 'uniqueId',
  });
  const { fields, insert, remove, update, move } = useFieldArrayReturn;

  const perspective = watch('perspective')

  // 型定義編集ダイアログを開く
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

  // グリッドの行変更時イベント
  const handleChangeRow: Layout.RowChangeEvent<GridRowType> = useEvent(e => {
    for (const x of e.changedRows) {
      update(x.rowIndex, x.newRow);
    }
  });

  // 詳細画面でのエンティティ変更時イベント
  const handleEntityChangedInDetailPage = useEvent((entity: Entity) => {
    if (selectedEntityIndex === undefined) return;
    update(selectedEntityIndex, entity);
  });

  // ---------------------------------------
  // キーボードイベント
  const handleKeyDown = useEvent((e: React.KeyboardEvent<HTMLFormElement>) => {
    // 保存
    if (e.ctrlKey && e.key === 's') {
      e.preventDefault();
      onSubmit(getValues());
    }
  });

  // ---------------------------------------
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

  // ---------------------------------------
  // フォーカス

  // グラフのノードがダブルクリックされたらそのノードに紐づくエンティティを選択
  const handleNodeDoubleClick = useEvent((nodeId: string) => {
    const rowIndex = getValues('perspective.nodes').findIndex(n => n.entityId === nodeId);
    if (rowIndex === -1 || !gridRef.current) {
      setSelectedEntityIndex(undefined);
    } else {
      gridRef.current.selectRow(rowIndex, rowIndex);
      setSelectedEntityIndex(rowIndex);
    }
  });

  React.useImperativeHandle(ref, () => ({
    selectEntity: (entityId: string) => {
      const rowIndex = getValues('perspective.nodes').findIndex(n => n.entityId === entityId);
      if (rowIndex === -1 || !gridRef.current) {
        setSelectedEntityIndex(undefined);
      } else {
        gridRef.current.selectRow(rowIndex, rowIndex);
        setSelectedEntityIndex(rowIndex);
      }
    }
  }), [gridRef, getValues]);

  // ---------------------------------------

  // パネルのサイズを保存する
  const panelStorage = React.useMemo<PanelGroupStorage>(() => ({
    getItem: (key: string) => getValues(`perspective.resizablePaneState.${key}`),
    setItem: (key: string, value: string) => setValue(`perspective.resizablePaneState.${key}`, value),
  }), [getValues, setValue]);

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form
        onSubmit={handleSubmit(onSubmit)}
        onKeyDown={handleKeyDown}
        className="h-full flex flex-col gap-1 pl-1 pt-1"
      >
        <div className="flex flex-wrap gap-1 items-center mb-2">
          <div className="font-semibold">{getValues('perspective.name')}</div>
          <Input.IconButton hideText onClick={handleOpenEntityTypeEditDialog} icon={Icon.PencilSquareIcon}>型定義編集</Input.IconButton>
          <div className="flex-1"></div>
          <Input.IconButton outline mini onClick={gridRef.current?.insertRow}>行挿入(Enter)</Input.IconButton>
          <Input.IconButton outline mini onClick={gridRef.current?.insertRowBelow}>下挿入(Ctrl + Enter)</Input.IconButton>
          <Input.IconButton outline mini onClick={gridRef.current?.deleteRow}>行削除(Shift + Delete)</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini onClick={gridRef.current?.moveUp}>上に移動(Alt + ↑)</Input.IconButton>
          <Input.IconButton outline mini onClick={gridRef.current?.moveDown}>下に移動(Alt + ↓)</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton outline mini onClick={gridRef.current?.indentDown}>インデント下げ(Shift + Tab)</Input.IconButton>
          <Input.IconButton outline mini onClick={gridRef.current?.indentUp}>インデント上げ(Tab)</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton submit outline mini>保存(Ctrl + S)</Input.IconButton>
        </div>

        <PanelGroup direction="horizontal" autoSaveId="page-root-horizontal" storage={panelStorage}>

          <Panel collapsible minSize={12}>
            <PanelGroup direction="vertical" autoSaveId="page-root-vertical" storage={panelStorage}>

              {/* グリッド */}
              <Panel collapsible minSize={12}>
                <EntityTypePage
                  ref={gridRef}
                  useFieldArrayReturn={useFieldArrayReturn}
                  perspective={perspective}
                  onChangeRow={handleChangeRow}
                  onSelectedRowChanged={setSelectedEntityIndex}
                  setValue={setValue}
                  className="h-full"
                />
              </Panel>

              <PanelResizeHandle className="h-1" />

              {/* グラフ */}
              <Panel collapsible minSize={12}>
                <PerspectivePageGraph
                  formMethods={formMethods}
                  onNodeDoubleClick={handleNodeDoubleClick}
                  className="h-full border border-gray-300"
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
});

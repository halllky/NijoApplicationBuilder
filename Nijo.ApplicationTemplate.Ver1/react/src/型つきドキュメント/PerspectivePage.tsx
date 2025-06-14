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
import { Panel, PanelGroup, PanelGroupProps, PanelGroupStorage, PanelResizeHandle } from 'react-resizable-panels';
import { PerspectivePageGraph } from './PerspectivePage.Graph';
import { EntityTypePage, EntityTypePageRef } from './PerspectivePage.Grid';
import { EntityDetailPane } from './PerspectivePage.Details';
import { EntityTypeEditDialog, EntityTypeSettingsDialogProps } from './PerspectivePage.Settings';
import { Entity, Perspective, PerspectivePageData } from './types';
import { ToTopPageButton } from '../スキーマ定義編集UI/ToTopPageButton';

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
    if (isSaving) return;
    setIsSaving(true);
    setError(null);

    // 保存
    try {
      const isSaved = await savePerspective(data);
      if (!isSaved) return;
      afterLoadedRef?.saveSucceeded();

    } finally {
      setIsSaving(false);
    }
  });

  if (!isReady || !isLoaded) {
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
  saveSucceeded: () => void
}

/**
 * データ読み込み後のフォーム
 */
export const AfterLoaded = React.forwardRef<AfterLoadedRef, AfterLoadedProps>(({ defaultValues, onSubmit }, ref) => {
  const formMethods = ReactHookForm.useForm<PerspectivePageData>({ defaultValues });
  const { handleSubmit, formState: { isDirty }, getValues, control, setValue, watch } = formMethods;
  const [selectedEntityIndex, setSelectedEntityIndex] = React.useState<number>()
  const gridRef = React.useRef<EntityTypePageRef>(null)

  // グラフの表示方向
  const graphViewPosition = ReactHookForm.useWatch({ name: 'perspective.graphViewPosition', control })
  const handleClickGraphHorizontal = useEvent(() => {
    setValue('perspective.graphViewPosition', 'horizontal');
  });
  const handleClickGraphVertical = useEvent(() => {
    setValue('perspective.graphViewPosition', 'vertical');
  });

  // グリッドの行の型 (EntityTypePageから移動してきたGridRowType相当)
  type GridRowType = Entity;

  const useFieldArrayReturn = ReactHookForm.useFieldArray({
    control,
    name: 'perspective.nodes',
    keyName: 'uniqueId',
  });
  const { fields, insert, remove, update, move } = useFieldArrayReturn;

  const perspective = watch('perspective')

  // 型定義編集ダイアログ
  const [entityTypeSettingsDialogProps, setEntityTypeSettingsDialogProps] = React.useState<EntityTypeSettingsDialogProps | undefined>(undefined);
  const handleOpenEntityTypeEditDialog = useEvent(() => {
    if (!perspective) {
      alert("エンティティ型が選択されていません。");
      return;
    }
    setEntityTypeSettingsDialogProps({
      initialEntityType: perspective,
      onApply: (updatedEntityType) => {
        setValue('perspective', updatedEntityType);
        setEntityTypeSettingsDialogProps(undefined);
      },
      onCancel: () => {
        setEntityTypeSettingsDialogProps(undefined);
      },
    })
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

  const [showSaveSuccessText, setShowSaveSuccessText] = React.useState(false);

  React.useImperativeHandle(ref, () => ({
    selectEntity: (entityId: string) => {
      const rowIndex = getValues('perspective.nodes').findIndex(n => n.entityId === entityId);
      if (rowIndex === -1 || !gridRef.current) {
        setSelectedEntityIndex(undefined);
      } else {
        gridRef.current.selectRow(rowIndex, rowIndex);
        setSelectedEntityIndex(rowIndex);
      }
    },
    saveSucceeded: () => {
      setShowSaveSuccessText(true);
      setTimeout(() => {
        setShowSaveSuccessText(false);
      }, 1000);
    }
  }), [gridRef, getValues]);

  // ---------------------------------------

  // パネルのサイズを保存する
  const panelStorage = React.useMemo<PanelGroupStorage>(() => ({
    getItem: (key: string) => getValues(`perspective.resizablePaneState.${key}`),
    setItem: (key: string, value: string) => setValue(`perspective.resizablePaneState.${key}`, value),
  }), [getValues, setValue]);

  // 詳細パネル
  const [detailPanelCollapsed, setDetailPanelCollapsed] = React.useState(false);

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form
        onSubmit={handleSubmit(onSubmit)}
        onKeyDown={handleKeyDown}
        tabIndex={0} // keydownイベントを拾うため
        className="h-full flex flex-col gap-1 outline-none"
      >
        <div className="flex flex-wrap gap-1 items-center p-1">
          <ToTopPageButton />
          <Icon.ChevronRightIcon className="w-4 h-4" />
          <h1>
            {getValues('perspective.name')}
          </h1>

          <div className="basis-1"></div>
          <Input.IconButton onClick={handleOpenEntityTypeEditDialog} icon={Icon.PencilSquareIcon}>設定</Input.IconButton>
          <div className="flex-1"></div>
          <div className="flex items-center">
            <Input.IconButton onClick={handleClickGraphHorizontal} icon={Icon.Bars2Icon} hideText outline={graphViewPosition === 'horizontal'} className="p-1">グラフをグリッドの横に表示</Input.IconButton>
            <Input.IconButton onClick={handleClickGraphVertical} icon={Icon.PauseIcon} hideText outline={graphViewPosition !== 'horizontal'} className="p-1">グラフをグリッドの下に表示</Input.IconButton>
          </div>
          <div className="basis-28 flex justify-end">
            <Input.IconButton submit fill mini>
              {showSaveSuccessText ? '保存しました。' : '保存(Ctrl + S)'}
            </Input.IconButton>
          </div>
        </div>

        <VerticalOrHorizontalLayout
          graphViewPosition={graphViewPosition}
          panelStorage={panelStorage}
          detailPanelCollapsed={detailPanelCollapsed}
          onDetailPanelCollapsedChanged={setDetailPanelCollapsed}
          grid={className => (
            <EntityTypePage
              ref={gridRef}
              useFieldArrayReturn={useFieldArrayReturn}
              perspective={perspective}
              onChangeRow={handleChangeRow}
              onSelectedRowChanged={setSelectedEntityIndex}
              setValue={setValue}
              className={className}
            />
          )}
          graph={className => (
            <PerspectivePageGraph
              formMethods={formMethods}
              onNodeDoubleClick={handleNodeDoubleClick}
              className={className}
            />
          )}
          detail={() => selectedEntityIndex !== undefined && fields[selectedEntityIndex] && (
            <EntityDetailPane
              key={fields[selectedEntityIndex].entityId}
              entity={fields[selectedEntityIndex]}
              onEntityChanged={handleEntityChangedInDetailPage}
              perspective={perspective}
              entityIndex={selectedEntityIndex}
            />
          )}
        />

      </form>

      {entityTypeSettingsDialogProps && (
        <EntityTypeEditDialog {...entityTypeSettingsDialogProps} />
      )}
    </ReactHookForm.FormProvider>
  );
});

/**
 * グリッドとグラフが縦方向か横方向に並ぶレイアウト
 */
const VerticalOrHorizontalLayout = (props: {
  graphViewPosition: PanelGroupProps['direction'] | undefined
  grid: (className: string) => React.ReactNode
  graph: (className: string) => React.ReactNode
  detail: (className: string) => React.ReactNode
  panelStorage: PanelGroupStorage
  detailPanelCollapsed: boolean
  onDetailPanelCollapsedChanged: (collapsed: boolean) => void
}) => {
  const { graphViewPosition, ...rest } = props

  return graphViewPosition === 'horizontal' ? (
    <HorizontalLayout {...rest} />
  ) : (
    <VerticalLayout {...rest} />
  )
}

const AUTOSAVEID_VERTICAL_LAYOUT = 'page-root-vertical';
const AUTOSAVEID_HORIZONTAL_LAYOUT = 'page-root-horizontal';

/**
 * グリッドとグラフが縦方向に並ぶレイアウト
 */
const VerticalLayout = ({
  grid,
  graph,
  detail,
  panelStorage,
  detailPanelCollapsed,
  onDetailPanelCollapsedChanged,
}: {
  grid: (className: string) => React.ReactNode
  graph: (className: string) => React.ReactNode
  detail: (className: string) => React.ReactNode
  panelStorage: PanelGroupStorage
  detailPanelCollapsed: boolean
  onDetailPanelCollapsedChanged: (collapsed: boolean) => void
}) => {

  const handleDetailPanelCollapse = useEvent(() => {
    onDetailPanelCollapsedChanged(true);
  });
  const handleDetailPanelExpand = useEvent(() => {
    onDetailPanelCollapsedChanged(false);
  });

  return (
    <PanelGroup direction="horizontal" autoSaveId={AUTOSAVEID_HORIZONTAL_LAYOUT} storage={panelStorage}>

      <Panel collapsible minSize={12}>
        <PanelGroup direction="vertical" autoSaveId={AUTOSAVEID_VERTICAL_LAYOUT} storage={panelStorage}>

          {/* グリッド */}
          <Panel collapsible minSize={12}>
            {grid('h-full')}
          </Panel>

          <PanelResizeHandle className="h-2" />

          {/* グラフ */}
          <Panel collapsible minSize={12}>
            {graph(`h-full ${!detailPanelCollapsed ? 'border-r border-gray-200' : ''}`)}
          </Panel>
        </PanelGroup>
      </Panel>

      <PanelResizeHandle className="w-1" />

      {/* 詳細画面 */}
      <Panel
        collapsible
        minSize={12}
        onCollapse={handleDetailPanelCollapse}
        onExpand={handleDetailPanelExpand}
      >
        {detail('')}
      </Panel>

    </PanelGroup>
  )
}

/**
 * グリッドとグラフが横方向に並ぶレイアウト
 */
const HorizontalLayout = ({
  grid,
  graph,
  detail,
  panelStorage,
  detailPanelCollapsed,
  onDetailPanelCollapsedChanged,
}: {
  grid: (className: string) => React.ReactNode
  graph: (className: string) => React.ReactNode
  detail: (className: string) => React.ReactNode
  panelStorage: PanelGroupStorage
  detailPanelCollapsed: boolean
  onDetailPanelCollapsedChanged: (collapsed: boolean) => void
}) => {
  const handleDetailPanelCollapse = useEvent(() => {
    onDetailPanelCollapsedChanged(true);
  });
  const handleDetailPanelExpand = useEvent(() => {
    onDetailPanelCollapsedChanged(false);
  });

  return (
    <PanelGroup direction="horizontal" autoSaveId={AUTOSAVEID_HORIZONTAL_LAYOUT} storage={panelStorage}>

      <Panel collapsible minSize={12}>
        {grid('h-full')}
      </Panel>

      <PanelResizeHandle className="w-1" />

      <Panel collapsible minSize={12}>
        <PanelGroup direction="vertical" autoSaveId={AUTOSAVEID_VERTICAL_LAYOUT} storage={panelStorage}>
          <Panel collapsible minSize={12}>
            {graph(`h-full ${!detailPanelCollapsed ? 'border-b border-gray-400' : ''}`)}
          </Panel>

          <PanelResizeHandle className="h-2 mb-4" />

          <Panel
            collapsible
            minSize={12}
            onCollapse={handleDetailPanelCollapse}
            onExpand={handleDetailPanelExpand}
          >
            {detail('')}
          </Panel>
        </PanelGroup>

      </Panel>

    </PanelGroup>
  )
}

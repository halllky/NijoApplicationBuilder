import * as React from 'react';
import * as ReactRouter from 'react-router-dom';
import * as ReactHookForm from 'react-hook-form';
import * as Icon from '@heroicons/react/24/solid';
import useEvent from 'react-use-event-hook';

import * as Input from '../../input';
import * as Layout from '../../layout';
import { NIJOUI_CLIENT_ROUTE_PARAMS } from '../routing';
import { Perspective, PerspectivePageData } from './types';
import { NijoUiOutletContextType } from '../types';
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels';
import { PerspectivePageGraph } from './PerspectivePage.Graph';
import { EntityTypePage, EntityTypePageGridRef } from './EntityTypePage';

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

export const AfterLoaded = ({ defaultValues, onSubmit }: {
  defaultValues: PerspectivePageData
  onSubmit: (data: PerspectivePageData) => Promise<void>
}) => {
  const formMethods = ReactHookForm.useForm<PerspectivePageData>({ defaultValues });
  const { handleSubmit, formState: { isDirty }, getValues } = formMethods;

  const gridRef = React.useRef<EntityTypePageGridRef>(null);

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
    if (rowIndex !== -1 && gridRef.current) {
      gridRef.current.selectRow(rowIndex, rowIndex);
    }
  });

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form onSubmit={handleSubmit(onSubmit)} className="h-full flex flex-col gap-1 pl-1 pt-1">
        <div className="flex flex-wrap gap-1 items-center mb-2">
          <div className="flex-1 font-semibold">{formMethods.getValues('perspective.name')}</div>
          <Input.IconButton outline mini onClick={() => console.log(JSON.parse(localStorage.getItem('typedDocument') ?? '{}'))}>（デバッグ用）console.log</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton submit={true} outline mini icon={Icon.ArrowDownOnSquareIcon} className="font-bold">保存</Input.IconButton>
        </div>

        <PanelGroup direction="horizontal">

          {/* グラフ */}
          <Panel collapsible minSize={12}>
            <PerspectivePageGraph
              formMethods={formMethods}
              onNodeDoubleClick={handleNodeDoubleClick}
              className="h-full border border-gray-300"
            />
          </Panel>

          <PanelResizeHandle className="w-1" />

          {/* グリッド */}
          <Panel collapsible minSize={12}>
            <EntityTypePage
              ref={gridRef}
              formMethods={formMethods}
              className="h-full"
            />
          </Panel>
        </PanelGroup>

      </form>
    </ReactHookForm.FormProvider>
  );
};

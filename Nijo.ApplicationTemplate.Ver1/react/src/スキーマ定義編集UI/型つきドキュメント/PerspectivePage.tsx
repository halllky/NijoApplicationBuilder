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
import { PerspectivePageGrid, PerspectivePageGridRef } from './PerspectivePage.Grid';
import { PerspectivePageGraph } from './PerspectivePage.Graph';

export const PerspectivePage = () => {
  const { [NIJOUI_CLIENT_ROUTE_PARAMS.PERSPECTIVE_ID]: perspectiveId } = ReactRouter.useParams();
  const {
    typedDoc: {
      isReady,
      loadPerspectivePageData,
      savePerspective,
    }
  } = ReactRouter.useOutletContext<NijoUiOutletContextType>();

  const [currentPerspective, setCurrentPerspective] = React.useState<Perspective | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const formMethods = ReactHookForm.useForm<PerspectivePageData>();
  const { handleSubmit, reset, formState: { isDirty }, getValues } = formMethods;

  const gridRef = React.useRef<PerspectivePageGridRef>(null);

  // データ読み込み
  const loadData = useEvent(async () => {
    if (!isReady) return;
    if (!perspectiveId) {
      setError('perspectiveIdが指定されていません。');
      setIsLoading(false);
      setCurrentPerspective(null);
      reset({} as PerspectivePageData); // 空のデータでリセット
      return;
    }
    setIsLoading(true);
    setError(null);
    const pageData = await loadPerspectivePageData(perspectiveId);
    if (!pageData) {
      setCurrentPerspective(null);
      reset({} as PerspectivePageData);
      setError(`指定されたPerspectiveが見つかりません: ${perspectiveId}`);
      setIsLoading(false);
      return;
    }
    setCurrentPerspective(pageData.perspective);
    reset(pageData); // 読み込んだデータでフォームをリセット
    setIsLoading(false);
  });

  React.useEffect(() => {
    loadData();
  }, [isReady]);

  // 保存処理
  const onSubmit = useEvent(async (data: PerspectivePageData) => {
    setIsLoading(true);
    setError(null);
    if (!perspectiveId || !currentPerspective) {
      setError('perspectiveIdまたはPerspectiveデータが読み込まれていません。保存できません。');
      setIsLoading(false);
      return;
    }
    await savePerspective(data);
    // 保存後再読み込み、またはresetを調整してisDirtyをリセット
    const reloadedPageData = await loadPerspectivePageData(perspectiveId);
    if (reloadedPageData) {
      setCurrentPerspective(reloadedPageData.perspective);
      reset(reloadedPageData);
    }
    alert('保存しました。');
    setIsLoading(false);
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
    const rowIndex = nodes.findIndex(n => n.nodeId === nodeId);
    if (rowIndex !== -1 && gridRef.current) {
      gridRef.current.selectRow(rowIndex, rowIndex);
    }
  });

  if (isLoading) {
    return <Layout.NowLoading />;
  }
  if (error) {
    return <div className="p-4 text-red-600">エラー: {error}</div>;
  }
  if (!currentPerspective) {
    return <div className="p-4">データが見つかりません。</div>;
  }

  return (
    <ReactHookForm.FormProvider {...formMethods}>
      <form onSubmit={handleSubmit(onSubmit)} className="h-full flex flex-col gap-1 pl-1 pt-1">
        <div className="flex flex-wrap gap-1 items-center mb-2">
          <div className="flex-1 font-semibold">{currentPerspective.name}</div>
          <Input.IconButton outline mini onClick={() => console.log(JSON.parse(localStorage.getItem('typedDocument') ?? '{}'))}>（デバッグ用）console.log</Input.IconButton>
          <div className="basis-2"></div>
          <Input.IconButton submit={true} outline mini icon={Icon.ArrowDownOnSquareIcon} className="font-bold">保存</Input.IconButton>
        </div>

        <PanelGroup direction="horizontal">

          {/* グラフ */}
          <Panel>
            <PerspectivePageGraph
              formMethods={formMethods}
              onNodeDoubleClick={handleNodeDoubleClick}
              className="h-full border border-gray-300"
            />
          </Panel>

          <PanelResizeHandle className="w-1" />

          {/* グリッド */}
          <Panel defaultSize={40} collapsible minSize={8}>
            <PerspectivePageGrid
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

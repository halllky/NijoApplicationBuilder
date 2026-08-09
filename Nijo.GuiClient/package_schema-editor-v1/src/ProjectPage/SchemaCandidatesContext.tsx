import React, { createContext, useContext, useState, ReactNode } from 'react';
import * as ReactHookForm from 'react-hook-form';
import * as ReactRouter from 'react-router-dom';
import { EditingProject } from '../backend';
import { fetchTypeCandidates } from '../backend/api';
import { NIJOUI_CLIENT_ROUTE_PARAMS } from '../routing';

type CandidateItem = { value: string; text: string };

type SchemaCandidatesContextType = {
  isLoading: boolean;
  items: CandidateItem[];
};

const SchemaCandidatesContext = createContext<SchemaCandidatesContextType>({ isLoading: false, items: [] });

/**
 * ノード種類の候補を提供するコンテキストプロバイダ。
 * フォームの内容に応じてサーバーから候補を取得し、コンテキスト経由で提供する。
 */
export const SchemaCandidatesProvider = ({ watch, children }: {
  watch: ReactHookForm.UseFormWatch<EditingProject>,
  children: ReactNode
}) => {
  // フォーム全体の値
  const watchedValues = watch();
  // コンテキスト値
  const [items, setItems] = useState<CandidateItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const abortControllerRef = React.useRef<AbortController | null>(null);

  const contextValue = React.useMemo((): SchemaCandidatesContextType => ({
    isLoading,
    items,
  }), [isLoading, items]);

  // 現在開いているプロジェクトの情報
  const [searchParams] = ReactRouter.useSearchParams();
  const projectDir = searchParams.get(NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR);

  // 候補をリロードする関数。現在のフォームデータを引数に受け取る。
  const reloadCandidates = async (): Promise<void> => {
    abortControllerRef.current?.abort();
    const abortController = new AbortController();
    abortControllerRef.current = abortController;
    setIsLoading(true);
    try {
      const result = await fetchTypeCandidates(projectDir, watchedValues, abortController.signal);
      if (abortController.signal.aborted) {
        return;
      }
      if (!result.ok) {
        console.error('Failed to fetch candidates:', result.error);
        return;
      }
      setItems(result.value);

    } finally {
      if (abortControllerRef.current === abortController) {
        abortControllerRef.current = null;
        setIsLoading(false);
      }
    }
  };

  // 処理の最適化のため、以下のいずれかの情報が変わった時のみ候補のリロードをトリガーする
  // * ルート集約（データ構造・コマンド）の数
  // * ルート集約のいずれかのモデルの種類・名前
  // * ルート集約直下の child, children いずれかの名前
  const [triggerValue, setTriggerValue] = useState<unknown[]>([]);
  React.useEffect(() => {
    const roots = [...(watchedValues.dataStructures ?? []), ...(watchedValues.commands ?? [])];
    const newTriggerValue = [
      roots.length,
      ...roots.flatMap(root => [
        root.model,
        root.physicalName,
        ...root.members
          .filter(m => m.type.kind === 'child' || m.type.kind === 'children')
          .map(m => m.physicalName),
      ]),
    ];
    const isChanged = newTriggerValue.length !== triggerValue.length ||
      newTriggerValue.some((v, i) => v !== triggerValue[i]);
    if (isChanged) {
      setTriggerValue(newTriggerValue);
      reloadCandidates();
    }
  }, [watchedValues]);

  return (
    <SchemaCandidatesContext.Provider value={contextValue}>
      {children}
    </SchemaCandidatesContext.Provider>
  );
};

export const useSchemaCandidates = () => {
  return useContext(SchemaCandidatesContext);
};

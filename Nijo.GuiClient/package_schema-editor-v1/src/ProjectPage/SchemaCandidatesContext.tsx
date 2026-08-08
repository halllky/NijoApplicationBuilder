import React, { createContext, useContext, useState, ReactNode } from 'react';
import * as ReactHookForm from 'react-hook-form';
import { ATTR_TYPE, ApplicationState, TYPE_CHILD, TYPE_CHILDREN } from '../types';
import { SERVER_DOMAIN } from '../main';
import { NIJOUI_CLIENT_ROUTE_PARAMS } from '../routing';
import * as ReactRouter from 'react-router-dom';

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
  watch: ReactHookForm.UseFormWatch<ApplicationState>,
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
      // SERVER_DOMAIN は本番ビルドでは空文字(同一オリジン)のため、baseを渡さないと
      // new URL('/api/types') が Invalid URL で例外になる。
      // SERVER_DOMAINが絶対URLの場合(ローカル開発時)はbaseは無視される。
      const url = new URL(`${SERVER_DOMAIN}/api/types`, window.location.origin);
      url.searchParams.set(NIJOUI_CLIENT_ROUTE_PARAMS.QUERY_PROJECT_DIR, projectDir ?? '');

      const res = await fetch(url.toString(), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(watchedValues),
      });

      if (abortController.signal.aborted) {
        return;
      }
      if (!res.ok) {
        console.error('Failed to fetch candidates:', res.statusText);
        return;
      }

      const result: CandidateItem[] = await res.json();
      if (abortController.signal.aborted) {
        return;
      }
      setItems(result);

    } catch (error) {
      console.error('Error fetching candidates:', error);

    } finally {
      if (abortControllerRef.current === abortController) {
        abortControllerRef.current = null;
        setIsLoading(false);
      }
    }
  };

  // 処理の最適化のため、以下のいずれかの情報が変わった時のみ候補のリロードをトリガーする
  // * ルート集約の数
  // * ルート集約のいずれかのモデルの種類
  // * ルート集約, Child, Children のいずれかの名前
  const [triggerValue, setTriggerValue] = useState<unknown[]>([]);
  React.useEffect(() => {
    const newTriggerValue = [
      watchedValues.xmlElementTrees?.length,
      ...(watchedValues.xmlElementTrees?.flatMap(tree => [
        tree.xmlElements?.[0].attributes?.[ATTR_TYPE],
        tree.xmlElements?.[0].localName,
        ...tree.xmlElements
          ?.filter(el => el.attributes?.[ATTR_TYPE] === TYPE_CHILD
            || el.attributes?.[ATTR_TYPE] === TYPE_CHILDREN)
          .map(el => el.localName)
        ?? []
      ]) ?? []),
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

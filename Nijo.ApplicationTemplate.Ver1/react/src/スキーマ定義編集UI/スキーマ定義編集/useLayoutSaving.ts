import React from 'react';
import cytoscape from 'cytoscape';
import { ViewState } from '../../layout/GraphView/Cy.SaveLoad';

export const LOCAL_STORAGE_KEY = 'nijoUiAggregateDiagramLayout';

/**
 * ダイアグラムのレイアウト設定をlocalStorageに保存するための型
 */
export type DiagramLayoutSettings = Partial<ViewState> & {
  /** ルート集約のみ表示フラグ */
  onlyRoot: boolean;
}

/**
 * ダイアグラムのレイアウト設定をlocalStorageに保存するためのカスタムフック
 */
export const useLayoutSaving = () => {

  // 保存された値
  const [forceUpdate, setForceUpdate] = React.useState(-1);
  const savedLayout: DiagramLayoutSettings | null = React.useMemo(() => {
    const savedSettings = localStorage.getItem(LOCAL_STORAGE_KEY);
    if (savedSettings) {
      try {
        const parsedSettings: DiagramLayoutSettings = JSON.parse(savedSettings);
        return parsedSettings;
      } catch (error) {
        console.error("Failed to parse saved layout settings:", error);
        localStorage.removeItem(LOCAL_STORAGE_KEY); // 不正なデータは削除
        return null;
      }
    }
    return null;
  }, [forceUpdate]);

  const savedViewState: Partial<ViewState> | undefined = React.useMemo(() => {
    if (!savedLayout) return undefined;
    return {
      nodePositions: savedLayout.nodePositions,
      collapsedNodes: savedLayout.collapsedNodes,
      zoom: savedLayout.zoom,
      scrollPosition: savedLayout.scrollPosition,
    }
  }, [savedLayout]);

  // 保存実行
  const triggerSaveLayout = React.useCallback((
    /** Cytoscapeのdragfreeイベントの引数。undefinedの場合、現在のlocalStorageの情報を正とする */
    event: cytoscape.EventObject | undefined,
    /** ルート集約のみ表示フラグ */
    onlyRoot: boolean
  ): void => {

    // Cytoscapeが持つ情報。
    // eventがある場合はその情報で上書き、
    // eventがない場合は、現在のlocalStorageの情報を正とする
    if (event) {
      // eventがある場合はその情報で上書き
      const cyViewState: ViewState = {
        nodePositions: {},
        collapsedNodes: [],
        zoom: event.cy.zoom(),
        scrollPosition: event.cy.pan(),
      }
      event.cy.nodes().forEach(node => {
        if (node.id()) cyViewState.nodePositions[node.id()] = node.position();
      });
      localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify({
        ...cyViewState,
        onlyRoot,
      } satisfies DiagramLayoutSettings));

    } else {
      const savedSettings = localStorage.getItem(LOCAL_STORAGE_KEY);
      if (savedSettings) {
        // eventがない場合は、現在のlocalStorageの情報を正とする
        const parsedSettings: DiagramLayoutSettings = JSON.parse(savedSettings);
        localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify({
          ...parsedSettings,
          onlyRoot,
        } satisfies DiagramLayoutSettings));

      } else {
        // 保存された情報がない場合は、ルート集約のみ表示フラグのみ保存
        localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify({
          onlyRoot,
        } satisfies DiagramLayoutSettings));
      }
    }

    // 保存
    setForceUpdate(prev => prev * -1);
  }, []);

  const clearSavedLayout = React.useCallback((): void => {
    localStorage.removeItem(LOCAL_STORAGE_KEY);
    setForceUpdate(prev => prev * -1);
  }, []);

  return {
    /** 現在のレイアウト設定をlocalStorageに保存する */
    triggerSaveLayout,
    /** 保存されたレイアウト設定を削除する */
    clearSavedLayout,
    /** 保存された「ルート集約のみ表示」フラグ */
    savedOnlyRoot: savedLayout?.onlyRoot,
    /** 保存されたノード位置 */
    savedViewState,
  }
}

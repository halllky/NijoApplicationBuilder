import { useCallback, useState } from 'react'
import cytoscape from 'cytoscape'
// @ts-expect-error このライブラリが型定義を提供していないので型チェックを無視する
import klay from 'cytoscape-klay'
// @ts-expect-error このライブラリが型定義を提供していないので型チェックを無視する
import dagre from 'cytoscape-dagre'
// @ts-expect-error このライブラリが型定義を提供していないので型チェックを無視する
import fcose from 'cytoscape-fcose'

const configure = (cy: typeof cytoscape) => {
  cy.use(klay)
  cy.use(dagre)
  cy.use(fcose)
}

/** ノードの自動整列に用いられるロジックの名前 */
export type LayoutLogicName
  = 'klay'
  | 'dagre'
  | 'fcose'
  | 'null'
  | 'random'
  | 'preset'
  | 'grid'
  | 'circle'
  | 'concentric'
  | 'breadthfirst'
  | 'cose'

export interface LayoutBeforeApplyParams {
  cy: cytoscape.Core;
  layoutTargetNodes: cytoscape.NodeCollection;
  layoutName: LayoutLogicName;
}

/**
 * エッジの接続関係を解析して各ノードの階層レベルを計算する
 */
const calculateHierarchyLevels = (cy: cytoscape.Core, nodes: cytoscape.NodeCollection): Map<string, number> => {
  const levelMap = new Map<string, number>();
  const nodeIds = new Set(nodes.map(n => n.id()));
  const inDegree = new Map<string, number>();
  const outEdges = new Map<string, string[]>();

  // console.log('=== 階層計算開始 ===');
  // console.log('対象ノード数:', nodeIds.size);

  // 各ノードの入次数と出エッジを計算
  nodeIds.forEach(id => {
    inDegree.set(id, 0);
    outEdges.set(id, []);
  });

  // エッジを処理して入次数と出エッジを設定
  cy.edges().forEach(edge => {
    const sourceId = edge.source().id();
    const targetId = edge.target().id();

    if (nodeIds.has(sourceId) && nodeIds.has(targetId)) {
      inDegree.set(targetId, (inDegree.get(targetId) || 0) + 1);
      outEdges.get(sourceId)?.push(targetId);
      // console.log(`エッジ: ${sourceId} -> ${targetId}`);
    }
  });

  // トポロジカルソートで階層レベルを決定
  const queue: { id: string; level: number }[] = [];

  // 入次数が0のノード（ルートノード）を見つける
  inDegree.forEach((degree, nodeId) => {
    if (degree === 0) {
      queue.push({ id: nodeId, level: 0 });
      levelMap.set(nodeId, 0);
    }
  });

  // console.log('ルートノード数:', queue.length);

  // BFSで階層レベルを計算
  while (queue.length > 0) {
    const { id: currentId, level: currentLevel } = queue.shift()!;

    outEdges.get(currentId)?.forEach(targetId => {
      const newInDegree = (inDegree.get(targetId) || 0) - 1;
      inDegree.set(targetId, newInDegree);

      if (newInDegree === 0) {
        const newLevel = currentLevel + 1;
        levelMap.set(targetId, newLevel);
        queue.push({ id: targetId, level: newLevel });
        // console.log(`ノード ${targetId} をレベル ${newLevel} に設定`);
      }
    });
  }

  // 残りのノードには適当にレベルを割り当て
  nodeIds.forEach(id => {
    if (!levelMap.has(id)) {
      levelMap.set(id, 0);
    }
  });

  // console.log('階層計算結果:', Object.fromEntries(levelMap));
  // console.log('=== 階層計算終了 ===');

  return levelMap;
};

/**
 * 階層配置を実行する（klayとpresetレイアウト用）
 */
const applyHierarchicalLayout = (params: LayoutBeforeApplyParams): void => {
  const { cy, layoutTargetNodes, layoutName } = params;

  if (layoutName !== 'klay' && layoutName !== 'preset') {
    return;
  }

  const hierarchyLevels = calculateHierarchyLevels(cy, layoutTargetNodes);

  // 階層レベルに基づいてノード位置を設定
  const levelGroups = new Map<number, cytoscape.NodeSingular[]>();
  const maxLevel = Math.max(...Array.from(hierarchyLevels.values()));

  // レベルごとにノードをグループ化
  layoutTargetNodes.forEach(node => {
    const level = hierarchyLevels.get(node.id()) ?? 0;
    if (!levelGroups.has(level)) {
      levelGroups.set(level, []);
    }
    levelGroups.get(level)!.push(node);
  });

  // 各レベルのノードの位置を設定
  const levelHeight = 200; // レベル間の距離
  const nodeSpacing = 150; // 同一レベル内のノード間の距離

  for (let level = 0; level <= maxLevel; level++) {
    const nodesInLevel = levelGroups.get(level) || [];
    const y = level * levelHeight;

    nodesInLevel.forEach((node, index) => {
      const x = (index - (nodesInLevel.length - 1) / 2) * nodeSpacing;
      node.position({ x, y });
    });
  }

  // console.log(`階層配置完了: ${maxLevel + 1}レベル, ${layoutTargetNodes.length}ノード`);
};

export const OPTION_LIST: { [key in LayoutLogicName]: {
  options: cytoscape.LayoutOptions,
  onBeforeApply?: (params: LayoutBeforeApplyParams) => void,
} } = {
  'klay': {
    options: {
      name: 'klay',
      // レイアウト方向（UNDEFINED, RIGHT, LEFT, DOWN, UP）
      direction: 'DOWN',
      // エッジルーティング（POLYLINE, ORTHOGONAL, SPLINES）
      edgeRouting: 'POLYLINE',
      // ノード間のスペーシング
      spacing: 20,
      // ボーダーのスペーシング
      borderSpacing: 20,
      // 交差最小化（LAYER_SWEEP, INTERACTIVE）
      crossingMinimization: 'LAYER_SWEEP',
      // ノードの配置戦略（LINEAR_SEGMENTSで縦方向の配置を改善）
      nodePlacement: 'LINEAR_SEGMENTS',
      // エッジ間のスペーシングファクター
      edgeSpacingFactor: 1.2,
      // レイヤー内のスペーシングファクター
      inLayerSpacingFactor: 1.2,
      // アスペクト比の制御
      aspectRatio: 1.0,
      // 断絶されたコンポーネントを個別に処理
      separateConnectedComponents: true,
    } as cytoscape.LayoutOptions,
    onBeforeApply: applyHierarchicalLayout,
  },
  'dagre': {
    options: {
      name: 'dagre',
    } as cytoscape.LayoutOptions,
  },
  'fcose': {
    options: {
      name: 'fcose',
      // 力学ベースのレイアウト、自然な配置
      quality: 'default',
      randomize: true,
      animate: false,
      fit: true,
      padding: 20,
      nodeDimensionsIncludeLabels: true,
      uniformNodeDimensions: false,
      packComponents: true,
      nodeRepulsion: 4500,
      idealEdgeLength: 100,
      edgeElasticity: 0.45,
      nestingFactor: 0.1,
      numIter: 2500,
      // 横方向の広がりを抑制
      nodeSpacing: 50,
      spacingFactor: 1.2,
    } as cytoscape.LayoutOptions
  },
  'breadthfirst': {
    options: {
      name: 'breadthfirst',
      // 階層的な配置でコンパクトに
      directed: true,
      padding: 20,
      spacingFactor: 0.8,
      maximal: false,
      grid: true,
      roots: undefined, // 自動でルートを検出
      animate: false,
      fit: true,
    } as cytoscape.LayoutOptions
  },
  'cose': {
    options: {
      name: 'cose',
      // 改良されたCoSEレイアウト
      animate: false,
      fit: true,
      padding: 20,
      nodeRepulsion: 400000,
      nodeOverlap: 10,
      idealEdgeLength: 100,
      edgeElasticity: 100,
      nestingFactor: 5,
      gravity: 80,
      numIter: 1000,
      initialTemp: 200,
      coolingFactor: 0.95,
      minTemp: 1.0,
      spacingFactor: 0.8,
    } as cytoscape.LayoutOptions
  },
  'null': { options: { name: 'null' } },
  'random': { options: { name: 'random' } },
  'preset': {
    options: { name: 'preset' },
    onBeforeApply: applyHierarchicalLayout,
  },
  'grid': {
    options: {
      name: 'grid',
      fit: true,
      padding: 20,
      spacingFactor: 0.8,
      rows: undefined, // 自動で行数を決定
      cols: undefined, // 自動で列数を決定
      position: function (node: any) { return { row: undefined, col: undefined }; },
      sort: undefined,
      animate: false,
    } as cytoscape.LayoutOptions
  },
  'circle': {
    options: {
      name: 'circle',
      fit: true,
      padding: 20,
      boundingBox: undefined,
      avoidOverlap: true,
      radius: undefined,
      startAngle: -Math.PI / 2,
      counterclockwise: false,
      sort: undefined,
      animate: false,
      spacingFactor: 0.8,
    } as cytoscape.LayoutOptions
  },
  'concentric': {
    options: {
      name: 'concentric',
      fit: true,
      padding: 20,
      startAngle: -Math.PI / 2,
      counterclockwise: false,
      minNodeSpacing: 50,
      spacingFactor: 0.8,
      animate: false,
      concentric: function (node: any) { return node.degree(); },
      levelWidth: function (nodes: any) { return 2; },
    } as cytoscape.LayoutOptions
  },
}
const DEFAULT = OPTION_LIST['klay']

const useAutoLayout = (cy: cytoscape.Core | undefined) => {
  const [currentLayout, setCurrentLayout] = useState(DEFAULT.options.name as LayoutLogicName)
  const LayoutSelector = useCallback(() => {
    return (
      <select
        className="border border-1 border-zinc-400"
        value={currentLayout}
        onChange={e => setCurrentLayout(e.target.value as LayoutLogicName)}>
        {Object.keys(OPTION_LIST).map(key => (
          <option key={key} value={key}>
            {key}
          </option>
        ))}
      </select>
    )
  }, [currentLayout])

  const autoLayout = useCallback((fit?: boolean) => {
    if (!cy) return
    if (fit) cy.resize().fit().reset()

    const layoutConfig = OPTION_LIST[currentLayout];

    // レイアウト対象のノードを取得
    const layoutTargetNodes = cy.nodes().filter(node => {
      // isMember, isTag, parent（親ノード）がある場合は除外
      if (node.data('isMember') !== undefined) return false;
      if (node.data('isTag') !== undefined) return false;
      if (node.data('parentNodeId') !== undefined) return false;
      return true;
    });

    // onBeforeApplyがある場合は実行
    if (layoutConfig.onBeforeApply) {
      layoutConfig.onBeforeApply({
        cy,
        layoutTargetNodes,
        layoutName: currentLayout
      });
    }

    cy.layout(layoutConfig.options)?.run()
  }, [cy, currentLayout])

  return {
    autoLayout,
    LayoutSelector,
  }
}

export default {
  OPTION_LIST,
  DEFAULT,
  configure,
  useAutoLayout,
}

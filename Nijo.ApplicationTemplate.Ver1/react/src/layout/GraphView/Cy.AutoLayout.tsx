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

export const OPTION_LIST: { [key in LayoutLogicName]: cytoscape.LayoutOptions } = {
  'klay': {
    name: 'klay',
    klay: {
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
    }
  } as cytoscape.LayoutOptions,
  'dagre': {
    name: 'dagre',
  } as cytoscape.LayoutOptions,
  'fcose': {
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
  } as cytoscape.LayoutOptions,
  'breadthfirst': {
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
  } as cytoscape.LayoutOptions,
  'cose': {
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
  } as cytoscape.LayoutOptions,
  'null': { name: 'null' },
  'random': { name: 'random' },
  'preset': { name: 'preset' },
  'grid': {
    name: 'grid',
    fit: true,
    padding: 20,
    spacingFactor: 0.8,
    rows: undefined, // 自動で行数を決定
    cols: undefined, // 自動で列数を決定
    position: function (node: any) { return { row: undefined, col: undefined }; },
    sort: undefined,
    animate: false,
  } as cytoscape.LayoutOptions,
  'circle': {
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
  } as cytoscape.LayoutOptions,
  'concentric': {
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
  } as cytoscape.LayoutOptions,
}
const DEFAULT = OPTION_LIST['klay']

const useAutoLayout = (cy: cytoscape.Core | undefined) => {
  const [currentLayout, setCurrentLayout] = useState(DEFAULT.name as LayoutLogicName)
  const LayoutSelector = useCallback(() => {
    return (
      <select
        className="border border-1 border-zinc-400"
        value={currentLayout}
        onChange={e => setCurrentLayout(e.target.value as LayoutLogicName)}>
        {Object.entries(OPTION_LIST).map(([key]) => (
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
    cy.layout(OPTION_LIST[currentLayout])?.run()
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

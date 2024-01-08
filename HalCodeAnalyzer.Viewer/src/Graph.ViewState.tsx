import { useCallback, useEffect, useReducer } from 'react'
import { Messaging, ReactHookUtil } from './util'
import { useTauriApi } from './TauriApi'

export type ViewState = {
  nodePositions: { [nodeId: string]: cytoscape.Position }
  collapsedNodes: string[]
}

export const getEmptyViewState = (): ViewState => ({
  nodePositions: {},
  collapsedNodes: [],
})

const viewStateReducer = ReactHookUtil.defineReducer((state: ViewState) => ({
  load: (value: ViewState) => ({ ...value }),
  clear: () => ({ ...state, nodePositions: {}, collapsedNodes: [] }),
}))
export type ViewStateDispatcher = ReactHookUtil.DispatcherOf<typeof viewStateReducer>

export const useViewState = () => {
  const [viewState, dispatchViewState] = useReducer(viewStateReducer, getEmptyViewState())
  const [, dispatchMessage] = Messaging.useMsgContext()
  const { loadViewStateFile, saveViewStateFile } = useTauriApi()

  // 初期表示時
  useEffect(() => {
    loadViewStateFile().then(obj => {
      dispatchViewState(vs => vs.load(obj))
    }).catch(err => {
      dispatchMessage(msg => msg.error(err))
    })
  }, [])

  const saveViewState = useCallback(async () => {
    try {
      await saveViewStateFile(viewState)
      dispatchMessage(msg => msg.info('保存しました。'))
    } catch (error) {
      dispatchMessage(msg => msg.error(error))
    }
  }, [viewState])

  return {
    viewState,
    dispatchViewState,
    saveViewState,
  }
}

const getViewState = (beforeState: ViewState, cy: cytoscape.Core): ViewState => {
  return {
    ...beforeState,
    nodePositions: cy.nodes().reduce((map, node) => {
      const pos = node.position()
      map[node.id()] = {
        x: Math.trunc(pos.x * 10000) / 10000,
        y: Math.trunc(pos.y * 10000) / 10000,
      }
      return map
    }, { ...beforeState.nodePositions }),
  }
}
const restoreViewState = (viewState: ViewState, cy: cytoscape.Core) => {
  for (const node of cy.nodes()) {

    // 子要素をもつノードの位置は子要素の位置が決まると自動的に決まるのであえて設定しない
    if (node.isParent()) continue

    const pos = viewState.nodePositions[node.id()]
    if (pos) node.position(pos)
  }
}

export default {
  getViewState,
  restoreViewState,
  useViewState,
}

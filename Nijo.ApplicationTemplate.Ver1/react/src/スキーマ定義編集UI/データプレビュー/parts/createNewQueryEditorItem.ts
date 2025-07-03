import { UUID } from "uuidjs"
import { DiagramViewState, QueryEditorItem } from "../types"

/** 新規ウィンドウを作成する */
export const createNewQueryEditorItem = (
  type: "sqlAndResult" | "dbTableEditor" | "dbTableSingleEditor" | "dbTableSingleEditor(new)",
  queryTitleOrTableName: string,
  keys?: string[],
  viewState?: DiagramViewState
): QueryEditorItem => {

  // 現在の表示領域内に配置するための座標を計算
  const calculatePosition = () => {
    if (!viewState) {
      return { x: 0, y: 0 }
    }

    // 現在の表示領域の左上座標を計算（ズーム適用前の座標系）
    const viewportX = -viewState.panOffset.x
    const viewportY = -viewState.panOffset.y

    // ズーム率を考慮した適切な余白
    // ズーム率が小さい（縮小表示）ほど、物理的に大きな余白が必要
    const margin = 50 / viewState.zoom

    // 表示領域内の左上に適切な余白を取って配置
    return {
      x: viewportX + margin,
      y: viewportY + margin,
    }
  }

  const position = calculatePosition()

  if (type === "sqlAndResult") {
    return {
      id: UUID.generate(),
      title: queryTitleOrTableName,
      type,
      sql: "SELECT 1",
      isSettingCollapsed: false,
      layout: {
        x: position.x,
        y: position.y,
        width: 640,
        height: 200,
      },
    }
  } else if (type === "dbTableEditor") {
    return {
      id: UUID.generate(),
      title: queryTitleOrTableName,
      type,
      tableName: queryTitleOrTableName,
      whereClause: "",
      isSettingCollapsed: false,
      layout: {
        x: position.x,
        y: position.y,
        width: 640,
        height: 200,
      },
    }
  } else {
    if (type === "dbTableSingleEditor" && !keys) throw new Error("keys is required")
    if (type === "dbTableSingleEditor(new)" && keys) throw new Error("keys is not allowed")

    return {
      id: UUID.generate(),
      title: queryTitleOrTableName,
      type: "dbTableSingleEditor",
      rootTableName: queryTitleOrTableName,
      rootItemKeys: type === "dbTableSingleEditor" ? keys! : null,
      isSettingCollapsed: false,
      layout: {
        x: position.x,
        y: position.y,
        width: 640,
        height: 200,
      },
    }
  }
}

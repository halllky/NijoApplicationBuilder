import * as ReactHookForm from "react-hook-form"
import { ColumnDef, ColumnDefFactories } from "./useFieldArrayEx"

/**
 * 列定義を取得する関数の型。
 * この関数の参照が変わる度にグリッドが再レンダリングされるため、
 * 原則として `useCallback` を使用すること。
 *
 * @param cellType セル型定義ヘルパー関数の一覧。
 */
export type GetColumnDefsFunction<TRow extends ReactHookForm.FieldValues> = (cellType: ColumnDefFactories<TRow>) => ColumnDef<TRow>[]

import React from "react"
import * as ReactHookForm from "react-hook-form"
import { QueryEditor } from "./types"

/**
 * データプレビューの設定をコンポーネントの深い箇所から更新したいことがあるので
 */
export const DataPreviewGlobalContext = React.createContext({} as ReactHookForm.UseFormReturn<QueryEditor>)

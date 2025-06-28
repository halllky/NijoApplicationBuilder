import React from "react"
import * as ReactHookForm from "react-hook-form"
import { EditorDesign, EditorDesignByAgggregate, ReloadTrigger } from "./types"

export type EditorDesignContextProviderProps = {
  editorDesign: EditorDesign | undefined
  trigger: ReloadTrigger
  onIsDirtyChange?: (isDirty: boolean) => void
  children?: React.ReactNode
}

export type EditorDesignContextProviderRef = {
  getUpdated: () => EditorDesign
}

type EditorDesignContextInternalType = {
  savedDesign: EditorDesign
  setUpdated: React.Dispatch<React.SetStateAction<EditorDesign>>
  setIsDirty: React.Dispatch<React.SetStateAction<boolean>>
}

const EditorDesignContextInternal = React.createContext<EditorDesignContextInternalType>({
  savedDesign: {},
  setUpdated: () => { },
  setIsDirty: () => { },
})

export const EditorDesignContextProvider = React.forwardRef<EditorDesignContextProviderRef, EditorDesignContextProviderProps>(({ children, onIsDirtyChange, editorDesign, trigger }, ref) => {
  const [updated, setUpdated] = React.useState<EditorDesign>({})
  const [isDirty, setIsDirty] = React.useState(false)

  React.useEffect(() => {
    setUpdated(editorDesign ? window.structuredClone(editorDesign) : {})
    setIsDirty(false)
  }, [editorDesign, trigger])

  React.useEffect(() => {
    onIsDirtyChange?.(isDirty)
  }, [isDirty, onIsDirtyChange])

  React.useImperativeHandle(ref, () => ({
    getUpdated: () => updated,
  }), [updated])

  const contextValue: EditorDesignContextInternalType = React.useMemo(() => ({
    savedDesign: editorDesign ?? {},
    setUpdated,
    setIsDirty,
  }), [editorDesign, setUpdated, setIsDirty])

  return (
    <EditorDesignContextInternal.Provider value={contextValue}>
      {children}
    </EditorDesignContextInternal.Provider>
  )
})


/**
 * 集約単位での表示設定。
 * 例えば従業員テーブルのSingleViewについての設定は
 * 従業員Aのデータのウィンドウにも従業員Bのデータのウィンドウにも適用される。
 */
export const useEditorDesign = () => {
  const {
    savedDesign,
    setUpdated,
    setIsDirty,
  } = React.useContext(EditorDesignContextInternal)

  const updateDesign = React.useCallback(<FieldPath extends ReactHookForm.Path<EditorDesignByAgggregate>>(
    aggregatePath: string,
    fieldPath: FieldPath,
    value: ReactHookForm.PathValue<EditorDesignByAgggregate, FieldPath>
  ) => {
    setUpdated(prev => {
      const newDesign: EditorDesignByAgggregate = { ...prev[aggregatePath] }
      ReactHookForm.set(newDesign, fieldPath, value)
      return { ...prev, [aggregatePath]: newDesign }
    })
    setIsDirty(true)
  }, [setUpdated, setIsDirty])

  return {
    /** 保存された表示設定 */
    savedDesign,
    /** 集約単位での表示設定を更新する。まだ保存はされない。 */
    updateDesign,
  }
}
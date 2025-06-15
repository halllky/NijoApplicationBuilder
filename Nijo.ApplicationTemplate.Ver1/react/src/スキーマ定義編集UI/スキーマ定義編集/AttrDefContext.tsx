import * as React from "react"
import * as ReactHookForm from "react-hook-form"
import { SchemaDefinitionGlobalState, XmlElementAttribute } from "./types"

const AttrDefContext = React.createContext<Map<string, XmlElementAttribute>>(new Map())

/** アプリケーション全体のどこからでも属性定義のMapを参照できるようにするためのコンテキスト */
export const useAttrDefs = () => {
  return React.useContext(AttrDefContext)
}

/** アプリケーション全体のどこからでも属性定義のMapを参照できるようにするためのコンテキスト */
export const AttrDefsProvider = ({ control, children }: {
  control: ReactHookForm.Control<SchemaDefinitionGlobalState>
  children: React.ReactNode
}) => {

  const watched = ReactHookForm.useWatch({ name: 'attributeDefs', control })
  const map = React.useMemo(() => {
    return new Map(watched.map(attrDef => [attrDef.attributeName, attrDef]))
  }, [watched])

  return (
    <AttrDefContext.Provider value={map}>
      {children}
    </AttrDefContext.Provider>
  )
}

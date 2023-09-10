import React, { createContext, useContext } from "react"

type PageMode = 'create' | 'view' | 'edit'

type PageState = {
  singleViewPageMode: PageMode
}
type Action
  = {}

export const pageContextReducer: React.Reducer<PageState, Action> = (state, action) => {
  return { ...state }
}

export const PageContext = createContext<[PageState, React.Dispatch<Action>]>([undefined as any, undefined as any])

export const usePageContext = () => {
  return useContext(PageContext)
}

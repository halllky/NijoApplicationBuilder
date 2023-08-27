import React, { createContext, useContext } from "react"

type PageState = {
  pageIsReadOnly?: boolean
}
type Action
  = { type: 'changeReadOnly', value: boolean }

export const pageContextReducer: React.Reducer<PageState, Action> = (state, action) => {
  switch (action.type) {
    case 'changeReadOnly':
      return { ...state, pageIsReadOnly: action.value }
  }
}

export const PageContext = createContext<[PageState, React.Dispatch<Action>]>([undefined as any, undefined as any])

export const usePageContext = () => {
  return useContext(PageContext)
}

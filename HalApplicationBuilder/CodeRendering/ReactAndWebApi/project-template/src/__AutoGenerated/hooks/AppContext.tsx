import moment from "moment";
import React, { useContext, useReducer } from "react";
import { UUID } from "uuidjs";
import { Toast, ToastMessage } from "../components/Toast";

const LOCALSTORAGEKEY = '::HALAPP_APIDOMAIN::'

export type AppState = {
  popupMessages: ToastMessage[]
  apiDomain?: string
}
type Action
  = { type: 'pushMsg', msg: string }
  | { type: 'delMessage', id: string }
  | { type: 'changeDomain', value: string }

const createDefaultAppState = (): AppState => ({
  popupMessages: [],
  apiDomain: localStorage.getItem(LOCALSTORAGEKEY) || '',
})
const initialState = createDefaultAppState()
const reducer: React.Reducer<AppState, Action> = (state, action) => {
  switch (action.type) {
    case 'pushMsg': {
      const id = UUID.generate()
      const popupTime = moment().format('YYYY-MM-DD hh:mm:ss')
      const popupMessages = [...state.popupMessages, { id, msg: action.msg, popupTime }]
      return { ...state, popupMessages }
    }
    case 'delMessage': {
      const popupMessages = state.popupMessages.filter(m => m.id !== action.id)
      return { ...state, popupMessages }
    }
    case 'changeDomain': {
      localStorage.setItem(LOCALSTORAGEKEY, action.value)
      return { ...state, apiDomain: action.value }
    }
  }
}

const AppContext = React.createContext<[AppState, React.Dispatch<Action>]>([undefined as any, undefined as any])

export const AppContextProvider = ({ children }: { children?: React.ReactNode }) => {

  const value = useReducer(reducer, initialState)

  return (
    <AppContext.Provider value={value}>

      {children}

      {/* TOAST MESSAGE */}
      <div className="fixed bottom-3 right-3" style={{ zIndex: 9999 }}>
        {value[0].popupMessages.map(msg => <Toast key={msg.id} item={msg} />)}
      </div>
    </AppContext.Provider>
  )
}

export const useAppContext = () => {
  return useContext(AppContext)
}

import moment from "moment";
import React, { useCallback, useContext, useReducer } from "react";
import { UUID } from "uuidjs";
import { Toast, ToastMessage } from "../components/Toast";
import { setTimeout } from "timers";
import * as GlobalFocus from '../hooks/GlobalFocus'

const LOCALSTORAGEKEY = '::HALAPP_APIDOMAIN::'

export type AppState = {
  popupMessages: ToastMessage[]
  apiDomain?: string
}
type Action
  = { type: 'pushMsg', id?: string, msg: string }
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
      const id = action.id ?? UUID.generate()
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

const AppContext = React.createContext<[AppState, React.Dispatch<Action>]>([{ popupMessages: [] }, undefined as any])

export const AppContextProvider = ({ children }: { children?: React.ReactNode }) => {

  const [state, dispatch] = useReducer(reducer, initialState)

  const dispatchWithSetTimeout = useCallback<typeof dispatch>(action => {
    if (action.type === 'pushMsg') {
      const id = UUID.generate()
      dispatch({ ...action, id })
      window.setTimeout(() => dispatch({ type: 'delMessage', id }), 5000)
    } else {
      dispatch(action)
    }
  }, [dispatch])

  return (
    <AppContext.Provider value={[state, dispatchWithSetTimeout]}>
      <GlobalFocus.GlobalFocusPage>

        {children}

        {/* TOAST MESSAGE */}
        <div className="fixed bottom-3 right-3" style={{ zIndex: 9999 }}>
          {state.popupMessages.map(msg => <Toast key={msg.id} item={msg} />)}
        </div>
      </GlobalFocus.GlobalFocusPage>
    </AppContext.Provider>
  )
}

export const useAppContext = () => {
  return useContext(AppContext)
}
